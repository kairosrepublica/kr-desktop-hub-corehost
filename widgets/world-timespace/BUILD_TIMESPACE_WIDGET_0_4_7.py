#!/usr/bin/env python3
from __future__ import annotations

import hashlib
import json
import os
import shutil
import subprocess
import sys
import tempfile
import time
import zipfile
from pathlib import Path
from typing import Any

PACKAGE_VERSION = "0.4.7"
PACKAGE_REVISION = "0.4.7-map-top-gap-6-bottom-pad-2-single-border"
REPORT_NAME = "TIMESPACE_WIDGET_0_4_7_BUILD_REPORT.json"
OUTPUT_WIDGET = f"KR_World_TimeSpace_Widget_{PACKAGE_VERSION}.krwidget.zip"
CONTRACT_DLL = "KRDesktopHub.WidgetSurface.Windows.Contracts.dll"
CONTRACT_MARKERS = [
    b"IWindowsWidgetNetworkReadBroker",
    b"WindowsWidgetNetworkReadRequest",
    b"WindowsWidgetNetworkReadResponse",
]
EXPECTED_CAPABILITIES = {"ui.surface", "height.report", "network.read"}
EXPECTED_MINIMUM_HOST_VERSION = "0.1.0"
EXPECTED_CONTRACT_ASSEMBLY_VERSION = "2.5.0.0"
EXPECTED_CONTRACT_PACKAGE_VERSION = "2.5.0"
EXPECTED_PREFERRED_EXPANDED_HEIGHT_DIP = 286
EXPECTED_LAYOUT_TOKENS = [
    "OuterVerticalPaddingDip = 11.0",
    "HeaderHeightBudgetDip = 48.0",
    "CityGridTopMarginDip = 7.0",
    "CardRowHeightBudgetDip = 63.0",
    "InterRowGapBudgetDip = 9.0",
    "BottomSafetyBudgetDip = 6.0",
    "CardBottomPaddingDip = 3.0",
    "MapExpandedHeightDip = 286.0",
    "MapPanelTopMarginDip = 6.0",
    "var listHeight = OuterVerticalPaddingDip",
    "return Math.Max(MapExpandedHeightDip, listHeight);",
    "MapPanelHeightDip = 221.0",
    "MapPanelCornerRadiusDip = 0.0",
    "Padding = new Thickness(9, 9, 9, 2)",
    "MapArtworkHeightDip = 230.0",
    "var artworkTop = (viewportHeight - artworkHeight) / 2.0;",
    "AddMapBottomBorderLine",
    "var bottomY = Math.Max(0.5, viewportHeight - 0.5);",
    "Y1 = bottomY",
    "StrokeStartLineCap = PenLineCap.Flat",
    "StrokeEndLineCap = PenLineCap.Flat",
    "Stroke = BrushFrom(\"#CBD3DA\")",
    "MapShadowBitmapWidth = 576",
    "new Thickness(8, 6, 8, CardBottomPaddingDip)",
    "DefaultVisibleCityIds",
    "world-time-space-state-v4.json",
    "WidgetDisplayMode.Map",
    "CreateMapPanel",
    "var labelX = x + layout.Dx;",
    "BorderThickness = new Thickness(1, 1, 1, 0)",
    "CornerRadius = new CornerRadius(MapPanelCornerRadiusDip)",
    "OnTitleMouseLeftButtonDown",
    "CreateNightShadowBitmap",
    "MapCityLayouts",
    "world_map_precise_lat84_-60.png",
    "rows * CardRowHeightBudgetDip",
    "Math.Max(0, rows - 1) * InterRowGapBudgetDip",
]
PROHIBITED_CAPABILITIES = {"network.http", "shell.execute", "script.execute"}

EXPECTED_DEFAULT_VISIBLE_CITY_IDS = [
    "local",
    "los-angeles",
    "new-york",
    "buenos-aires",
    "lisbon",
    "johannesburg",
    "istanbul",
    "dubai",
    "ho-chi-minh-city",
    "hong-kong",
    "tokyo",
    "sydney",
]


def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()


def run(cmd: list[str], cwd: Path | None = None, timeout: int = 180) -> dict[str, Any]:
    start = time.time()
    try:
        proc = subprocess.run(
            cmd,
            cwd=str(cwd) if cwd else None,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            timeout=timeout,
        )
        return {
            "cmd": cmd,
            "cwd": str(cwd) if cwd else None,
            "exit_code": proc.returncode,
            "duration_seconds": round(time.time() - start, 3),
            "stdout_tail": proc.stdout[-12000:],
        }
    except subprocess.TimeoutExpired as exc:
        return {
            "cmd": cmd,
            "cwd": str(cwd) if cwd else None,
            "exit_code": 124,
            "duration_seconds": round(time.time() - start, 3),
            "stdout_tail": (exc.stdout or "")[-12000:] if isinstance(exc.stdout, str) else "",
            "error": "TIMEOUT",
        }


def find_dotnet() -> str | None:
    found = shutil.which("dotnet")
    if found:
        return found
    candidates = [
        Path(os.environ.get("ProgramFiles", r"C:\Program Files")) / "dotnet" / "dotnet.exe",
        Path(os.environ.get("ProgramFiles(x86)", r"C:\Program Files (x86)")) / "dotnet" / "dotnet.exe",
        Path(r"C:\Program Files\dotnet\dotnet.exe"),
    ]
    for candidate in candidates:
        if candidate.is_file():
            return str(candidate)
    return None


def contract_has_network_api(dll: Path) -> bool:
    try:
        data = dll.read_bytes()
    except OSError:
        return False
    return all(marker in data for marker in CONTRACT_MARKERS)


def find_upgraded_compiled_contracts_dir(root: Path) -> Path | None:
    """Return only a compiled contract DLL that already exposes CoreHost 2.6.3 network-read markers."""
    candidates: list[Path] = []
    env = os.environ.get("KRDH_CONTRACTS_DIR", "").strip()
    if env:
        candidates.append(Path(env))
    candidates.extend([
        root,
        root / "contracts",
        root / "reference_assemblies",
        root.parent,
    ])

    for candidate in candidates:
        dll = candidate / CONTRACT_DLL
        if dll.is_file() and contract_has_network_api(dll):
            return candidate

    # Bounded local search only near the extracted package; no repo discovery or whole-disk scan.
    found_paths: list[Path] = []
    searched = 0
    for base in [root, root.parent]:
        if not base.exists():
            continue
        for path in base.rglob(CONTRACT_DLL):
            searched += 1
            if searched > 2500:
                break
            if path.is_file() and contract_has_network_api(path):
                found_paths.append(path)
    found_paths.sort(key=lambda path: ("2_6_3" not in str(path) and "2.6.3" not in str(path), str(path).lower()))
    return found_paths[0].parent if found_paths else None


def copytree(src: Path, dst: Path) -> None:
    if dst.exists():
        shutil.rmtree(dst)
    shutil.copytree(src, dst)


def basic_csharp_guard(source_text: str) -> tuple[list[str], dict[str, Any]]:
    """Small guard for repeated low-level mistakes; not a substitute for dotnet build."""
    failures: list[str] = []
    info: dict[str, Any] = {}

    # Ignore strings/comments only lightly; this is deliberately advisory/basic.
    balance = 0
    min_balance = 0
    for ch in source_text:
        if ch == "{":
            balance += 1
        elif ch == "}":
            balance -= 1
            min_balance = min(min_balance, balance)
    info["brace_balance"] = balance
    info["brace_min_balance"] = min_balance
    if balance != 0 or min_balance < 0:
        failures.append("brace_balance")

    if "public sealed class WorldTimeSpaceSurfaceLease" in source_text:
        failures.append("lease_must_not_be_public")
    if "internal sealed class WorldTimeSpaceSurfaceLease" not in source_text:
        failures.append("lease_internal_missing")
    if "IWindowsWidgetSurfaceHostStateSink" not in source_text:
        failures.append("host_state_sink_missing")
    if "readonly TextBlock _localClock" in source_text or "readonly Slider _timeSlider" in source_text or "readonly TextBlock _offsetLabel" in source_text:
        failures.append("readonly_ui_field_regression")
    if "_localClock = new TextBlock" not in source_text or "#C46A21" not in source_text:
        failures.append("local_clock_orange_missing")
    if "IsHitTestVisible = false\n            IsHitTestVisible = false" in source_text:
        failures.append("duplicate_is_hit_test_visible_initializer")
    if "BorderThickness = new Thickness(1),\n            CornerRadius = new CornerRadius(MapPanelCornerRadiusDip)" in source_text and "AddMapBottomBorderLine(viewportWidth, viewportHeight)" in source_text:
        failures.append("map_bottom_border_is_double_drawn")
    if "HttpClient" in source_text or "System.Net.Http" in source_text:
        failures.append("direct_http_client_forbidden")
    if ".Result" in source_text or ".Wait()" in source_text or "Task.Wait(" in source_text:
        failures.append("blocking_async_forbidden")
    return failures, info


def build_bundled_contract(dotnet: str, bundled_contract_project: Path, sandbox_root: Path) -> tuple[Path | None, dict[str, Any]]:
    contract_work_root = sandbox_root / "src" / "contracts_snapshot"
    contract_work_project = contract_work_root / "KRDesktopHub.WidgetSurface.Windows.Contracts"
    copytree(bundled_contract_project.parent, contract_work_project)
    result = run([
        dotnet,
        "build",
        str(contract_work_project / "KRDesktopHub.WidgetSurface.Windows.Contracts.csproj"),
        "-c",
        "Release",
        "/p:ContinuousIntegrationBuild=true",
        "/nologo",
    ], cwd=contract_work_project, timeout=180)
    if result["exit_code"] != 0:
        return None, result
    out_dir = contract_work_project / "bin" / "Release" / "net10.0-windows"
    dll = out_dir / CONTRACT_DLL
    return (out_dir if dll.is_file() else None), result


def main() -> int:
    root = Path(__file__).resolve().parent
    report_path = root / REPORT_NAME
    report: dict[str, Any] = {
        "package": "KR World Time-Space Widget",
        "version": PACKAGE_VERSION,
        "package_revision": PACKAGE_REVISION,
        "mode": "owner_run_build",
        "ok": False,
        "gates": [],
        "artifacts": {},
        "next_valid_owner_action": None,
    }

    def gate(gate_id: str, semantic_status: str, **extra: Any) -> None:
        item = {"id": gate_id, "semantic_status": semantic_status}
        item.update(extra)
        report["gates"].append(item)

    try:
        source_root = root / "source" / "KRDesktopHub.WorldTimeSpace"
        source_file = source_root / "WorldTimeSpaceSurface.cs"
        project_file = source_root / "KRDesktopHub.WorldTimeSpace.csproj"
        manifest = root / "manifest.json"
        bundled_contract_project = root / "contracts_snapshot" / "KRDesktopHub.WidgetSurface.Windows.Contracts" / "KRDesktopHub.WidgetSurface.Windows.Contracts.csproj"

        if not source_root.is_dir() or not project_file.is_file() or not source_file.is_file():
            gate("SOURCE_TRUST", "FAIL_COLLECTED", reason="source project missing")
            report["next_valid_owner_action"] = "Re-extract the package and rerun the same command."
            return 2
        if not manifest.is_file():
            gate("MANIFEST", "FAIL_COLLECTED", reason="manifest.json missing")
            report["next_valid_owner_action"] = "Re-extract the package and rerun the same command."
            return 2
        gate("SOURCE_TRUST", "PASS")

        asset_file = source_root / "Assets" / "world_map_precise_lat84_-60.png"
        if not asset_file.is_file():
            gate("MAP_ASSET", "FAIL_COLLECTED", reason="embedded map asset missing", expected=str(asset_file.relative_to(root)))
            report["next_valid_owner_action"] = f"Upload {REPORT_NAME}; the map-mode widget source is missing its embedded map asset."
            return 2
        gate("MAP_ASSET", "PASS", path=str(asset_file.relative_to(root)), sha256=sha256(asset_file))

        try:
            manifest_data = json.loads(manifest.read_text(encoding="utf-8"))
            capabilities = set(manifest_data.get("capabilities") or [])
        except Exception as exc:
            gate("MANIFEST_PARSE", "FAIL_COLLECTED", reason=str(exc))
            report["next_valid_owner_action"] = f"Upload {REPORT_NAME} so the manifest parse failure can be triaged."
            return 2

        project_text = project_file.read_text(encoding="utf-8")
        manifest_version = str(manifest_data.get("package_version") or "").strip()
        version_failures: list[str] = []
        if manifest_version != PACKAGE_VERSION:
            version_failures.append(f"manifest package_version={manifest_version!r}")
        if f"<Version>{PACKAGE_VERSION}</Version>" not in project_text:
            version_failures.append("csproj Version mismatch")
        if f"<AssemblyVersion>{PACKAGE_VERSION}.0</AssemblyVersion>" not in project_text:
            version_failures.append("csproj AssemblyVersion mismatch")
        if f"<FileVersion>{PACKAGE_VERSION}.0</FileVersion>" not in project_text:
            version_failures.append("csproj FileVersion mismatch")
        if "0.3.0" in project_text or "0.3.1" in project_text or "0.3.2" in project_text or "0.4.0" in project_text or "0.4.1" in project_text or "0.4.2" in project_text or "0.4.3" in project_text:
            version_failures.append("stale pre-0.4.7 token in csproj")
        if version_failures:
            gate("VERSION_IDENTITY", "FAIL_COLLECTED", failures=version_failures, expected=PACKAGE_VERSION)
            report["next_valid_owner_action"] = f"Upload {REPORT_NAME}; package version identity is inconsistent."
            return 2
        gate("VERSION_IDENTITY", "PASS", package_version=PACKAGE_VERSION)

        missing_required = sorted(EXPECTED_CAPABILITIES.difference(capabilities))
        unexpected = sorted(capabilities.difference(EXPECTED_CAPABILITIES))
        prohibited = sorted(capabilities.intersection(PROHIBITED_CAPABILITIES))
        if missing_required or unexpected or prohibited:
            gate(
                "CAPABILITY_POLICY",
                "FAIL_COLLECTED",
                missing_required=missing_required,
                unexpected_capabilities=unexpected,
                prohibited_capabilities=prohibited,
            )
            report["next_valid_owner_action"] = f"Upload {REPORT_NAME}; the manifest capability declaration is invalid for TimeSpace 0.4.7."
            return 2
        gate("CAPABILITY_POLICY", "PASS", capabilities=sorted(capabilities))

        minimum_host_version = str(manifest_data.get("minimum_host_version") or "").strip()
        if minimum_host_version != EXPECTED_MINIMUM_HOST_VERSION:
            gate(
                "MANIFEST_HOST_VERSION",
                "FAIL_COLLECTED",
                expected=EXPECTED_MINIMUM_HOST_VERSION,
                actual=minimum_host_version,
            )
            report["next_valid_owner_action"] = f"Upload {REPORT_NAME}; manifest minimum_host_version must remain installer-compatible for CoreHost 2.6.3 external widgets."
            return 2
        gate("MANIFEST_HOST_VERSION", "PASS", minimum_host_version=minimum_host_version)

        preferred_height = manifest_data.get("preferred_expanded_height_dip")
        if preferred_height != EXPECTED_PREFERRED_EXPANDED_HEIGHT_DIP:
            gate(
                "MANIFEST_LAYOUT_HEIGHT",
                "FAIL_COLLECTED",
                expected=EXPECTED_PREFERRED_EXPANDED_HEIGHT_DIP,
                actual=preferred_height,
            )
            report["next_valid_owner_action"] = f"Upload {REPORT_NAME}; manifest preferred height does not match the TimeSpace 0.4.7 three-row height budget."
            return 2
        gate("MANIFEST_LAYOUT_HEIGHT", "PASS", preferred_expanded_height_dip=preferred_height)

        source_text = source_file.read_text(encoding="utf-8")
        guard_failures, guard_info = basic_csharp_guard(source_text)
        if guard_failures:
            gate("CSharp_BASIC_GUARD", "FAIL_COLLECTED", failures=guard_failures, info=guard_info)
            report["next_valid_owner_action"] = f"Upload {REPORT_NAME}; a basic source guard failed before build."
            return 2
        gate("CSharp_BASIC_GUARD", "PASS", info=guard_info)

        missing_layout_tokens = [token for token in EXPECTED_LAYOUT_TOKENS if token not in source_text]
        stale_layout_tokens = [token for token in ["DefaultHeightDip = 206.0", "RowIncrementDip = 62.0", "DesiredExpandedHeightDip() => DefaultHeightDip"] if token in source_text]
        missing_default_city_tokens = [f'"{city_id}"' for city_id in EXPECTED_DEFAULT_VISIBLE_CITY_IDS if f'"{city_id}"' not in source_text]
        if missing_layout_tokens or stale_layout_tokens:
            gate(
                "SOURCE_LAYOUT_HEIGHT_POLICY",
                "FAIL_COLLECTED",
                missing_layout_tokens=missing_layout_tokens,
                stale_layout_tokens=stale_layout_tokens,
            )
            report["next_valid_owner_action"] = f"Upload {REPORT_NAME}; source height calculation does not include row-gap-aware 0.4.7 map/list layout budget."
            return 2

        # Core 0.4.7 regression check: the panel must fit inside the fixed outer height,
        # and the map artwork must use centered vertical crop, not bottom-only clipping.
        map_budget = EXPECTED_PREFERRED_EXPANDED_HEIGHT_DIP - 9 - 48 - 6 - 2
        map_panel_ok = 221.0 <= map_budget
        crop_contract_ok = (
            "var artworkTop = (viewportHeight - artworkHeight) / 2.0;" in source_text
            and "AddMapCityMarker(city, layout, utcNow, viewportWidth, artworkHeight, artworkTop)" in source_text
            and "AddMapBottomBorderLine(viewportWidth, viewportHeight)" in source_text
        )
        if not map_panel_ok or not crop_contract_ok:
            gate("MAP_VIEWPORT_BUDGET", "FAIL_COLLECTED", map_budget=map_budget, map_panel_ok=map_panel_ok, crop_contract_ok=crop_contract_ok)
            report["next_valid_owner_action"] = f"Upload {REPORT_NAME}; map viewport contract is invalid."
            return 2
        gate("MAP_VIEWPORT_BUDGET", "PASS", map_budget=map_budget, panel_height=221.0, artwork_height=230.0, top_margin=6.0, bottom_padding=2.0)

        gate("SOURCE_LAYOUT_HEIGHT_POLICY", "PASS")

        if missing_default_city_tokens:
            gate("DEFAULT_CITY_MODE", "FAIL_COLLECTED", missing_default_city_tokens=missing_default_city_tokens)
            report["next_valid_owner_action"] = f"Upload {REPORT_NAME}; default three-row city mode is incomplete."
            return 2
        gate("DEFAULT_CITY_MODE", "PASS", default_visible_city_ids=EXPECTED_DEFAULT_VISIBLE_CITY_IDS)

        expected_map_tokens = [
            "[\"hong-kong\"] = new(\"HKG\", 22.3193, 114.1694, -34, -28",
            "[\"tokyo\"] = new(\"TYO\", 35.6762, 139.6503, -18, -20",
            "[\"sydney\"] = new(\"SYD\", -33.8688, 151.2093, -18, -18",
            "[\"ho-chi-minh-city\"] = new(\"HCMC\", 10.8231, 106.6297, -44, 34",
            "MapCityLayouts.TryGetValue",
            "WidgetDisplayMode.Map ? WidgetDisplayMode.List : WidgetDisplayMode.Map",
        ]
        missing_map_tokens = [token for token in expected_map_tokens if token not in source_text]
        if missing_map_tokens:
            gate("MAP_MODE_UI_CONTRACT", "FAIL_COLLECTED", missing_map_tokens=missing_map_tokens)
            report["next_valid_owner_action"] = f"Upload {REPORT_NAME}; the map-mode UI contract is incomplete."
            return 2
        gate("MAP_MODE_UI_CONTRACT", "PASS")

        forbidden_source_tokens = ["HttpClient", "System.Net.Http", "Task.Wait(", ".Result"]
        present_forbidden_tokens = [token for token in forbidden_source_tokens if token in source_text]
        required_source_tokens = [
            "context.Network",
            "IWindowsWidgetNetworkReadBroker?",
            "WindowsWidgetNetworkReadRequest",
            "IWindowsWidgetSurfaceHostStateSink",
            "ReadAsync(",
            "https://date.nager.at/api/v3/PublicHolidays",
            "Accept",
            "Cache-Control",
            "CancelAfter(TimeSpan.FromSeconds(5))",
        ]
        missing_source_tokens = [token for token in required_source_tokens if token not in source_text]
        if present_forbidden_tokens or missing_source_tokens:
            gate(
                "SOURCE_STATIC_NETWORK_CONTRACT",
                "FAIL_COLLECTED",
                present_forbidden_tokens=present_forbidden_tokens,
                missing_source_tokens=missing_source_tokens,
            )
            report["next_valid_owner_action"] = f"Upload {REPORT_NAME}; the source does not match the CoreHost 2.6.3 network-read contract."
            return 2
        gate("SOURCE_STATIC_NETWORK_CONTRACT", "PASS")

        if not bundled_contract_project.is_file():
            gate("BUNDLED_CONTRACT_SOURCE", "FAIL_COLLECTED", reason="bundled CoreHost 2.6.3 contract source snapshot missing")
            report["next_valid_owner_action"] = f"Upload {REPORT_NAME}; the source package is missing its bundled contract snapshot."
            return 2
        bundled_contract_project_text = bundled_contract_project.read_text(encoding="utf-8")
        contract_version_missing = []
        if f"<Version>{EXPECTED_CONTRACT_PACKAGE_VERSION}</Version>" not in bundled_contract_project_text:
            contract_version_missing.append("Version")
        if f"<AssemblyVersion>{EXPECTED_CONTRACT_ASSEMBLY_VERSION}</AssemblyVersion>" not in bundled_contract_project_text:
            contract_version_missing.append("AssemblyVersion")
        if f"<FileVersion>{EXPECTED_CONTRACT_ASSEMBLY_VERSION}</FileVersion>" not in bundled_contract_project_text:
            contract_version_missing.append("FileVersion")
        if contract_version_missing:
            gate("BUNDLED_CONTRACT_IDENTITY", "FAIL_COLLECTED", missing=contract_version_missing, expected_assembly_version=EXPECTED_CONTRACT_ASSEMBLY_VERSION)
            report["next_valid_owner_action"] = f"Upload {REPORT_NAME}; bundled contract snapshot identity does not match CoreHost 2.6.3 contract identity."
            return 2
        gate("BUNDLED_CONTRACT_SOURCE", "PASS", path=str(bundled_contract_project.relative_to(root)))
        gate("BUNDLED_CONTRACT_IDENTITY", "PASS", assembly_version=EXPECTED_CONTRACT_ASSEMBLY_VERSION)

        dotnet = find_dotnet()
        if not dotnet:
            gate("DOTNET", "FAIL_COLLECTED", reason="dotnet not found on PATH or standard Windows path")
            report["next_valid_owner_action"] = "Install or repair the local .NET SDK, then rerun python .\\BUILD_TIMESPACE_WIDGET_0_4_7.py."
            return 2
        gate("DOTNET", "PASS", path=dotnet)

        sdk = run([dotnet, "--list-sdks"], timeout=30)
        report["dotnet_sdks"] = sdk["stdout_tail"]
        if sdk["exit_code"] != 0:
            gate("DOTNET_SDK", "FAIL_COLLECTED", result=sdk)
            report["next_valid_owner_action"] = "Repair the local .NET SDK, then rerun python .\\BUILD_TIMESPACE_WIDGET_0_4_7.py."
            return 2
        if "10." not in sdk["stdout_tail"]:
            gate("DOTNET_SDK", "FAIL_COLLECTED", reason="net10.0 SDK not listed", result=sdk)
            report["next_valid_owner_action"] = "Install .NET 10 SDK or run this inside the KR Desktop Hub build environment that has it, then rerun the same command."
            return 2
        gate("DOTNET_SDK", "PASS")

        sandbox_root = Path(tempfile.gettempdir()) / f"KRWST_{PACKAGE_VERSION.replace('.', '_')}_{os.getpid()}"
        if sandbox_root.exists():
            shutil.rmtree(sandbox_root)
        sandbox_root.mkdir(parents=True)
        work_project = sandbox_root / "src" / "KRDesktopHub.WorldTimeSpace"
        copytree(source_root, work_project)
        shutil.copy2(manifest, sandbox_root / "manifest.json")
        gate("SANDBOX", "PASS", path=str(sandbox_root))

        contracts_dir = find_upgraded_compiled_contracts_dir(root)
        if contracts_dir is not None:
            gate("CONTRACT_DLL", "PASS", source="compiled_external_or_nearby", contracts_dir=str(contracts_dir), sha256=sha256(contracts_dir / CONTRACT_DLL))
        else:
            gate("CONTRACT_DLL", "NOT_RUN", reason="no upgraded compiled contract DLL found; building bundled CoreHost 2.6.3 contract snapshot")
            contracts_dir, contract_build = build_bundled_contract(dotnet, bundled_contract_project, sandbox_root)
            report["contract_build_result"] = contract_build
            if contracts_dir is None:
                gate("CONTRACT_SOURCE_BUILD", "FAIL_COLLECTED", result=contract_build)
                report["next_valid_owner_action"] = f"Upload {REPORT_NAME} so the bundled contract build failure can be triaged."
                return 2
            gate("CONTRACT_SOURCE_BUILD", "PASS", contracts_dir=str(contracts_dir), sha256=sha256(contracts_dir / CONTRACT_DLL))

        contract_dll = contracts_dir / CONTRACT_DLL
        missing_contract_markers = [marker.decode("ascii") for marker in CONTRACT_MARKERS if marker not in contract_dll.read_bytes()]
        if missing_contract_markers:
            gate(
                "CONTRACT_API",
                "FAIL_COLLECTED",
                contracts_dir=str(contracts_dir),
                missing_markers=missing_contract_markers,
            )
            report["next_valid_owner_action"] = (
                "Use CoreHost 2.6.3 or the bundled contract snapshot; the selected contract lacks network-read API markers."
            )
            return 2
        gate("CONTRACT_API", "PASS", required_markers=[marker.decode("ascii") for marker in CONTRACT_MARKERS])

        build = run([
            dotnet,
            "build",
            str(work_project / "KRDesktopHub.WorldTimeSpace.csproj"),
            "-c",
            "Release",
            f"/p:ContractsDir={contracts_dir}",
            "/p:ContinuousIntegrationBuild=true",
            "/nologo",
        ], cwd=work_project, timeout=240)
        report["build_result"] = build
        if build["exit_code"] != 0:
            gate("BUILD", "FAIL_COLLECTED", result=build)
            report["next_valid_owner_action"] = f"Upload {REPORT_NAME} so the failed build output can be triaged."
            return 2
        gate("BUILD", "PASS")

        out_dir = work_project / "bin" / "Release" / "net10.0-windows"
        dll = out_dir / "KRDesktopHub.WorldTimeSpace.dll"
        pdb = out_dir / "KRDesktopHub.WorldTimeSpace.pdb"
        if not dll.is_file():
            gate("ARTIFACT_DLL", "FAIL_COLLECTED", reason="compiled dll missing", out_dir=str(out_dir))
            report["next_valid_owner_action"] = f"Upload {REPORT_NAME} so artifact generation can be triaged."
            return 2
        gate("ARTIFACT_DLL", "PASS", path=str(dll), sha256=sha256(dll))

        package_path = root / OUTPUT_WIDGET
        if package_path.exists():
            package_path.unlink()
        with zipfile.ZipFile(package_path, "w", compression=zipfile.ZIP_DEFLATED) as zf:
            zf.write(dll, "KRDesktopHub.WorldTimeSpace.dll")
            if pdb.is_file():
                zf.write(pdb, "KRDesktopHub.WorldTimeSpace.pdb")
            zf.write(manifest, "manifest.json")
        gate("PACKAGE", "PASS", path=str(package_path), sha256=sha256(package_path))

        report["ok"] = True
        report["artifacts"] = {
            "krwidget_zip": str(package_path),
            "krwidget_zip_sha256": sha256(package_path),
        }
        report["next_valid_owner_action"] = f"Import {OUTPUT_WIDGET} into KR Desktop Hub/CoreHost 2.6.3 and visually verify the widget."
        return 0
    finally:
        try:
            report_path.write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding="utf-8")
            print(f"Report: {report_path}")
            if report.get("ok"):
                print(f"Built: {root / OUTPUT_WIDGET}")
            print(f"Next: {report.get('next_valid_owner_action')}")
        except Exception as exc:
            print(f"FAILED_TO_WRITE_REPORT: {exc}", file=sys.stderr)


if __name__ == "__main__":
    raise SystemExit(main())

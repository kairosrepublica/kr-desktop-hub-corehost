[CmdletBinding()]
param(
    [string]$Version = "0.1.0-rc1"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
$env:DOTNET_NOLOGO = "1"

Add-Type `
    -AssemblyName System.IO.Compression

Add-Type `
    -AssemblyName System.IO.Compression.FileSystem

$RepoRoot =
    (Resolve-Path (
        Join-Path `
            $PSScriptRoot `
            ".."
    )).Path

$DotNetExe =
    "$env:ProgramFiles\dotnet\dotnet.exe"

$ReleaseRoot =
    Join-Path `
        $RepoRoot `
        "dist\releases"

$ZipName =
    "KRDesktopHub_CoreHost_win-x64_portable_v$Version.zip"

$ZipPath =
    Join-Path `
        $ReleaseRoot `
        $ZipName

$HashPath =
    "$ZipPath.sha256.txt"

$BaselinePath =
    Join-Path `
        $ReleaseRoot `
        "KRDesktopHub_CoreHost_win-x64_resource_baseline_v$Version.json"

$WorkRoot =
    Join-Path `
        $env:TEMP `
        ("KRDesktopHub\release-" + [guid]::NewGuid().ToString("N"))

$PublishRoot =
    Join-Path `
        $WorkRoot `
        "publish"

$StageRoot =
    Join-Path `
        $WorkRoot `
        "stage"

$FixtureRoot =
    Join-Path `
        $WorkRoot `
        "fixture"

$RuntimeProcess =
    $null

function Assert-LastExitCode {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message
    )

    if ($LASTEXITCODE -ne 0) {
        throw $Message
    }
}

function Write-Utf8File {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Content
    )

    $Parent =
        Split-Path `
            -Parent `
            $Path

    if ($Parent) {
        New-Item `
            -ItemType Directory `
            -Force `
            -Path $Parent |
            Out-Null
    }

    [System.IO.File]::WriteAllText(
        $Path,
        $Content,
        [System.Text.UTF8Encoding]::new($false))
}

function Copy-DirectoryContents {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Source,

        [Parameter(Mandatory = $true)]
        [string]$Destination
    )

    New-Item `
        -ItemType Directory `
        -Force `
        -Path $Destination |
        Out-Null

    Get-ChildItem `
        -LiteralPath $Source `
        -Force |
        ForEach-Object {
            Copy-Item `
                -LiteralPath $_.FullName `
                -Destination $Destination `
                -Recurse `
                -Force
        }
}

function Stop-ValidationProcess {
    if ($null -eq $script:RuntimeProcess) {
        return
    }

    try {
        if (-not $script:RuntimeProcess.HasExited) {
            Stop-Process `
                -Id $script:RuntimeProcess.Id `
                -Force `
                -ErrorAction SilentlyContinue

            $script:RuntimeProcess.WaitForExit(
                5000) |
                Out-Null
        }
    }
    finally {
        $script:RuntimeProcess =
            $null
    }
}

Set-Location $RepoRoot

$StatusBefore = @(
    git status --porcelain
)

Assert-LastExitCode "Unable to read Git status."

if ($StatusBefore.Count -gt 0) {
    $StatusBefore |
        ForEach-Object {
            Write-Host $_
        }

    throw "Repository must be clean before release-candidate generation."
}

if (-not (Test-Path -LiteralPath $DotNetExe)) {
    throw ".NET SDK executable not found: $DotNetExe"
}

$RunningCoreHost =
    @(
        Get-Process `
            -Name "KRDesktopHub.App.Windows" `
            -ErrorAction SilentlyContinue
    )

if ($RunningCoreHost.Count -gt 0) {
    throw "A KR Desktop Hub process is already running. Exit it from the tray before release validation."
}

New-Item `
    -ItemType Directory `
    -Force `
    -Path $ReleaseRoot |
    Out-Null

if (Test-Path -LiteralPath $WorkRoot) {
    Remove-Item `
        -LiteralPath $WorkRoot `
        -Recurse `
        -Force
}

New-Item `
    -ItemType Directory `
    -Force `
    -Path $PublishRoot |
    Out-Null

New-Item `
    -ItemType Directory `
    -Force `
    -Path $StageRoot |
    Out-Null

try {
    Write-Host "`n=== Release validation 1. Build solution ===" -ForegroundColor Cyan

    & $DotNetExe build `
        ".\KR_Desktop_Hub.sln" `
        --configuration Release

    Assert-LastExitCode "Release-candidate solution build failed."

    Write-Host "`n=== Release validation 2. Run all smoke tests ===" -ForegroundColor Cyan

    $SmokeTests = @(
        ".\tests\KRDesktopHub.Contracts.SmokeTests\KRDesktopHub.Contracts.SmokeTests.csproj",
        ".\tests\KRDesktopHub.Core.SmokeTests\KRDesktopHub.Core.SmokeTests.csproj",
        ".\tests\KRDesktopHub.Platform.Windows.SmokeTests\KRDesktopHub.Platform.Windows.SmokeTests.csproj",
        ".\tests\KRDesktopHub.WidgetRuntime.SmokeTests\KRDesktopHub.WidgetRuntime.SmokeTests.csproj",
        ".\tests\KRDesktopHub.SystemPolicies.SmokeTests\KRDesktopHub.SystemPolicies.SmokeTests.csproj",
        ".\tests\KRDesktopHub.DiagnosticsMigration.SmokeTests\KRDesktopHub.DiagnosticsMigration.SmokeTests.csproj"
    )

    foreach ($Project in $SmokeTests) {
        Write-Host "Running: $Project"

        & $DotNetExe run `
            --project $Project `
            --configuration Release `
            --no-build

        Assert-LastExitCode "Smoke test failed: $Project"
    }

    Write-Host "`n=== Release validation 3. Publish self-contained win-x64 app ===" -ForegroundColor Cyan

    & $DotNetExe publish `
        ".\src\KRDesktopHub.App.Windows\KRDesktopHub.App.Windows.csproj" `
        --configuration Release `
        --runtime win-x64 `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:DebugType=None `
        -p:DebugSymbols=false `
        --output $PublishRoot

    Assert-LastExitCode "Self-contained win-x64 publish failed."

    $PublishedExe =
        Join-Path `
            $PublishRoot `
            "KRDesktopHub.App.Windows.exe"

    if (-not (Test-Path -LiteralPath $PublishedExe -PathType Leaf)) {
        throw "Published executable was not found: $PublishedExe"
    }

    Write-Host "`n=== Release validation 4. Assemble portable folder ===" -ForegroundColor Cyan

    $AppStage =
        Join-Path `
            $StageRoot `
            "app"

    Copy-DirectoryContents `
        -Source $PublishRoot `
        -Destination $AppStage

    Copy-Item `
        -LiteralPath (Join-Path $RepoRoot "resources") `
        -Destination $StageRoot `
        -Recurse `
        -Force

    $ConfigStage =
        Join-Path `
            $StageRoot `
            "config\examples"

    New-Item `
        -ItemType Directory `
        -Force `
        -Path $ConfigStage |
        Out-Null

    Get-ChildItem `
        -LiteralPath (Join-Path $RepoRoot "config") `
        -File `
        -Filter "*.example.json" |
        ForEach-Object {
            Copy-Item `
                -LiteralPath $_.FullName `
                -Destination $ConfigStage `
                -Force
        }

    $HelloStage =
        Join-Path `
            $StageRoot `
            "plugins\samples\HelloWidget"

    $HelloBinStage =
        Join-Path `
            $HelloStage `
            "bin\Release\net10.0"

    New-Item `
        -ItemType Directory `
        -Force `
        -Path $HelloBinStage |
        Out-Null

    Copy-Item `
        -LiteralPath (Join-Path $RepoRoot "samples\HelloWidget\manifest.json") `
        -Destination $HelloStage `
        -Force

    Get-ChildItem `
        -LiteralPath (Join-Path $RepoRoot "samples\HelloWidget\bin\Release\net10.0") `
        -File |
        ForEach-Object {
            Copy-Item `
                -LiteralPath $_.FullName `
                -Destination $HelloBinStage `
                -Force
        }

    $DocsStage =
        Join-Path `
            $StageRoot `
            "docs"

    New-Item `
        -ItemType Directory `
        -Force `
        -Path $DocsStage |
        Out-Null

    Copy-Item `
        -LiteralPath (Join-Path $RepoRoot "docs\release\KR_Desktop_Hub_CoreHost_Portable_RC1_Release_Notes.md") `
        -Destination $DocsStage `
        -Force

    Copy-Item `
        -LiteralPath (Join-Path $RepoRoot "docs\release\KR_Desktop_Hub_CoreHost_Portable_Manual_Acceptance_Checklist.md") `
        -Destination $DocsStage `
        -Force

    Set-Content `
        -LiteralPath (Join-Path $StageRoot "START_KR_DESKTOP_HUB.cmd") `
        -Encoding Ascii `
        -Value @(
            "@echo off",
            "start `"`" `"%~dp0app\KRDesktopHub.App.Windows.exe`""
        )

    Set-Content `
        -LiteralPath (Join-Path $StageRoot "RUN_SELF_TEST.cmd") `
        -Encoding Ascii `
        -Value @(
            "@echo off",
            "set MARKER=%TEMP%\KRDesktopHub-self-test-%RANDOM%.json",
            "start /wait `"`" `"%~dp0app\KRDesktopHub.App.Windows.exe`" --self-test-marker `"%MARKER%`"",
            "type `"%MARKER%`"",
            "del `"%MARKER%`""
        )

    $SourceRevision =
        (git rev-parse HEAD).Trim()

    Assert-LastExitCode "Unable to read source revision."

    $Manifest =
        [ordered]@{
            schema_version =
                1

            product =
                "KR Desktop Hub CoreHost"

            version =
                $Version

            runtime =
                "win-x64"

            deployment_mode =
                "self-contained-single-file"

            source_revision =
                $SourceRevision

            built_at_utc =
                [DateTimeOffset]::UtcNow.ToString("o")

            entrypoint =
                "app/KRDesktopHub.App.Windows.exe"

            launchers =
                @(
                    "START_KR_DESKTOP_HUB.cmd",
                    "RUN_SELF_TEST.cmd"
                )

            included_samples =
                @(
                    "plugins/samples/HelloWidget"
                )
        }

    Write-Utf8File `
        -Path (Join-Path $StageRoot "release-manifest.json") `
        -Content (
            $Manifest |
                ConvertTo-Json `
                    -Depth 8
        )

    Write-Host "`n=== Release validation 5. Enforce package whitelist ===" -ForegroundColor Cyan

    $AllowedRootEntries = @(
        "app",
        "config",
        "docs",
        "plugins",
        "resources",
        "release-manifest.json",
        "RUN_SELF_TEST.cmd",
        "START_KR_DESKTOP_HUB.cmd"
    )

    $ActualRootEntries =
        @(
            Get-ChildItem `
                -LiteralPath $StageRoot `
                -Force |
                Select-Object `
                    -ExpandProperty Name
        )

    $UnexpectedRootEntries =
        @(
            $ActualRootEntries |
                Where-Object {
                    $AllowedRootEntries -notcontains $_
                }
        )

    $MissingRootEntries =
        @(
            $AllowedRootEntries |
                Where-Object {
                    $ActualRootEntries -notcontains $_
                }
        )

    if ($UnexpectedRootEntries.Count -gt 0) {
        $UnexpectedRootEntries |
            ForEach-Object {
                Write-Host $_ -ForegroundColor Red
            }

        throw "Unexpected root entry detected in portable package."
    }

    if ($MissingRootEntries.Count -gt 0) {
        $MissingRootEntries |
            ForEach-Object {
                Write-Host $_ -ForegroundColor Red
            }

        throw "Required root entry is missing from portable package."
    }

    if (@(
        Get-ChildItem `
            -LiteralPath $StageRoot `
            -Recurse `
            -Force |
            Where-Object {
                $_.FullName -match "owner_private_docs"
            }
    ).Count -gt 0) {
        throw "Private directory content was detected in portable package."
    }

    Write-Host "Portable-package whitelist passed." -ForegroundColor Green

    Write-Host "`n=== Release validation 6. Create ZIP and SHA-256 ===" -ForegroundColor Cyan

    if (Test-Path -LiteralPath $ZipPath) {
        Remove-Item `
            -LiteralPath $ZipPath `
            -Force
    }

    if (Test-Path -LiteralPath $HashPath) {
        Remove-Item `
            -LiteralPath $HashPath `
            -Force
    }

    [System.IO.Compression.ZipFile]::CreateFromDirectory(
        $StageRoot,
        $ZipPath,
        [System.IO.Compression.CompressionLevel]::Optimal,
        $false)

    $Hash =
        (
            Get-FileHash `
                -LiteralPath $ZipPath `
                -Algorithm SHA256
        ).Hash.ToLower()

    Set-Content `
        -LiteralPath $HashPath `
        -Encoding Ascii `
        -Value "$Hash  $ZipName"

    Write-Host "ZIP:"
    Write-Host $ZipPath

    Write-Host "SHA-256:"
    Write-Host $Hash

    Write-Host "`n=== Release validation 7. Clean extraction fixture ===" -ForegroundColor Cyan

    if (Test-Path -LiteralPath $FixtureRoot) {
        Remove-Item `
            -LiteralPath $FixtureRoot `
            -Recurse `
            -Force
    }

    New-Item `
        -ItemType Directory `
        -Force `
        -Path $FixtureRoot |
        Out-Null

    [System.IO.Compression.ZipFile]::ExtractToDirectory(
        $ZipPath,
        $FixtureRoot)

    $ExtractedExe =
        Join-Path `
            $FixtureRoot `
            "app\KRDesktopHub.App.Windows.exe"

    if (-not (Test-Path -LiteralPath $ExtractedExe -PathType Leaf)) {
        throw "Extracted executable is missing."
    }

    if (-not (Test-Path -LiteralPath (Join-Path $FixtureRoot "START_KR_DESKTOP_HUB.cmd") -PathType Leaf)) {
        throw "Extracted launcher is missing."
    }

    Write-Host "`n=== Release validation 8. Execute extracted self-test ===" -ForegroundColor Cyan

    $MarkerPath =
        Join-Path `
            $WorkRoot `
            "self-test-marker.json"

    $SelfTestProcess =
        Start-Process `
            -FilePath $ExtractedExe `
            -ArgumentList @(
                "--self-test-marker",
                "`"$MarkerPath`""
            ) `
            -PassThru

    if (-not $SelfTestProcess.WaitForExit(
        20000)) {
        Stop-Process `
            -Id $SelfTestProcess.Id `
            -Force `
            -ErrorAction SilentlyContinue

        throw "Extracted self-test process did not exit within 20 seconds."
    }

    if ($SelfTestProcess.ExitCode -ne 0) {
        throw "Extracted self-test process returned a non-zero exit code."
    }

    if (-not (Test-Path -LiteralPath $MarkerPath -PathType Leaf)) {
        throw "Extracted self-test marker is missing."
    }

    $Marker =
        Get-Content `
            -LiteralPath $MarkerPath `
            -Raw |
            ConvertFrom-Json

    if ($Marker.Status -ne "PASS") {
        throw "Extracted self-test marker did not report PASS."
    }

    if ($Marker.Architecture -ne "X64") {
        throw "Unexpected extracted self-test architecture: $($Marker.Architecture)"
    }

    Write-Host "Extracted self-test passed." -ForegroundColor Green

    Write-Host "`n=== Release validation 9. Launch hidden tray host and sample baseline ===" -ForegroundColor Cyan

    $script:RuntimeProcess =
        Start-Process `
            -FilePath $ExtractedExe `
            -ArgumentList @(
                "--start-hidden"
            ) `
            -PassThru

    Start-Sleep `
        -Seconds 4

    if ($script:RuntimeProcess.HasExited) {
        throw "Hidden tray host exited during startup."
    }

    $Samples =
        @()

    $PreviousCpu =
        $script:RuntimeProcess.TotalProcessorTime.TotalMilliseconds

    $PreviousTime =
        Get-Date

    for ($Index = 0;
        $Index -lt 8;
        $Index++) {
        Start-Sleep `
            -Seconds 1

        $script:RuntimeProcess.Refresh()

        if ($script:RuntimeProcess.HasExited) {
            throw "Hidden tray host exited during resource sampling."
        }

        $Now =
            Get-Date

        $CurrentCpu =
            $script:RuntimeProcess.TotalProcessorTime.TotalMilliseconds

        $ElapsedMilliseconds =
            ($Now - $PreviousTime).TotalMilliseconds

        $CpuPercent =
            if ($ElapsedMilliseconds -le 0) {
                0
            }
            else {
                (($CurrentCpu - $PreviousCpu) / $ElapsedMilliseconds / [Environment]::ProcessorCount * 100)
            }

        $Samples +=
            [pscustomobject]@{
                sampled_at_utc =
                    [DateTimeOffset]::UtcNow.ToString("o")

                cpu_percent =
                    [Math]::Round(
                        [Math]::Max(
                            0,
                            $CpuPercent),

                        4)

                working_set_bytes =
                    $script:RuntimeProcess.WorkingSet64
            }

        $PreviousCpu =
            $CurrentCpu

        $PreviousTime =
            $Now
    }

    Stop-ValidationProcess

    $CpuValues =
        @(
            $Samples |
                Select-Object `
                    -ExpandProperty cpu_percent
        )

    $MemoryValues =
        @(
            $Samples |
                Select-Object `
                    -ExpandProperty working_set_bytes
        )

    $Baseline =
        [ordered]@{
            schema_version =
                1

            product =
                "KR Desktop Hub CoreHost"

            version =
                $Version

            runtime =
                "win-x64"

            source_revision =
                $SourceRevision

            measured_at_utc =
                [DateTimeOffset]::UtcNow.ToString("o")

            sample_count =
                $Samples.Count

            warmup_seconds =
                4

            sample_interval_seconds =
                1

            cpu_percent_average =
                [Math]::Round(
                    (
                        $CpuValues |
                            Measure-Object `
                                -Average
                    ).Average,

                    4)

            cpu_percent_maximum =
                [Math]::Round(
                    (
                        $CpuValues |
                            Measure-Object `
                                -Maximum
                    ).Maximum,

                    4)

            working_set_bytes_average =
                [Math]::Round(
                    (
                        $MemoryValues |
                            Measure-Object `
                                -Average
                    ).Average,

                    0)

            working_set_bytes_maximum =
                (
                    $MemoryValues |
                        Measure-Object `
                            -Maximum
                ).Maximum

            acceptance_thresholds_frozen =
                $false

            note =
                "Proof-of-Concept baseline only. Freeze acceptance thresholds after repeated measurements."
        }

    Write-Utf8File `
        -Path $BaselinePath `
        -Content (
            $Baseline |
                ConvertTo-Json `
                    -Depth 8
        )

    Write-Host "Resource baseline:"
    Write-Host $BaselinePath

    Write-Host (
        "Average CPU: {0}% | Maximum CPU: {1}% | Maximum working set: {2:N0} bytes" `
            -f `
            $Baseline.cpu_percent_average,
            $Baseline.cpu_percent_maximum,
            $Baseline.working_set_bytes_maximum
    )

    Write-Host "`n=== Release validation 10. Confirm repository remains clean ===" -ForegroundColor Cyan

    $StatusAfter =
        @(
            git status --porcelain
        )

    Assert-LastExitCode "Unable to read Git status after release generation."

    if ($StatusAfter.Count -gt 0) {
        $StatusAfter |
            ForEach-Object {
                Write-Host $_
            }

        throw "Repository is not clean after release-candidate generation."
    }

    Write-Host "`n=== Portable release candidate complete ===" -ForegroundColor Green
    Write-Host "ZIP:      $ZipPath"
    Write-Host "SHA-256:  $Hash"
    Write-Host "Baseline: $BaselinePath"
}
finally {
    Stop-ValidationProcess

    if (Test-Path -LiteralPath $WorkRoot) {
        Remove-Item `
            -LiteralPath $WorkRoot `
            -Recurse `
            -Force `
            -ErrorAction SilentlyContinue
    }
}
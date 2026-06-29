# World Time-Space Widget 0.4.7 — Validation Notes

## Verified before publication package generation

- Source package structure exists.
- Root-level builder exists in the widget folder.
- `manifest.json` package version is `0.4.7`.
- `preferred_expanded_height_dip` is `286`.
- Required capabilities remain `ui.surface`, `height.report`, and `network.read`.
- Map asset exists.
- Contract snapshot exists.
- Final map layout tokens are present:
  - `MapPanelTopMarginDip = 6.0`
  - `MapPanelHeightDip = 221.0`
  - root bottom padding = `2 DIP`
  - single-bottom-border strategy
  - centered vertical map crop
- No direct `HttpClient` use in the widget source.

## Not verified in this publication package

- Windows WPF live rendering.
- CoreHost install behavior.
- Runtime visual confirmation after publication.

The committed screenshot captures the Owner-observed map-mode UI in KR Desktop Hub 2.6.3.

# Changelog

All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.1] - 2026-08-22

### Changed

- The Add Component window now opens where Unity places it, rather than directly beneath the **+** button.
- Adding a component from a locked Inspector now selects that object first, so the component lands on the object the bar is showing rather than on the selection.

### Fixed

- Entering Play mode with **Reload Domain** disabled no longer leaves stale state behind, so the bar, the component clipboard and chip dragging all behave as they do on a fresh domain.
- The copy button no longer looks usable when only the Transform is picked. It is disabled, and says why.
- **Paste Rotation** now also accepts a rotation copied from the Inspector's own Rotation field, not only one copied from a Transform.
- The copy and paste icons now resolve wherever the package is installed, including as a plain folder under `Assets/`, rather than only from `Packages/com.andreleandrodev.componentry`.

## [1.0.0] - 2026-08-11

Initial release.

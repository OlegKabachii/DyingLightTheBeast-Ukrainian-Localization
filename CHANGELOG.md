# Changelog

All notable changes follow [Semantic Versioning](https://semver.org/).

## [1.0.1] - 2026-08-10

### Fixed

- Changed the optional installer to consume the same `ph_ft` layout as the manual
  Required Data archive, so both ZIP files can be extracted into one folder.
- Corrected Ukrainian `Є/є` rendering across all 17 Rajdhani font definitions.
- Fixed font-script parsing so each `Char(1028)` is associated with the
  `Texture(...)` active in its own section instead of the last texture in the file.
- Rebuilt the font payload from a clean baseline, removing unintended changes to
  unrelated atlas regions from earlier experiments.
- Changed public versioning to Semantic Versioning (`MAJOR.MINOR.PATCH`).

## [1.0.0] - 2026-08-10

### Added

- Initial Ukrainian localization release.
- Transparent x64 WinForms installer with install, update, verify, and uninstall.
- SHA-256 verification and reversible backup handling.

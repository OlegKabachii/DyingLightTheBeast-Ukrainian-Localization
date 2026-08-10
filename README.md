# Dying Light: The Beast — Ukrainian Localization Installer

Open-source installer for the Ukrainian localization of **Dying Light: The Beast**.

**Author:** Oleg Kabachii  
**Release:** 1.0.1
**Nexus Mods:** [Mod 964](https://www.nexusmods.com/dyinglightthebeast/mods/964)

## Purpose

This repository contains the complete source code used to build
`UkrainianLocalizationInstaller.exe`. The installer provides a small WinForms UI
for locating the game, installing or updating the localization, verifying installed
files, and restoring the previous files during uninstall.

The localization payload (`.pak` and `.rpack`) is intentionally not stored here:

- it contains no executable code;
- the largest file exceeds GitHub's 100 MB file limit;
- it is distributed separately through the Nexus Mods release.

## Security properties

- normal x64 .NET Framework WinForms executable;
- runs as the current user (`asInvoker`);
- no PowerShell, CMD, VBS, shell, or subprocess execution;
- no network communication;
- no registry writes (Steam paths are read only for game discovery);
- no drivers, services, scheduled tasks, startup entries, or system components;
- no UPX, obfuscation, executable packing, self-extraction, or embedded archives;
- only writes localization and backup files inside the selected game directory.

See [SECURITY.md](SECURITY.md) for the complete behavior and threat-surface summary.

## Build

The published v1.0.1 installer was built with the 64-bit Microsoft .NET Framework C#
compiler included with Windows. No third-party build tools are required.

```bat
build.cmd
```

Detailed prerequisites, the exact compiler invocation, and verification steps are
documented in [BUILD.md](BUILD.md).

## Source layout

```text
src/
  Installer.cs        Complete installer logic and WinForms UI
  app.manifest        Explicit asInvoker execution manifest
build.cmd              Transparent local build command
BUILD.md               Reproducible build instructions
SECURITY.md            Security behavior and boundaries
SHA256SUMS.txt         Published release binary/archive hashes
LICENSE                Source-code license and third-party notice
CHANGELOG.md           Semantic Versioning release history
tools/                 Auditable font-atlas correction utility
```

## Usage

The compiled installer expects the release payload in a sibling `data` directory:

```text
UkrainianLocalizationInstaller.exe
data/
  data0.pak
  dataen.pak
  gui_common_pc.rpack
```

Keep the Steam game language set to **English** and close the game before install,
update, or uninstall operations.

## Versioning

This project follows Semantic Versioning. `1.0.1` is a patch release correcting
Ukrainian `Є/є` glyph selection without changing gameplay behavior or localization
scope.

## Disclaimer

This is an independent community localization project. It is not affiliated with,
endorsed by, or sponsored by Techland. Dying Light, Dying Light: The Beast, and all
original game assets and trademarks belong to their respective rights holders.

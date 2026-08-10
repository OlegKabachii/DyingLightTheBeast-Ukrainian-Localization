# Build instructions

These instructions build the complete installer application from source.

## Requirements

- Windows 10 or Windows 11 x64
- Microsoft .NET Framework 4.x compiler at:
  `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`
- No NuGet packages, SDK downloads, packers, or third-party build tools

## One-command build

From the repository root, run:

```bat
build.cmd
```

Output:

```text
build\UkrainianLocalizationInstaller.exe
```

## Exact compiler command

```bat
"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" ^
  /nologo ^
  /target:winexe ^
  /platform:x64 ^
  /optimize+ ^
  /debug- ^
  /win32manifest:src\app.manifest ^
  /reference:System.dll ^
  /reference:System.Core.dll ^
  /reference:System.Drawing.dll ^
  /reference:System.Windows.Forms.dll ^
  /reference:Microsoft.CSharp.dll ^
  /out:build\UkrainianLocalizationInstaller.exe ^
  src\Installer.cs
```

## Verification

Inspect the resulting manifest:

```powershell
Select-String -Path .\src\app.manifest -Pattern asInvoker
```

Calculate the binary hash:

```powershell
Get-FileHash .\build\UkrainianLocalizationInstaller.exe -Algorithm SHA256
```

The v1.0.1 binary distributed in the release ZIP has SHA-256:

```text
b7d6181866f000536afb3369d39a4e603d0a008b389f4748970e68f976764dc6
```

The legacy .NET Framework compiler may generate a new module-version identifier on
each compilation, so a clean rebuild can have a different SHA-256 while remaining
source-equivalent. Reviewers can compare behavior, metadata, imports, and source.

## Packaging

The executable is copied next to the separately distributed `data` directory. Do
not embed payload files, create a self-extracting archive, or apply executable
compression. The public release itself is a normal ZIP archive.

## Font-atlas utility

`tools/build_font_atlas.py` documents the exact context-aware glyph correction.
It requires Python 3 and Pillow and operates on locally supplied matching font
definitions and a clean `gui_common_pc.rpack` baseline:

```text
python tools/build_font_atlas.py ^
  --font-dir path\to\gui\common_pc ^
  --baseline-rpack path\to\gui_common_pc.rpack ^
  --output gui_common_pc.fixed.rpack
```

No original game assets are stored in this repository.

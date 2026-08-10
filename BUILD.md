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

The v2 binary distributed in the reviewed ZIP has SHA-256:

```text
e12c44d96036db423c512650978842595b07ee7f275580611a1aceb0fac10877
```

The legacy .NET Framework compiler may generate a new module-version identifier on
each compilation, so a clean rebuild can have a different SHA-256 while remaining
source-equivalent. Reviewers can compare behavior, metadata, imports, and source.

## Packaging

The executable is copied next to the separately distributed `data` directory. Do
not embed payload files, create a self-extracting archive, or apply executable
compression. The public release itself is a normal ZIP archive.

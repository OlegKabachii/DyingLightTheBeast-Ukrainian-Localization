@echo off
setlocal
set "CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" (
  echo ERROR: Microsoft .NET Framework x64 C# compiler was not found.
  exit /b 1
)
if not exist build mkdir build
"%CSC%" /nologo /target:winexe /platform:x64 /optimize+ /debug- /win32manifest:src\app.manifest /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:Microsoft.CSharp.dll /out:build\UkrainianLocalizationInstaller.exe src\Installer.cs
if errorlevel 1 exit /b %errorlevel%
echo Built: build\UkrainianLocalizationInstaller.exe

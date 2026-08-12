# Manual installation

The Steam game language must remain set to **English**. Close the game before
installing, updating, or removing the localization.

## Install

1. Download `DyingLightTheBeast_Ukrainian_v1.0.1_Manual.zip` from Nexus Mods.
2. Extract the ZIP to a normal folder.
3. In Steam, right-click **Dying Light: The Beast** and select
   **Manage > Browse local files**.
4. Copy the extracted `ph_ft` folder into the game root, merging it with the
   existing `ph_ft` folder.
5. Allow Windows to replace files if prompted, then launch the game through Steam.

The manual package installs exactly these files:

```text
ph_ft/work/data0.pak
ph_ft/work/data_lang/dataen.pak
ph_ft/work/data_platform/pc/assets/gui_common_pc.rpack
```

## Update

Close the game and repeat the installation steps using the newest manual ZIP.

## Verify

Compare the files against `SHA256SUMS.txt`, which is included in the ZIP and
published in this repository.

## Uninstall

Close the game and delete only the three files listed above. Do not delete the
entire `ph_ft` directory. Steam file verification can be run afterwards if needed.

## Security

The manual package contains only three PAK/RPACK localization payload files. It
contains no executable, PowerShell, CMD, VBS, service, driver, scheduled task,
startup entry, or system-level component.

The graphical installer will remain a separate optional download after its
security review is complete.

## Optional installer

The optional installer uses this same Required Data package. Extract the Required
Data ZIP and the optional Installer ZIP into the same folder so that
`UkrainianLocalizationInstaller.exe` is next to `ph_ft`, then run the installer.
No second copy or rearrangement of the payload is required.

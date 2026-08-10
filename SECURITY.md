# Security and behavior

## Supported security reports

Please use the repository's GitHub Issues page for non-sensitive findings. For a
sensitive vulnerability, contact the author privately before publishing details.

## Execution model

The application is a small x64 .NET Framework WinForms program. Its manifest uses:

```xml
<requestedExecutionLevel level="asInvoker" uiAccess="false" />
```

It does not request elevation or attempt to bypass Windows access controls. A user
must already have permission to write to the selected game directory.

## Files read

- sibling release files under `data`;
- the selected Dying Light: The Beast directory;
- existing localization override files for backup and verification;
- Steam installation paths and `libraryfolders.vdf` for automatic discovery;
- read-only Steam registry values used only for discovery.

## Files written

Only the following paths inside the user-selected game directory are managed:

```text
ph_ft\work\data0.pak
ph_ft\work\data_lang\dataen.pak
ph_ft\work\data_platform\pc\assets\gui_common_pc.rpack
UkrainianLocalizationBackup_v2\
```

Temporary copy files use the `.ua_v2_tmp` suffix beside their final target. Paths
are canonicalized and checked to remain under the selected game directory.

## Backup and uninstall

Before the first installation, any existing target files are copied to the visible
`UkrainianLocalizationBackup_v2` directory. An existing backup state is preserved
during updates. Uninstall restores files that existed before installation and
removes override files that did not previously exist.

## Explicitly absent behavior

The installer does not:

- execute PowerShell, CMD, VBS, scripts, or child processes;
- access the Internet or open sockets;
- download or upload data;
- write to the Windows registry;
- install drivers or services;
- create scheduled tasks or startup entries;
- write to Windows or other system directories;
- use UPX, obfuscation, packers, self-extraction, or embedded archives;
- modify files outside the selected game directory and its own release directory.

## Payload integrity

Before installation, each payload file's size and SHA-256 are checked against
constants embedded in the reviewed source. Installed files can be verified from the
UI using the same SHA-256 values.

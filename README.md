# Variant Mod Organizer 1.3.0 — source review package

This package contains the C# / WPF organizer application, Inno Setup installer definition, build script, and existing regression tests. It was copied from the local 1.3.0 development project. Application source is unchanged; the build script was corrected to publish into the folder consumed by setup.iss and to locate an explicitly supplied or installed Inno Setup compiler.

## Build on Windows x64

Install the .NET 10 SDK (https://dotnet.microsoft.com/download/dotnet/10.0) and Inno Setup 6 (https://jrsoftware.org/isinfo.php). Validation used .NET SDK 10.0.400. Internet access is required for initial .NET restore and the test suite: NetworkTests.cs also downloads the current public mod release into a temporary test fixture. Run from this package directory:

```powershell
dotnet run --project .\DovaLocksAppTests\Tests.csproj -c Release
powershell -NoProfile -ExecutionPolicy Bypass -File .\DovaLocksApp\Build-Installer.ps1 -Compiler "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
```

Adjust the compiler path to your installation. The process publishes the application to DovaLocksApp/Ready-Organizer and creates app-release/Variant-Mod-Organizer-Installer.exe. It does not run the app or install it. No signing certificate or publishing credentials are required. The result is unsigned.

## What to review

- DovaLocksApp/setup.iss: per-user installation, shortcuts, uninstallation and optional Open With registration for save files.
- InstallCore.cs and OrganizerCore.cs: game discovery, mod installation, import, backup, removal and enabled/disabled loadouts.
- ReleaseClient.cs: GitHub release metadata, download size and SHA-256 checks. The latest mod is fetched from VariantCreator/Dova-Locks. This does not independently establish that a release is benign or protect against a compromised publishing account.
- MainWindow.xaml.cs and App.xaml.cs: UI actions, Steam launch, background local log watcher and command-line entry points.
- LockSaveEditor.cs: save-file editing and backup/rollback handling.
- DovaOutputLog.cs and UnrealCrashReport.cs: local diagnostics processing.
- DovaLocksAppTests: existing isolated-fixture tests, including mocked network responses and a live GitHub download check.

## Included assets and scope

Resources/Dova.ico is the application icon. Resources/Dova-Locks_P.pak is a compiled mod payload embedded by the project. Its original Unreal project/source is NOT included here. This is application and installer source, not a complete source release of the embedded mod. Reviewers may require that separately.

No SDK, installer compiler, build output, personal logs, real player saves, credentials, or original installer is included in the source ZIP. The .NET runtime is bundled into the rebuilt app by dotnet publish; its upstream licenses apply. No new open-source license is granted by this preparation step. The owner must select the intended license and confirm asset rights before describing this as an open-source release.

## Relationship to the uploaded release

The existing local app-release installer has the same SHA-256 as the Downloads installer previously scanned by Microsoft Defender:
E0C87ED43E5C216C37B11E6D35C1719E4EED1FA0489B56B72BD89D6EF2F75C67

This links those two existing binaries, not a proof that this source recreates the uploaded executable byte-for-byte. Toolchain versions, generated timestamps and packaging can change rebuilt bytes. See VALIDATION.md for the actual verification performed. Defender reported no threats for the original downloaded installer; that is not a full security audit or a result for the rebuild.

The tests include two synthetic Unreal save fixtures with Example Owner/Example Friend data and their generator script. Tests resolve these beside the test executable and write edited copies only into temporary test directories.


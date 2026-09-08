# Variant Mod Organizer

An ICARUS mod organizer by Dova. Less folder juggling, more prospecting.

**Current version: 1.4.0. Includes Dova Locks 1.0.7.**

[Download the Windows installer](https://github.com/VariantCreator/Variant-Mod-Organizer/releases/latest/download/Variant-Mod-Organizer-Installer.exe) · [Nexus page](https://www.nexusmods.com/icarus/mods/329) · [Dova Locks](https://github.com/VariantCreator/Dova-Locks) · [Variant Interactive Map](https://variantinteractivemap.org)

## What you can do

- Import mods and move them between Enabled and Disabled lists with arrows or drag and drop.
- Launch modded or vanilla ICARUS through Steam, with a confirmation before launch.
- Install or update Dova Locks with file checks and backups.
- Manage lock saves and collect useful lock events with DovaOutPut.
- Open Unreal crash reports to see the available details.

This organizes PAKs; it does not merge mods or automatically fix conflicts between them.

## Getting started

Download the installer above, run it, and open Variant Mod Organizer. Check the ICARUS folder it found before making changes. Close the game before adding, removing or updating mods.

Use **Add / remove mods** to choose your loadout, then **Launch modded** or **Launch vanilla**. Use **Install / Update** for Dova Locks when your server is on the matching version. Installing Dova Locks is optional.

## What's new in 1.4.0

- Includes Dova Locks 1.0.7 and keeps the included version when GitHub has an older release.
- DovaOutPut diagnostics and a viewer for Unreal crash reports.
- More room in the diagnostics window so the text can breathe.

Windows 10 (1809 or newer) or Windows 11, 64-bit. The installer includes the app runtime. Steam and ICARUS are required to play, and update checks need internet access.

## Build

Requires Windows, [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), and [Inno Setup 6](https://jrsoftware.org/isinfo.php).

Run from the repository folder:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\DovaLocksApp\Build-Installer.ps1 -Compiler "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
```

Adjust the compiler path if needed. The installer is created in `app-release`.

## Tests

```powershell
dotnet run --project .\DovaLocksAppTests\Tests.csproj -c Release
```

The tests require internet access for the GitHub download check.

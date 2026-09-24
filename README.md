# Variant Mod Organizer

An ICARUS mod organizer by Dova. Less folder juggling, more prospecting.

**Version 1.6. Includes Dova Locks 1.1.**

[Download the Windows installer](https://github.com/VariantCreator/Variant-Mod-Organizer/releases/latest/download/Variant-Mod-Organizer-Installer.exe) · [Nexus page](https://www.nexusmods.com/icarus/mods/329) · [Dova Locks](https://github.com/VariantCreator/Dova-Locks) · [Variant Interactive Map](https://variantinteractivemap.org)

## What's new

- Edit lock-save JSON inside the app: choose a save, Edit, then Apply. Both A/B saves are backed up and written together.
- Active locks show by default. Show inactive locks brings back older records without deleting them.
- Fixed valid saves being rejected as an unknown lock because of old links.
- Includes the current Dova Locks 1.1 PAK. Same-version mod updates now check the GitHub file too.

Less file juggling. Slightly less opportunity to yell at a folder.

## Use it

Run the installer, check the ICARUS folder it found, and open **Add / remove mods**. Move PAKs between Enabled and Disabled with the arrows or drag and drop. **Launch modded** and **Launch vanilla** both go through Steam and ask before launching.

Close ICARUS before changing mods. This app organizes PAKs; it doesn't merge mods or magically make conflicts get along.

Use **Install / Update** for Dova Locks and **Update organizer** for the app itself. Downloads are checked against GitHub's file hash before use. Organizer updates keep your install location, mods and lock saves.

## Lock saves

Choose your `.sav`, open **Edit JSON**, click **Edit**, then **Apply**. The app validates it and backs up both A/B saves before writing. Import and export are also available.

Active means a PIN and owner exist in the selected save. This is not a live server query. Inactive records stay preserved when hidden.

Stop the world or server first. For a remote server, download both saves and upload both edited replacements afterward. Editing files on your PC doesn't change someone else's server.

## Diagnostics

DovaOutPut collects useful lock events locally. **Open game log** starts in the ICARUS Logs folder; **Open crash report** starts in Crashes for your Windows account. For a hosted server, download its log first.

Logs are normally under `%LOCALAPPDATA%/Icarus/Saved/Logs`, crashes under `%LOCALAPPDATA%/Icarus/Saved/Crashes`, and collected output under `%LOCALAPPDATA%/VariantModOrganizer/DovaOutPut`.

Windows 10 (1809+) or Windows 11, 64-bit. The installer includes the runtime. Steam and ICARUS are needed to play; update checks need internet.

[Report a problem](https://github.com/VariantCreator/Variant-Mod-Organizer/issues) with the app version and what happened. Keep PINs and private server details out of screenshots.

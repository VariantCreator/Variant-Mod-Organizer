# Variant Mod Organizer

An ICARUS mod organizer by Dova. **Version 1.6 beta, provided as-is.**

[Windows installer](https://github.com/VariantCreator/Variant-Mod-Organizer/releases/latest/download/Variant-Mod-Organizer-Installer.exe) · [Portable ZIP](https://github.com/VariantCreator/Variant-Mod-Organizer/releases/download/v1.6.0/Variant-Mod-Organizer-1.6-beta-portable.zip) · [Release notes](https://github.com/VariantCreator/Variant-Mod-Organizer/releases/tag/v1.6.0) · [Nexus page](https://www.nexusmods.com/icarus/mods/329)

## What's new

- Dova Locks is no longer bundled or downloaded automatically.
- Import IMM merged PAKs and choose which original mods to disable.
- Optionally merge local mods with conflict review.
- Edit lock saves with search, undo, a change review, and backups of both saves.
- Choose the installer or a portable ZIP.

Already using 1.6? Download and run the updated installer manually. This build keeps version 1.6, so the in-app update check will not offer it as a newer version.

## Use it

Run the installer, or extract the whole portable ZIP and open Variant-Mod-Organizer.exe. Choose your game or server folder, then open **Manage mods**. Close ICARUS and IMM before changing mods. Modded and vanilla launches use Steam.

Dova Locks is a separate [download](https://github.com/VariantCreator/Dova-Locks). Import its local PAK when you want it. Organizer updates are checked only when you choose them.

## Merge mods

Choose **Merge mods**, add local EXMOD, EXMODZ, mod ZIPs or supported PAKs, arrange the order, and click **Analyze**. Review conflicts, build a merged PAK, then import it. Select only the originals included in that merge to move them to Disabled. Use the same PAK on the server and clients.

This beta supports standard IMM row changes and version 11 PAKs using no compression, zlib or gzip. Oodle, encrypted and older PAKs need an EXMODZ alternative. The limit is 512 MB of unpacked content. Conflicting compiled assets need a compatibility patch; Blueprint code cannot be combined. Arrays are treated as complete values. Raw full-table mods are compared with current game data; prefer EXMODZ after game updates.

## Edit lock saves

Keep the world's A and B saves together and open either one. Search for an owner, player or lock, select **Edit this lock**, and review changes before saving. Undo and discard affect pending changes. Saving backs up both originals and refuses to overwrite saves changed elsewhere.

Stop the game or server first. For a hosted server, download both saves and upload both edited replacements before restarting. JSON exports contain PINs and Steam IDs; keep them private.

IMM-merged Dova Locks uses the same save editor. Merging mods does not combine worlds or saves.

## Diagnostics and requirements

DovaOutPut collects lock events locally when you start it. Open game logs or Unreal crash reports from Diagnostics. For a hosted server, download its logs first.

Windows 10 (1809+) or Windows 11, 64-bit. The runtime is included.

[Report a problem](https://github.com/VariantCreator/Variant-Mod-Organizer/issues) · [Variant Interactive Map](https://variantinteractivemap.org)

Keep PINs and private server details out of screenshots.

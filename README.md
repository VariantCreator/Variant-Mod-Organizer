# Variant Mod Organizer 1.7 beta

By Dova. Beta, provided as-is.

Manage local ICARUS PAK mods, import IMM merges, build a merged PAK, and edit Dova Locks saves.

Extract the whole portable ZIP and run Variant-Mod-Organizer.exe, or use the installer. Windows 10 (1809+) or Windows 11, 64-bit. The runtime is included.

Close ICARUS and IMM before changing mods. Choose your game or server folder, then open Manage mods. Dova Locks is not bundled or downloaded automatically. Organizer updates are checked only when you choose them.

## Merge mods

Choose Merge mods, add local EXMOD, EXMODZ, mod ZIPs or supported PAK files, arrange the order, and click Analyze. If a matching compatibility patch is available, click **Auto patch**, then build. Review any remaining conflicts first. Import the finished PAK and select only the originals included in it to move them to Disabled. Use the same merged PAK on the server and clients.

This beta supports standard IMM row changes and version 11 PAKs using no compression, zlib or gzip. Oodle, encrypted and older PAKs need an EXMODZ alternative. Merges are limited to 512 MB of unpacked content. Auto patch supports author-provided compatibility patches for any mod. Dova Locks 1.1.6 + Pickup and Move 1.0 is recognized out of the box, in either order. It uses the files you selected and checks their versions by file hash; it does not download mods or invent missing Blueprint fixes. Unresolved conflicts block the normal Build button. **Merge anyway** is a separate option: after a warning, the later mod's files win. This can break features or protection; it does not combine Blueprint behavior. Arrays are treated as complete values. Different values require your choice of the later mod. Raw full-table mods are compared with the current game data; prefer EXMODZ after game updates.

## Edit saves

Keep the world's A and B saves together, then open either one. Search for an owner, player or lock. Select Edit this lock to change ownership, PIN or player access. Undo this lock or Discard all edits restores pending changes.

Review and save writes both slots and backs up both originals. Stop the game or server first. Upload both edited files to a hosted server before restarting it. JSON imports are staged for review; they do not save immediately. JSON exports contain PINs and Steam IDs, so keep them private.

IMM-merged Dova Locks uses the same save editor. Merging PAKs does not combine worlds or save files.

[Downloads and support](https://github.com/VariantCreator/Variant-Mod-Organizer)
[Variant website](https://variantinteractivemap.org)

[Compatibility patch format for mod authors](COMPATIBILITY.md)

# Compatibility patches

Auto patch works with any mod author’s prepared compatibility files. It applies a matching patch; it cannot combine arbitrary Blueprint code.

Dova Locks 1.1.6 with Pickup and Move 1.0 is recognized automatically. Select both mods, analyze, and click **Auto patch**. The original mod files stay untouched.

## For mod authors

Ship your compiled replacement assets in a PAK with an EXMOD descriptor, or in an EXMODZ. Add a `VariantAssetPatches` object to that descriptor. Normal EXMOD data changes still belong in `Rows`.

- `Schema`: `1`.
- `Patches`: an array of patch definitions.
- Each definition has `Name`, `Files`, and optional `Requires`.
- Each `Files` entry has `Path`, `Sha256`, and `Replaces`.
- `Path` is relative to `Icarus/Content`, with forward slashes.
- `Sha256` is the lowercase SHA-256 of the replacement file included in your package.
- `Replaces` is an array of SHA-256 values for the original file revisions your patch supports.
- Each `Requires` entry has `Path` and `Sha256` for a dependency that must be present in the finished merge. Dependencies may come from another selected mod.

Keep related `.uasset` and `.uexp` files together. Include other changed companions too. If a companion stays unchanged, list its expected hash in `Requires`.

The Organizer checks every conflicting revision against the patch’s supported hashes, verifies the replacement bytes and dependencies, and leaves unrelated conflicts alone. Changed inputs require another analysis. Conflicting data values still require the user’s choice.

If two matching patches propose different replacements for the same file, Auto patch will not choose between them. Select the correct patch for your mod list. After updating a patch, update its file hashes as well.

This metadata describes your compiled compatibility files. Include player instructions and credits, and keep your project source out of the download.

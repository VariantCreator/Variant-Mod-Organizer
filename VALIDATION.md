# Validation — 2026-09-08

- Windows x64; .NET SDK 10.0.400; Inno Setup 6.7.3.
- Existing regression executable completed with exit code 0. It covers installation and rollback, unrelated-file preservation, mod activation, ZIP traversal rejection, save roundtrips and edits, diagnostics, malformed crash reports, mocked downloads, and a live public release download into a temporary fixture.
- Full application publish and installer compilation completed with exit code 0.
- Rebuilt installer SHA-256: 73D404D2D9DD66AC84767D029D27DC8F19E1DBE006D0F027833983DED23D0CDC.
- Original release installer SHA-256: E0C87ED43E5C216C37B11E6D35C1719E4EED1FA0489B56B72BD89D6EF2F75C67. The original release-folder installer and Downloads copy match each other. The rebuild differs; byte-for-byte reproduction of the original is not established.
- A targeted credential-pattern scan found no matches in included text source. This is not a comprehensive security audit.
- Application C#, XAML, project definition, installer definition, manifest, and resources were preserved. Build script changes fix the publish directory and compiler discovery. Test changes make fixture paths independent of the working directory and remove an external edited-fixture write.
- Synthetic save fixtures and their Unreal generator were included. No real player saves are included.
- Installer was compiled, not launched. Fresh installation, GUI behavior, SmartScreen, and antivirus results for this rebuild were not tested.
- Source ZIP intentionally excludes EXEs, bin/obj directories, runtime bundles and compiler binaries. The compiled embedded mod PAK remains required for the current project; its Unreal source is outside this package.

This package is ready to share for source review subject to the owner's rights to included assets. Published to VariantCreator/Variant-Mod-Organizer for source review at the owner's request. No license grant, email, or Nexus submission has been performed.


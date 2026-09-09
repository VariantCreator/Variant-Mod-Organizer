# Publishing updates

## Organizer

Build the Windows installer with the build script. Create a stable release in VariantCreator/Variant-Mod-Organizer using a tag such as v1.5.0, attach Variant-Mod-Organizer-Installer.exe, add the change notes, and mark it latest. The app version and release tag must agree.

Players on 1.5 or newer choose Update organizer. The app verifies the installer against GitHub's SHA256 digest, then runs setup in its existing location. Setup reopens the organizer when finished. Players on older versions install 1.5 once to get this button.

## Dova Locks

Publish a stable release in VariantCreator/Dova-Locks, attach Dova-Locks_P.pak and mark it latest. Players use Check mod update, then Install / Update. Keep the server and players on the same mod version.

The app has no publishing credentials. Drafts, prereleases and files from other repositories are rejected. Keep GitHub write access limited to trusted accounts.

Uninstalling the organizer leaves game mods and saves alone. The installer is unsigned; antivirus and SmartScreen results are decided by their providers.

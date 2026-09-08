# Variant Mod Organizer

Application and Windows installer source for version 1.3.0.

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

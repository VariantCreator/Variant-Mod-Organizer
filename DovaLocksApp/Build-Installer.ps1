param([string]$Compiler)
$ErrorActionPreference = 'Stop'
if (-not $Compiler) {
    $found = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($found) { $Compiler = $found.Source }
    else {
        $candidates = @("${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe", "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe")
        $Compiler = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    }
}
if (-not $Compiler -or -not (Test-Path -LiteralPath $Compiler)) {
    throw 'Install Inno Setup 6 and pass -Compiler with the full path to ISCC.exe.'
}
Get-Command dotnet -ErrorAction Stop | Out-Null
$publish = Join-Path $PSScriptRoot 'Ready-Organizer'
dotnet publish (Join-Path $PSScriptRoot 'DovaLocksInstaller.csproj') -c Release -r win-x64 --self-contained true -o $publish
if ($LASTEXITCODE -ne 0) { throw 'App build failed' }
if (-not (Test-Path -LiteralPath (Join-Path $publish 'Variant-Mod-Organizer.exe'))) { throw 'Published app missing' }
& $Compiler (Join-Path $PSScriptRoot 'setup.iss')
if ($LASTEXITCODE -ne 0) { throw 'Windows setup build failed' }
Get-FileHash -LiteralPath (Join-Path $PSScriptRoot '../app-release/Variant-Mod-Organizer-Installer.exe') -Algorithm SHA256

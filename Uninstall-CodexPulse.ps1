$ErrorActionPreference = 'Stop'
$installDirectory = Join-Path $env:LOCALAPPDATA 'CodexPulse'
$startMenuShortcut = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\Codex Pulse.lnk'
$startupShortcut = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\Startup\Codex Pulse.lnk'
$appPathsSubKey = 'Software\Microsoft\Windows\CurrentVersion\App Paths\CodexPulse.exe'

Get-Process CodexPulse -ErrorAction SilentlyContinue | Stop-Process -Force
foreach ($path in @($startMenuShortcut, $startupShortcut)) {
    if (Test-Path -LiteralPath $path) {
        Remove-Item -LiteralPath $path -Force
    }
}
$appPathsParent = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey('Software\Microsoft\Windows\CurrentVersion\App Paths', $true)
if ($null -ne $appPathsParent) {
    $appPathsParent.DeleteSubKeyTree('CodexPulse.exe', $false)
    $appPathsParent.Dispose()
}
if (Test-Path -LiteralPath $installDirectory) {
    Remove-Item -LiteralPath $installDirectory -Recurse -Force
}

Write-Host 'Codex Pulse uninstalled.'

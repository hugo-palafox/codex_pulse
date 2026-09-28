param(
    [switch]$StartWithWindows
)

$ErrorActionPreference = 'Stop'
$installDirectory = Join-Path $env:LOCALAPPDATA 'CodexPulse'
$startupDirectory = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\Startup'
$startMenuDirectory = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs'
$sourceExecutable = Join-Path $PSScriptRoot 'CodexPulse.exe'
$installedExecutable = Join-Path $installDirectory 'CodexPulse.exe'
$appPathsSubKey = 'Software\Microsoft\Windows\CurrentVersion\App Paths\CodexPulse.exe'

if (-not (Test-Path -LiteralPath $sourceExecutable)) {
    throw "CodexPulse.exe was not found next to this installer."
}

New-Item -ItemType Directory -Path $installDirectory -Force | Out-Null
Copy-Item -LiteralPath $sourceExecutable -Destination $installedExecutable -Force

$shell = New-Object -ComObject WScript.Shell
$startMenuShortcut = $shell.CreateShortcut((Join-Path $startMenuDirectory 'Codex Pulse.lnk'))
$startMenuShortcut.TargetPath = $installedExecutable
$startMenuShortcut.WorkingDirectory = $installDirectory
$startMenuShortcut.Description = 'Codex usage limits overlay'
$startMenuShortcut.Save()

$appPathsKey = [Microsoft.Win32.Registry]::CurrentUser.CreateSubKey($appPathsSubKey)
$appPathsKey.SetValue('', $installedExecutable)
$appPathsKey.SetValue('Path', $installDirectory)
$appPathsKey.Dispose()

$startupShortcutPath = Join-Path $startupDirectory 'Codex Pulse.lnk'
if ($StartWithWindows) {
    $startupShortcut = $shell.CreateShortcut($startupShortcutPath)
    $startupShortcut.TargetPath = $installedExecutable
    $startupShortcut.WorkingDirectory = $installDirectory
    $startupShortcut.Description = 'Start Codex Pulse with Windows'
    $startupShortcut.Save()
} elseif (Test-Path -LiteralPath $startupShortcutPath) {
    Remove-Item -LiteralPath $startupShortcutPath -Force
}

Start-Process -FilePath $installedExecutable
Write-Host "Codex Pulse installed to $installDirectory"
Write-Host 'Run command registered: CodexPulse'
if ($StartWithWindows) {
    Write-Host 'Startup launch enabled.'
} else {
    Write-Host 'Startup launch is disabled. Use -StartWithWindows to enable it.'
}

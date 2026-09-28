# Codex Pulse

Codex Pulse is a small native Windows tray utility that displays the current ChatGPT-backed Codex rate-limit windows in an always-on-top liquid-glass overlay.

## Shareable release

Download the `CodexPulse-vX.Y.Z-win-x64.zip` asset from GitHub Releases, extract it, and run `Install-CodexPulse.ps1` from PowerShell.

To install and start Codex Pulse with Windows:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\Install-CodexPulse.ps1 -StartWithWindows
```

Without `-StartWithWindows`, the app installs normally but does not launch automatically at sign-in. Run `Uninstall-CodexPulse.ps1` to remove the app and its shortcuts.

After installation, press `Win+R`, type `CodexPulse`, and press Enter to launch it at any time.

## Requirements

- Windows 10 version 2004 or newer, preferably Windows 11
- The Codex CLI installed and available as `codex` on `PATH`
- A Codex/ChatGPT authentication session supported by the Codex App Server
- The .NET SDK matching the target framework

## Run

```powershell
dotnet run --project .\CodexPulse\CodexPulse.csproj
```

The app starts with a visible overlay and places a tray icon in the notification area. Closing or hiding the overlay leaves the process running. Use the tray menu to show it again or exit.

## Integration notes

The app starts `codex app-server` as a child process and uses its stdio JSON-RPC transport. It calls `account/rateLimits/read` initially and refreshes every 45 seconds. It also listens for `account/rateLimits/updated` notifications.

The app intentionally does not scrape ChatGPT pages or read private browser/Codex token files.

# Installs or updates Messenger Bubbles from the latest GitHub release.
#   Install or update: irm https://raw.githubusercontent.com/umefor-labs/MessengerBubbles/main/install.ps1 | iex
#   Uninstall:         & ([scriptblock]::Create((irm https://raw.githubusercontent.com/umefor-labs/MessengerBubbles/main/install.ps1))) -Uninstall
# Works in Windows PowerShell 5.1 and PowerShell 7. Needs no admin rights: everything goes under the current user.
param(
    [switch]$Uninstall,
    # Download again even when the installed version is already the latest.
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
# Invoke-WebRequest is many times slower in Windows PowerShell while it draws a progress bar.
$ProgressPreference = 'SilentlyContinue'
[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12

$Repo = 'umefor-labs/MessengerBubbles'
# Not %LOCALAPPDATA%\MessengerBubbles: the app keeps its settings and Facebook session there.
$Dir = Join-Path $env:LOCALAPPDATA 'Programs\MessengerBubbles'
$Exe = Join-Path $Dir 'MessengerBubbles.exe'
$Shortcut = Join-Path ([Environment]::GetFolderPath('Programs')) 'Messenger Bubbles.lnk'
$RunKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'

# The app allows one copy at a time, so any running copy has to go before the file can be replaced.
function Stop-App {
    $running = @(Get-Process MessengerBubbles -ErrorAction SilentlyContinue)
    if ($running.Count -eq 0) { return $false }
    Write-Host 'Closing Messenger Bubbles...'
    $running | Stop-Process -Force
    $running | Wait-Process -Timeout 15 -ErrorAction SilentlyContinue
    return $true
}

if ($Uninstall) {
    Stop-App | Out-Null
    Remove-Item $Shortcut -Force -ErrorAction SilentlyContinue
    Remove-Item $Dir -Recurse -Force -ErrorAction SilentlyContinue
    $run = (Get-ItemProperty $RunKey -ErrorAction SilentlyContinue).MessengerBubbles
    if ($run -and $run -like "*$Exe*") { Remove-ItemProperty $RunKey -Name MessengerBubbles }
    Write-Host 'Messenger Bubbles is uninstalled.'
    Write-Host "Settings and the Facebook login are still in $(Join-Path $env:LOCALAPPDATA 'MessengerBubbles'). Delete that folder to remove them too."
    return
}

Write-Host 'Checking the latest version on GitHub...'
$release = Invoke-RestMethod "https://api.github.com/repos/$Repo/releases/latest" -Headers @{ 'User-Agent' = 'MessengerBubbles-installer' }
$asset = $release.assets | Where-Object name -like '*-win-x64.exe' | Select-Object -First 1
if (-not $asset) { throw "Release $($release.tag_name) has no exe yet. It is attached a few minutes after a release is published; try again shortly." }
$latest = [version]($release.tag_name.TrimStart('v'))

if ((Test-Path $Exe) -and -not $Force) {
    $installed = [version](Get-Item $Exe).VersionInfo.FileVersion
    # FileVersion has four parts (1.0.0.0) and the tag three, so compare the first three.
    if ([version]::new($installed.Major, $installed.Minor, $installed.Build) -ge $latest) {
        Write-Host "Messenger Bubbles $latest is already installed and is the latest version."
        return
    }
    Write-Host "Updating from $installed to $latest..."
} else {
    Write-Host "Installing Messenger Bubbles $latest..."
}

New-Item -ItemType Directory -Force $Dir | Out-Null
$download = "$Exe.download"
Write-Host "Downloading $($asset.name) ($([math]::Round($asset.size / 1MB)) MB)..."
Invoke-WebRequest $asset.browser_download_url -OutFile $download -UseBasicParsing
if ((Get-Item $download).Length -ne $asset.size) {
    Remove-Item $download -Force
    throw 'The download was incomplete. Run the command again.'
}

$wasRunning = Stop-App
Move-Item $download $Exe -Force
# Windows marks downloaded files, which makes SmartScreen ask before the first run.
Unblock-File $Exe

$shell = New-Object -ComObject WScript.Shell
$link = $shell.CreateShortcut($Shortcut)
$link.TargetPath = $Exe
$link.WorkingDirectory = $Dir
$link.Save()

Write-Host "Done: $Exe"
Write-Host "Run the same command again later to update."
# The app rewrites its autostart entry with its own path each time it starts, so starting it here
# moves a "Start with Windows" entry from an old copy elsewhere to this one.
Start-Process $Exe
if (-not $wasRunning) { Write-Host 'Messenger Bubbles is starting; look for its icon in the system tray.' }

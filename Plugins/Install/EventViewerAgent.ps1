$ErrorActionPreference = "Stop"
Set-Variable ProgressPreference SilentlyContinue

$InstallDir = "C:\Apps\EventViewer.Agent"
$ZipName = "EventViewer.Agent.zip"
$Urls = @(
    "https://cloud.it-guards.at/download/eventviewer/agent"
    "http://cloud.it-guards.at/download/eventviewer/agent"
    "https://nockal.com/download/eventviewer/agent"
    "http://nockal.com/download/eventviewer/agent"
)

Write-Host ""
Write-Host "=== EventViewer Agent - Installer ===" -ForegroundColor Cyan
Write-Host ""

# ZIP herunterladen (URLs durchprobieren)
$zipPath = Join-Path $env:TEMP $ZipName
$downloadUrl = $null

Write-Host "[1/4] Lade Agent herunter..."
foreach ($baseUrl in $Urls) {
    $url = "$baseUrl/$ZipName"
    try {
        [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12
        $wc = New-Object System.Net.WebClient
        $wc.DownloadFile($url, $zipPath)
        $downloadUrl = $baseUrl
        Write-Host "      OK: $url" -ForegroundColor Green
        break
    } catch {
        Write-Host "      $url - nicht erreichbar" -ForegroundColor DarkGray
    }
}

if (-not $downloadUrl) {
    Write-Host "FEHLER: Kein Download-Server erreichbar." -ForegroundColor Red
    exit 1
}

# Zielordner erstellen/leeren
Write-Host "[2/4] Entpacke nach $InstallDir..."
if (Test-Path $InstallDir) {
    # Bestehende appsettings.json und agent.key sichern
    $backupFiles = @()
    foreach ($file in @("appsettings.json", "agent.key")) {
        $filePath = Join-Path $InstallDir $file
        if (Test-Path $filePath) {
            $backupPath = Join-Path $env:TEMP "eventviewer_backup_$file"
            Copy-Item $filePath $backupPath -Force
            $backupFiles += $file
            Write-Host "      Gesichert: $file" -ForegroundColor DarkGray
        }
    }

    # Ordner leeren
    Get-ChildItem $InstallDir -Recurse | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
} else {
    New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
    $backupFiles = @()
}

# Entpacken
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::ExtractToDirectory($zipPath, $InstallDir)
Write-Host "      OK" -ForegroundColor Green

# Gesicherte Dateien wiederherstellen
Write-Host "[3/4] Konfiguration..."
foreach ($file in $backupFiles) {
    $backupPath = Join-Path $env:TEMP "eventviewer_backup_$file"
    $targetPath = Join-Path $InstallDir $file
    Copy-Item $backupPath $targetPath -Force
    Remove-Item $backupPath -Force
    Write-Host "      Wiederhergestellt: $file" -ForegroundColor DarkGray
}

# Version.txt vom Server holen
try {
    $versionUrl = "$downloadUrl/version.txt"
    $version = (New-Object System.Net.WebClient).DownloadString($versionUrl).Trim()
    $versionPath = Join-Path $InstallDir "version.txt"
    [System.IO.File]::WriteAllText($versionPath, $version)
    Write-Host "      Version: $version" -ForegroundColor DarkGray
} catch {
    Write-Host "      Version konnte nicht abgerufen werden" -ForegroundColor Yellow
}

# Temp ZIP aufraeumen
Remove-Item $zipPath -Force -ErrorAction SilentlyContinue

# Agent starten (interaktiver Modus -> Setup)
Write-Host "[4/4] Starte Agent-Setup..."
Write-Host ""

$exePath = Join-Path $InstallDir "EventViewer.Agent.exe"
if (Test-Path $exePath) {
    Start-Process -FilePath $exePath -Verb RunAs -Wait
} else {
    Write-Host "FEHLER: $exePath nicht gefunden." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "=== Installation abgeschlossen ===" -ForegroundColor Cyan
Write-Host ""

<#
.SYNOPSIS
    Meldet einen Benutzer von allen Microsoft-Diensten lokal ab.
.DESCRIPTION
    Entfernt Credentials, Tokens und Cache für:
    - Microsoft 365 / Azure AD
    - Outlook
    - Teams
    - OneDrive
    - Office-Anwendungen
.PARAMETER UserName
    Optional: Benutzername des Profils (Standard: aktueller Benutzer)
#>

param(
    [string]$UserName = $env:USERNAME
)

$ErrorActionPreference = "SilentlyContinue"
Write-Host ""

Write-Host "=== Microsoft Logout Script ===" -ForegroundColor Cyan
Write-Host "Zielbenutzer: $UserName" -ForegroundColor Yellow
Write-Host ""

# 1. Microsoft-Prozesse beenden
Write-Host "[1/6] Beende Microsoft-Prozesse..." -ForegroundColor Green
$processes = @(
    "OUTLOOK", "WINWORD", "EXCEL", "POWERPNT", "ONENOTE", "MSACCESS",
    "Teams", "ms-teams", "OneDrive", "MSOUC", "OfficeClickToRun"
)
foreach ($proc in $processes) {
    Get-Process -Name $proc -ErrorAction SilentlyContinue | Stop-Process -Force
    if ($?) { Write-Host " - $proc beendet" }
}
Write-Host ""

# 2. Credentials aus Windows Credential Manager entfernen
Write-Host "[2/6] Entferne Microsoft-Credentials aus Credential Manager..." -ForegroundColor Green
$credTargets = @(
    "MicrosoftOffice*",
    "Microsoft.AAD*",
    "Microsoft.Office*",
    "OneDrive*",
    "msteams*",
    "Microsoft_OC*",
    "*office*",
    "*outlook*",
    "LegacyGeneric:target=MicrosoftOffice*",
    "WindowsLive:*"
)

foreach ($target in $credTargets) {
    $matches = cmdkey /list | Select-String -Pattern $target -AllMatches
    foreach ($match in $matches) {
        $name = $match.Line
        $name = ($name -replace "\s+Target:\s+", "")
        $name = ($name -replace "\s+Ziel:\s+", "")
        $name = $name.Trim()
        if ($name) {
            cmdkey /delete:"$name" 2>$null
            Write-Host " - Credential entfernt: $name"
        }
    }
}

Write-Host ""

# 3. Token-Cache und Identity-Daten löschen
Write-Host "[3/6] Lösche Token-Cache und Identity-Daten..." -ForegroundColor Green
$userProfile = "C:\Users\$UserName"

$pathsToDelete = @(
    "$userProfile\AppData\Local\Microsoft\TokenBroker",
    "$userProfile\AppData\Local\Microsoft\IdentityCache",
    "$userProfile\AppData\Local\Microsoft\OneAuth",
    "$userProfile\AppData\Local\Microsoft\Office\16.0\Wef",
    "$userProfile\AppData\Local\Microsoft\Office\OTele",
    "$userProfile\AppData\Roaming\Microsoft\Office\Recent",
    "$userProfile\AppData\Local\Packages\Microsoft.AAD.BrokerPlugin_*",
    "$userProfile\AppData\Local\Packages\Microsoft.Windows.CloudExperienceHost_*"
)

foreach ($path in $pathsToDelete) {
    $resolvedPaths = Resolve-Path $path -ErrorAction SilentlyContinue
    foreach ($resolvedPath in $resolvedPaths) {
        if (Test-Path $resolvedPath) {
            Remove-Item -Path $resolvedPath -Recurse -Force -ErrorAction SilentlyContinue
            Write-Host " - Gelöscht: $resolvedPath"
        }
    }
}

Write-Host ""

# 4. Teams-Daten löschen
Write-Host "[4/6] Lösche Teams-Cache und Anmeldedaten..." -ForegroundColor Green
$teamsPaths = @(
    "$userProfile\AppData\Roaming\Microsoft\Teams",
    "$userProfile\AppData\Local\Microsoft\Teams",
    "$userProfile\AppData\Local\Packages\MSTeams_*",
    "$userProfile\AppData\Local\Packages\MicrosoftTeams_*"
)

foreach ($path in $teamsPaths) {
    $resolvedPaths = Resolve-Path $path -ErrorAction SilentlyContinue
    foreach ($resolvedPath in $resolvedPaths) {
        if (Test-Path $resolvedPath) {
            Remove-Item -Path $resolvedPath -Recurse -Force -ErrorAction SilentlyContinue
            Write-Host " - Gelöscht: $resolvedPath"
        }
    }
}

Write-Host ""

# 5. OneDrive abmelden
Write-Host "[5/6] Melde OneDrive ab..." -ForegroundColor Green
$oneDrivePath = "$userProfile\AppData\Local\Microsoft\OneDrive\settings"
if (Test-Path $oneDrivePath) {
    Remove-Item -Path "$oneDrivePath\*" -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host " - OneDrive-Einstellungen gelöscht"
}

# OneDrive über CLI abmelden (falls verfügbar)
$oneDriveExe = "$env:LOCALAPPDATA\Microsoft\OneDrive\OneDrive.exe"
if (Test-Path $oneDriveExe) {
    & $oneDriveExe /shutdown 2>$null
    Write-Host " - OneDrive heruntergefahren"
}

Write-Host ""

# 6. Registry-Einträge bereinigen
Write-Host "[6/6] Bereinige Registry-Einträge..." -ForegroundColor Green
$regPaths = @(
    "HKCU:\Software\Microsoft\Office\16.0\Common\Identity",
    "HKCU:\Software\Microsoft\Office\16.0\Common\Internet",
    "HKCU:\Software\Microsoft\Office\16.0\Common\Licensing",
    "HKCU:\Software\Microsoft\Office\16.0\Common\SignIn"
)

foreach ($regPath in $regPaths) {
    if (Test-Path $regPath) {
        Remove-Item -Path $regPath -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host " - Registry-Pfad gelöscht: $regPath"
    }
}

Write-Host ""

# WAM (Web Account Manager) Reset
Write-Host "[Bonus] WAM-Reset wird durchgeführt..." -ForegroundColor Magenta
$wamPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\AAD"
if (Test-Path $wamPath) {
    Remove-Item -Path $wamPath -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host " - WAM-Daten gelöscht"
}

Write-Host ""

# Abschluss
Write-Host "=== Fertig ===" -ForegroundColor Cyan
Write-Host "Der Benutzer '$UserName' wurde von allen Microsoft-Diensten abgemeldet." -ForegroundColor Green
Write-Host "Hinweis: Bei der nächsten Anmeldung an Office/Teams/OneDrive wird eine neue Authentifizierung benötigt." -ForegroundColor Yellow
Write-Host "Ein Neustart wird empfohlen, um alle Änderungen zu übernehmen." -ForegroundColor Yellow
Write-Host ""

$currentIdentity = [Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
$administratorRole = [Security.Principal.WindowsBuiltInRole]::Administrator

if ($currentIdentity.IsInRole($administratorRole))
{
    Write-Host "Success: Running as administrator" -ForegroundColor green
}
else
{
    Write-Host "Error: Not running as administrator" -ForegroundColor red
    Exit
}

Set-Variable ProgressPreference SilentlyContinue

Install-PackageProvider -Name NuGet -MinimumVersion 2.8.5.201 -Force
Install-Module -Name PSWindowsUpdate -Force
Import-Module PSWindowsUpdate
Get-WindowsUpdate -MicrosoftUpdate -Verbose
Install-WindowsUpdate -MicrosoftUpdate -AcceptAll -IgnoreReboot
Get-WURebootStatus | select RebootRequired, RebootScheduled

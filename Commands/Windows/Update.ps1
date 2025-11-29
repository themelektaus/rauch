Set-Variable ProgressPreference SilentlyContinue

Install-PackageProvider -Name NuGet -MinimumVersion 2.8.5.201 -Force
Install-Module -Name PSWindowsUpdate -Force
Import-Module PSWindowsUpdate
Get-WindowsUpdate -MicrosoftUpdate -Verbose
Install-WindowsUpdate -MicrosoftUpdate -AcceptAll -IgnoreReboot
Get-WURebootStatus | select RebootRequired, RebootScheduled

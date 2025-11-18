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

net stop nxlog
Start-Sleep -seconds 1

sc.exe delete nxlog
Start-Sleep -seconds 1

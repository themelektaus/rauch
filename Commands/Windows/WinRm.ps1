# PsExec
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\system" /v LocalAccountTokenFilterPolicy /t REG_DWORD /d 1 /f
reg add "HKLM\System\CurrentControlSet\Control\Terminal Server" /v AllowRemoteRPC /t REG_DWORD /d 1 /f
reg add "HKLM\System\CurrentControlSet\Control\Terminal Server" /v fDenyTSConnections /t REG_DWORD /d 0 /f

# Script Execution
Set-ExecutionPolicy Bypass -Force

# WinRM
Enable-PSRemoting -Force

# Firewall Rules
$rules = Get-NetFirewallRule
$rules | Where-Object -Property "Name" -Match "WINRM" | Enable-NetFirewallRule
$rules | Where-Object -Property "Name" -Match "NETDIS" | Enable-NetFirewallRule
$rules | Where-Object -Property "Name" -Match "RemoteSvcAdmin" | Enable-NetFirewallRule
$rules | Where-Object -Property "Name" -Match "RemoteFwAdmin" | Enable-NetFirewallRule
$rules | Where-Object -Property "Name" -Match "RemoteDesktop" | Enable-NetFirewallRule

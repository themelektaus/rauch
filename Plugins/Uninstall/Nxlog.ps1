net stop nxlog
Start-Sleep -seconds 1

sc.exe delete nxlog
Start-Sleep -seconds 1

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

Set-Variable ProgressPreference SilentlyContinue

function Set-WorkingDirectory($path)
{
    if (!(Test-Path -PathType Container $path))
    {
        New-Item -ItemType Directory -Path $path
    }
    Set-Location $path
}

$currentLocation = Get-Location

Set-WorkingDirectory "$currentLocation\data\nxlog"

function Download($Url, $Filename, $Force = $False)
{
    if ($Force -or !(Test-Path($Filename)))
    {
        Write-Host -NoNewline "Downloading $Filename... "
        Invoke-WebRequest $Url -OutFile $Filename
        Write-Host "OK"
    }
}

Download "https://cloud.it-guards.at/download/nxlog.zip" "nxlog.zip" $True
tar -xf "nxlog.zip"
del "nxlog.zip"

$content = [System.IO.File]::ReadAllText("$currentLocation\data\nxlog\conf\nxlog.conf")
$content = $content.Replace("C:\Apps\nxlog", "$currentLocation\data\nxlog")
[System.IO.File]::WriteAllText("$currentLocation\data\nxlog\conf\nxlog.conf", $content)

sc.exe create nxlog binPath= "$currentLocation\data\nxlog\nxlog.exe -c $currentLocation\data\nxlog\conf\nxlog.conf" start= auto
Start-Sleep -seconds 1

net start nxlog
Start-Sleep -seconds 1

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

Write-Host

$installed = Get-ItemProperty HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\* |
    Where-Object { $_.DisplayName -like "Microsoft Visual C++ 2022 X64*" }

if ($installed)
{
    Write-Host "Already installed"
    foreach ($i in $installed)
    {
        Write-Host " " $i.DisplayName
    }
}
else
{
    $path = Get-Location
    $path = "$path\data"
    
    if (!(Test-Path -PathType Container $path))
    {
        New-Item -ItemType Directory -Path $path
    }
    Set-Location $path

    function Download($Url, $Filename)
    {
        if (!(Test-Path($Filename)))
        {
            Write-Host -NoNewline "Downloading $Filename... "
            Invoke-WebRequest $Url -OutFile $Filename
            Write-Host "OK"
        }
    }

    Download "https://aka.ms/vs/17/release/vc_redist.x64.exe" "vc_redist.x64.exe"

    Write-Host -NoNewline "Installing... "
    Start-Process -FilePath "vc_redist.x64.exe" -ArgumentList "/quiet /norestart" -Wait
    Write-Host "OK"

    if (Test-Path "vc_redist.x64.exe")
    {
        Remove-Item "vc_redist.x64.exe"
    }
}

Write-Host

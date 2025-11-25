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

Set-WorkingDirectory "$currentLocation\data"

function Download($Url, $Filename, $Force = $False)
{
    if ($Force -or !(Test-Path($Filename)))
    {
        Write-Host -NoNewline "Downloading $Filename... "
        Invoke-WebRequest $Url -OutFile $Filename
        Write-Host "OK"
    }
}

Download "https://cloud.it-guards.at/download/speedtest.exe" "speedtest.exe"

$running = $true

while ($running)
{
    & ./speedtest.exe --accept-license --accept-gdpr

    Write-Host '                      '
    Write-Host '   [Space] Run again  '
    Write-Host '  [Escape] Exit       '
    Write-Host '                      '

    while ($true)
    {
        if ([Console]::KeyAvailable)
        {
            $key = [Console]::ReadKey($true).Key

            if ($key -eq 'Escape')
            {
                $running = $false
                break
            }

            if ($key -eq 'Space')
            {
                Write-Host '  '
                Write-Host '  '
                break
            }
        }
        else
        {
            Start-Sleep -MilliSeconds 100
        }
    }
}

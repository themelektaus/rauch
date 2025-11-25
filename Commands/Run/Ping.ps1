$separator = "-------------------------------------------"

class Host {
    [string] $Name
    [bool] $IsNew
    [bool] $WasOnline
    [bool] $IsOnline
}

$hosts = @()

foreach ($arg in $args)
{
    $_host = [PSCustomObject][Host]::new()
    $_host.Name = $arg
    $_host.IsNew = $true
    $hosts += $_host
}

function Start-AsyncPing($_host)
{
    $hostname = $_host.Name
    $ping = New-Object System.Net.NetworkInformation.Ping
    Unregister-Event -SourceIdentifier "PingCompleted-$hostname" -ErrorAction SilentlyContinue
    Register-ObjectEvent $ping PingCompleted -SourceIdentifier "PingCompleted-$hostname" -MessageData $_host -Action {
        $_host = $Event.MessageData
        $_status = $Event.SourceEventArgs.Reply.Status
        $_host.IsOnline = ($_status -eq "Success")
    } | Out-Null
    $ping.SendPingAsync($hostname, 1900) | Out-Null
}

Write-Host $separator
Write-Host " Press [Escape] to quit "

$running = $true
while ($running)
{
    foreach ($_host in $hosts)
    {
        Start-AsyncPing $_host
    }

    for ($i = 0; $i -lt 20; $i++)
    {
        if ([console]::KeyAvailable)
        {
            $key = [system.console]::readkey($true)

            if ($key.key -eq "Escape")
            {
                $running = $false
                break
            }
        }

        Start-Sleep -Milliseconds 100
    }

    if ($running)
    {
        $dirty = $false

        foreach ($_host in $hosts)
        {
            if ($_host.IsNew -or $_host.WasOnline -ne $_host.IsOnline)
            {
                $dirty = $true
                break
            }
        }

        if (!$dirty)
        {
            continue
        }

        Write-Host $separator
        foreach ($_host in $hosts)
        {
            $_host.IsNew = $false
            $_host.WasOnline = $_host.IsOnline

            $color = &{ If ($_host.IsOnline) { "green" } Else { "red" } }

            Write-Host -ForegroundColor $color "" (
                (Get-Date -Format "[HH:mm:ss]").PadRight(12)
            ) (
                $_host.Name.PadRight(20)
            ) (
                &{ If ($_host.IsOnline) { "     OK" } Else { "OFFLINE" } }
            )
        }
    }
}

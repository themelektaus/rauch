Write-Host ""

Set-Variable ProgressPreference SilentlyContinue

Function IsDotNetRuntimeInstalled
{
    if (Test-Path "$env:programfiles/dotnet/")
    {
        try
        {
            [Collections.Generic.List[string]] $runtimes = dotnet --list-runtimes
                
            foreach ($runtime in $runtimes)
            {
                if ($runtime.StartsWith("Microsoft.NETCore.App 10."))
                {
                    return $True
                }
            }
        }
        catch
        {
            
        }
    }
    
    return $False
}

# Determine installation mode:
#   - Default (admin)        -> system-wide install into C:\ProgramData\Rauch
#                               with ACL "Users:Modify" so EVERY user can run rauch
#                               AND rauch can do its relative-path downloads under
#                               any user account.
#   - With --user (no admin) -> per-user install into %USERPROFILE%\.rauch\bin.
#   - No admin AND no --user -> abort and tell the user how to proceed.
$userMode    = ($args -contains '--user') -or ($args -contains '-user') -or ($args -contains '-User')
$unattended  = ($args -contains '--unattended') -or ($args -contains '-unattended') -or ($args -contains '-Unattended')
$isAdmin     = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin -and -not $userMode)
{
    Write-Host "ERROR: Administrator rights are required for system-wide installation." -ForegroundColor Red
    Write-Host ""
    Write-Host "Either:" -ForegroundColor Yellow
    Write-Host "  1) Re-run this script in an elevated PowerShell, or" -ForegroundColor Yellow
    Write-Host "  2) Install for the current user only by passing --user:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host '     iex "& { $(irm it-guards.at/rauch) } --user"' -ForegroundColor Cyan
    Write-Host ""
    return
}

if ($userMode)
{
    $path = "$env:USERPROFILE\.rauch\bin"
    $pathScope = "User"
    Write-Host "Installing for current user only ($env:USERNAME) [--user]." -ForegroundColor Cyan
}
else
{
    $path = "C:\ProgramData\Rauch"
    $pathScope = "Machine"
    Write-Host "Admin rights detected -> installing system-wide for all users." -ForegroundColor Cyan
}
Write-Host ""

# Create installation directory if it doesn't exist
if (!(Test-Path -PathType Container $path))
{
    Write-Host "Creating installation directory..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $path | Out-Null
    Write-Host "  -> $path" -ForegroundColor Gray
    Write-Host ""
}

# When installing system-wide, grant the built-in Users group Modify rights so
# every user can write into the rauch folder (rauch downloads files relative to
# its own location at runtime). Use the well-known SID *S-1-5-32-545 to stay
# language-independent (works on German/English Windows alike).
if ($isAdmin -and -not $userMode)
{
    Write-Host "Setting ACL (Users: Modify) on $path ..." -ForegroundColor Yellow
    try
    {
        $acl = Get-Acl $path
        $usersSid = New-Object System.Security.Principal.SecurityIdentifier("S-1-5-32-545")
        $rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
            $usersSid,
            "Modify",
            "ContainerInherit,ObjectInherit",
            "None",
            "Allow"
        )
        $acl.SetAccessRule($rule)
        Set-Acl -Path $path -AclObject $acl
        Write-Host "  -> ACL successfully updated" -ForegroundColor Green
        Write-Host ""
    }
    catch
    {
        Write-Host "  -> ACL update failed: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host ""
    }
}

# When installing system-wide, clean up any leftover per-user installations:
#   - delete %USERPROFILE%\.rauch in every user profile
#   - remove %USERPROFILE%\.rauch\bin entries from every user's PATH
# This works for all profiles (loaded or not) by temporarily mounting
# their NTUSER.DAT hive via reg load / reg unload.
if ($isAdmin -and -not $userMode)
{
    Write-Host "Cleaning up per-user rauch installations on this machine..." -ForegroundColor Yellow

    $profiles = Get-CimInstance -ClassName Win32_UserProfile -ErrorAction SilentlyContinue |
        Where-Object { -not $_.Special -and $_.LocalPath -and (Test-Path $_.LocalPath) }

    foreach ($profile in $profiles)
    {
        $localPath = $profile.LocalPath
        $sid       = $profile.SID

        # 1) Delete %USERPROFILE%\.rauch
        $rauchDir = Join-Path $localPath ".rauch"
        if (Test-Path $rauchDir)
        {
            try
            {
                Remove-Item -Path $rauchDir -Recurse -Force -ErrorAction Stop
                Write-Host "  -> Removed $rauchDir" -ForegroundColor Gray
            }
            catch
            {
                Write-Host "  -> Could not remove ${rauchDir}: $($_.Exception.Message)" -ForegroundColor Red
            }
        }

        # 2) Remove .rauch\bin entries from this user's PATH
        $hiveKey    = "HKEY_USERS\$sid"
        $envKey     = "Registry::$hiveKey\Environment"
        $hiveLoaded = Test-Path "Registry::$hiveKey"
        $weLoaded   = $false

        if (-not $hiveLoaded)
        {
            $ntuser = Join-Path $localPath "NTUSER.DAT"
            if (Test-Path $ntuser)
            {
                $null = reg.exe load "$hiveKey" "$ntuser" 2>&1
                if ($LASTEXITCODE -eq 0)
                {
                    $weLoaded = $true
                }
            }
        }

        if (Test-Path $envKey)
        {
            try
            {
                $userPath = (Get-ItemProperty -Path $envKey -Name "Path" -ErrorAction SilentlyContinue).Path
                if ($userPath)
                {
                    $entries = $userPath -split ';' | Where-Object {
                        $_ -and ($_.Trim().TrimEnd('\') -notmatch '(?i)\\\.rauch(\\bin)?$')
                    }
                    $newPath = ($entries -join ';').Trim(';')
                    if ($newPath -ne $userPath)
                    {
                        Set-ItemProperty -Path $envKey -Name "Path" -Value $newPath
                        Write-Host "  -> Cleaned PATH for SID $sid" -ForegroundColor Gray
                    }
                }
            }
            catch
            {
                Write-Host "  -> PATH cleanup failed for SID ${sid}: $($_.Exception.Message)" -ForegroundColor Red
            }
        }

        if ($weLoaded)
        {
            # Force GC so the registry handles are released before unloading
            [gc]::Collect()
            [gc]::WaitForPendingFinalizers()
            $null = reg.exe unload "$hiveKey" 2>&1
        }
    }

    Write-Host "  -> Cleanup done" -ForegroundColor Green
    Write-Host ""
}

Set-Location $path

if (!(IsDotNetRuntimeInstalled))
{
    if (!(Test-Path -PathType Container "data"))
    {
        Write-Host "Creating data directory..." -ForegroundColor Yellow
        New-Item -ItemType Directory -Path "data" | Out-Null
        Write-Host "  -> $path\data" -ForegroundColor Gray
        Write-Host ""
    }
    
    Write-Host "Downloading .NET 10 (Runtime) ..." -ForegroundColor Yellow
    try
    {
        Invoke-WebRequest "https://cloud.it-guards.at/download/dotnet-runtime-10.0.0-win-x64.exe" -OutFile "data\dotnet-runtime-10.0.0-win-x64.exe"
        Write-Host "  -> Download successful" -ForegroundColor Green
        Write-Host ""
    }
    catch
    {
        Write-Host "  -> Download error: $($_.Exception.Message)" -ForegroundColor Red
        return
    }

    Write-Host "Installing .NET 10 (Runtime) ..." -ForegroundColor Yellow
    try
    {
        Start-Process -Wait "data\dotnet-runtime-10.0.0-win-x64.exe" -ArgumentList "/install /quiet /norestart"
        Write-Host "  -> Installation successful" -ForegroundColor Green
        Write-Host ""
    }
    catch
    {
        Write-Host "  -> Installation error: $($_.Exception.Message)" -ForegroundColor Red
        return
    }
}

# Download rauch.exe
Write-Host "Downloading rauch..." -ForegroundColor Yellow
try
{
    Invoke-WebRequest "https://raw.githubusercontent.com/themelektaus/rauch/main/Build/Windows/rauch.exe" -OutFile "rauch.exe"
    Write-Host "  -> Download successful" -ForegroundColor Green
    Write-Host ""
}
catch
{
    Write-Host "  -> Download error: $($_.Exception.Message)" -ForegroundColor Red
    return
}

# Add to PATH (Machine when admin, User otherwise) if not exists
$currentPath = [Environment]::GetEnvironmentVariable("Path", $pathScope)

if ($currentPath -notlike "*$path*")
{
    Write-Host "Adding rauch to $pathScope PATH environment variable..." -ForegroundColor Yellow
    [Environment]::SetEnvironmentVariable("Path", "$currentPath;$path", $pathScope)
    Write-Host "  -> PATH successfully updated" -ForegroundColor Green
    Write-Host ""
    Write-Host "IMPORTANT: Please restart your console for 'rauch' to be available everywhere." -ForegroundColor Yellow
}
else
{
    Write-Host "rauch is already in $pathScope PATH." -ForegroundColor Green
}

Write-Host ""

# Update & Launch rauch
if (-not $unattended)
{
    cmd /c rauch update
    cmd /k rauch
}

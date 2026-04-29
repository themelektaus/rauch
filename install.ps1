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
$userMode = ($args -contains '--user') -or ($args -contains '-user') -or ($args -contains '-User')
$isAdmin  = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin -and -not $userMode)
{
    Write-Host "ERROR: Administrator rights are required for system-wide installation." -ForegroundColor Red
    Write-Host ""
    Write-Host "Either:" -ForegroundColor Yellow
    Write-Host "  1) Re-run this script in an elevated PowerShell, or" -ForegroundColor Yellow
    Write-Host "  2) Install for the current user only by passing --user:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host '     iex "& { $(irm https://raw.githubusercontent.com/themelektaus/rauch/main/install.ps1) } --user"' -ForegroundColor Cyan
    Write-Host ""
    exit 1
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
        exit 3
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
        exit 2
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
    exit 1
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

# Launch rauch
cmd /c rauch update
cmd /k rauch

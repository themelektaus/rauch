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

$path = "$env:USERPROFILE\.rauch\bin"

# Create installation directory if it doesn't exist
if (!(Test-Path -PathType Container $path))
{
    Write-Host "Creating installation directory..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $path | Out-Null
    Write-Host "  -> $path" -ForegroundColor Gray
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

# Add to PATH if not exists
$currentUserPath = [Environment]::GetEnvironmentVariable("Path", "User")

if ($currentUserPath -notlike "*$path*")
{
    Write-Host "Adding rauch to PATH environment variable..." -ForegroundColor Yellow
    [Environment]::SetEnvironmentVariable("Path", "$currentUserPath;$path", "User")
    Write-Host "  -> PATH successfully updated" -ForegroundColor Green
    Write-Host ""
    Write-Host "IMPORTANT: Please restart your console for 'rauch' to be available everywhere." -ForegroundColor Yellow
}
else
{
    Write-Host "rauch is already in PATH." -ForegroundColor Green
}

Write-Host ""

# Launch rauch
cmd /c rauch update
cmd /k rauch

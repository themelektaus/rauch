Set-Variable ProgressPreference SilentlyContinue

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

# Download rauch.exe
Write-Host "Downloading rauch..." -ForegroundColor Yellow
try {
    Invoke-WebRequest "https://raw.githubusercontent.com/themelektaus/rauch/main/Build/Windows/rauch.exe" -OutFile "rauch.exe"
    Write-Host "  -> Download successful" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "  -> Download error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Installation aborted." -ForegroundColor Red
    pause
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

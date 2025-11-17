# PNG to ICO Converter
# Converts Logo.png to Logo.ico with multiple sizes (256x256, 128x128, 64x64, 48x48, 32x32, 16x16)

param(
    [string]$InputPng = "Logo.png",
    [string]$OutputIco = "Logo.ico"
)

Add-Type -AssemblyName System.Drawing

if (-not (Test-Path $InputPng)) {
    Write-Host "Error: Input file '$InputPng' not found!" -ForegroundColor Red
    exit 1
}

try {
    # Load the PNG image
    $sourcePng = [System.Drawing.Image]::FromFile((Resolve-Path $InputPng).Path)

    Write-Host "Loading image: $InputPng" -ForegroundColor Cyan
    Write-Host "Original size: $($sourcePng.Width)x$($sourcePng.Height)" -ForegroundColor Gray

    # ICO file sizes to include (Windows standard icon sizes)
    $iconSizes = @(256, 128, 64, 48, 32, 16)

    # Create a memory stream for the ICO file
    $icoStream = New-Object System.IO.MemoryStream
    $writer = New-Object System.IO.BinaryWriter($icoStream)

    # ICO Header
    $writer.Write([UInt16]0)           # Reserved, must be 0
    $writer.Write([UInt16]1)           # Type: 1 = ICO
    $writer.Write([UInt16]$iconSizes.Count)  # Number of images

    # Prepare image data for each size
    $imageData = @()
    $offset = 6 + ($iconSizes.Count * 16)  # Header size + directory entries

    foreach ($size in $iconSizes) {
        # Create resized bitmap
        $bitmap = New-Object System.Drawing.Bitmap($size, $size)
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality

        # Draw resized image
        $graphics.DrawImage($sourcePng, 0, 0, $size, $size)
        $graphics.Dispose()

        # Save to PNG in memory
        $pngStream = New-Object System.IO.MemoryStream
        $bitmap.Save($pngStream, [System.Drawing.Imaging.ImageFormat]::Png)
        $pngBytes = $pngStream.ToArray()
        $pngStream.Dispose()
        $bitmap.Dispose()

        # Store image data
        $imageData += @{
            Size = $size
            Data = $pngBytes
            Length = $pngBytes.Length
            Offset = $offset
        }

        $offset += $pngBytes.Length

        Write-Host "  Created ${size}x${size} icon ($($pngBytes.Length) bytes)" -ForegroundColor Green
    }

    # Write directory entries
    foreach ($img in $imageData) {
        $width = if ($img.Size -eq 256) { 0 } else { $img.Size }   # 0 means 256
        $height = if ($img.Size -eq 256) { 0 } else { $img.Size }

        $writer.Write([byte]$width)           # Width (0 = 256)
        $writer.Write([byte]$height)          # Height (0 = 256)
        $writer.Write([byte]0)                # Color palette (0 = no palette)
        $writer.Write([byte]0)                # Reserved
        $writer.Write([UInt16]1)              # Color planes
        $writer.Write([UInt16]32)             # Bits per pixel
        $writer.Write([UInt32]$img.Length)    # Image data size
        $writer.Write([UInt32]$img.Offset)    # Offset to image data
    }

    # Write image data
    foreach ($img in $imageData) {
        $writer.Write($img.Data)
    }

    # Save to file
    $writer.Flush()
    $icoBytes = $icoStream.ToArray()
    [System.IO.File]::WriteAllBytes((Join-Path (Get-Location) $OutputIco), $icoBytes)

    # Cleanup
    $writer.Dispose()
    $icoStream.Dispose()
    $sourcePng.Dispose()

    Write-Host "`nSuccessfully created $OutputIco" -ForegroundColor Green
    Write-Host "Total size: $($icoBytes.Length) bytes" -ForegroundColor Cyan
    Write-Host "Included icon sizes: $($iconSizes -join ', ')" -ForegroundColor Cyan
}
catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host $_.ScriptStackTrace -ForegroundColor DarkRed
    exit 1
}

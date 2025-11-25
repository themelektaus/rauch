param(
    [string]$SourceFolder,
    [string]$OutputFile
)

Add-Type -AssemblyName System.IO.Compression.FileSystem

Remove-Item $OutputFile -Force -ErrorAction SilentlyContinue
$zip = [System.IO.Compression.ZipFile]::Open($OutputFile, "Create")

Get-ChildItem -Path $SourceFolder -Recurse -Include *.cs, *.ps1 | ForEach-Object {
    $entryName = $_.FullName.Substring($SourceFolder.Length + 1)
    [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
        $zip, $_.FullName, $entryName, "Optimal"
    ) | Out-Null
}

$zip.Dispose()

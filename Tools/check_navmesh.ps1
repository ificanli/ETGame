$path = 'Packages/cn.etetet.map/Bundles/Recast/SDCMap.bytes'
$bytes = [System.IO.File]::ReadAllBytes($path)
Write-Host "File size: $($bytes.Length) bytes"

$magic = [System.BitConverter]::ToInt32($bytes, 0)
$version = [System.BitConverter]::ToInt32($bytes, 4)
$numTiles = [System.BitConverter]::ToInt32($bytes, 8)

Write-Host "Magic: 0x$($magic.ToString('X8'))"
Write-Host "Version: $version"
Write-Host "NumTiles: $numTiles"

# Check magic bytes raw
Write-Host "First 16 bytes (hex):"
$hex = ($bytes[0..15] | ForEach-Object { $_.ToString('X2') }) -join ' '
Write-Host $hex
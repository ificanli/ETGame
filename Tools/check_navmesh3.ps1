$path = 'Packages/cn.etetet.map/Bundles/Recast/SDCMap.bytes'
$bytes = [System.IO.File]::ReadAllBytes($path)

Write-Host "=== offset 36-80 (hex + int32 view) ==="
for ($i = 36; $i -le 80; $i += 4) {
    $val32 = [System.BitConverter]::ToInt32($bytes, $i)
    $valf = [System.BitConverter]::ToSingle($bytes, $i)
    $hex = ($bytes[$i..($i+3)] | ForEach-Object { $_.ToString('X2') }) -join ' '
    Write-Host "offset $i : $hex  | int32=$val32  float=$($valf.ToString('F4'))"
}

Write-Host ""
Write-Host "=== Check if file is 64-bit tileRef format ==="
# In 64-bit format: tileRef is 8 bytes at offset 40
$tileRef64 = [System.BitConverter]::ToInt64($bytes, 40)
$dataSize64 = [System.BitConverter]::ToInt32($bytes, 48)
$padding = [System.BitConverter]::ToInt32($bytes, 52)  # cCompatibility padding
Write-Host "64-bit read: tileRef=$tileRef64, dataSize=$dataSize64, padding=$padding"
Write-Host "MeshData at offset 56:"
$o = 56
$dmMagic = [System.BitConverter]::ToInt32($bytes, $o)
$dmVersion = [System.BitConverter]::ToInt32($bytes, $o+4)
$dmPolyCount = [System.BitConverter]::ToInt32($bytes, $o+24)
$dmVertCount = [System.BitConverter]::ToInt32($bytes, $o+28)
Write-Host "  magic=0x$($dmMagic.ToString('X8')), version=$dmVersion, polyCount=$dmPolyCount, vertCount=$dmVertCount"

Write-Host ""
Write-Host "=== Check 32-bit without cCompatibility padding ==="
# tileRef32 at 40 (4 bytes), dataSize at 44 (4 bytes), MeshData at 48
$o2 = 48
$dmMagic2 = [System.BitConverter]::ToInt32($bytes, $o2)
$dmVersion2 = [System.BitConverter]::ToInt32($bytes, $o2+4)
$dmPolyCount2 = [System.BitConverter]::ToInt32($bytes, $o2+24)
$dmVertCount2 = [System.BitConverter]::ToInt32($bytes, $o2+28)
Write-Host "  offset 48 magic=0x$($dmMagic2.ToString('X8')), version=$dmVersion2, polyCount=$dmPolyCount2, vertCount=$dmVertCount2"
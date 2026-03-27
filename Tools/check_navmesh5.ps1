$path = 'Packages/cn.etetet.map/Bundles/Recast/SDCMap.bytes'
$bytes = [System.IO.File]::ReadAllBytes($path)

# MeshData at offset 56
# Header fields (each 4 bytes):
# 0:magic 1:version 2:x 3:y 4:layer 5:userId 6:polyCount 7:vertCount
# 8:maxLinkCount 9:detailMeshCount 10:detailVertCount 11:detailTriCount
# 12:bvNodeCount 13:offMeshConCount 14:offMeshBase
# 15:walkableHeight(float) 16:walkableRadius(float) 17:walkableClimb(float)
# 18-20:bmin(3 floats) 21-23:bmax(3 floats)

$base = 56
$bminOffset = $base + 15*4 + 3*4  # skip 15 ints + 3 floats = 18 fields = 72 bytes
# actually: offsets in MeshHeader bytes:
# fields 0-14 are int (15*4=60 bytes)
# fields 15-17 are float (3*4=12 bytes)
# total before bmin = 72 bytes
$bminX = [System.BitConverter]::ToSingle($bytes, $base + 72)
$bminY = [System.BitConverter]::ToSingle($bytes, $base + 76)
$bminZ = [System.BitConverter]::ToSingle($bytes, $base + 80)
$bmaxX = [System.BitConverter]::ToSingle($bytes, $base + 84)
$bmaxY = [System.BitConverter]::ToSingle($bytes, $base + 88)
$bmaxZ = [System.BitConverter]::ToSingle($bytes, $base + 92)
Write-Host "NavMesh tile bmin (nav space): ($bminX, $bminY, $bminZ)"
Write-Host "NavMesh tile bmax (nav space): ($bmaxX, $bmaxY, $bmaxZ)"
# With navXSign=-1: unity X = -nav X
Write-Host "Unity space bmin (navXSign=-1): ($(-$bminX), $bminY, $bminZ)"
Write-Host "Unity space bmax (navXSign=-1): ($(-$bmaxX), $bmaxY, $bmaxZ)"
Write-Host "Unity space bmin (navXSign=+1): ($bminX, $bminY, $bminZ)"
Write-Host "Unity space bmax (navXSign=+1): ($bmaxX, $bmaxY, $bmaxZ)"

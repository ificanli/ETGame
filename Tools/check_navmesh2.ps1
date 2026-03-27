$path = 'Packages/cn.etetet.map/Bundles/Recast/SDCMap.bytes'
$bytes = [System.IO.File]::ReadAllBytes($path)

# Header:
# 0: magic (4)
# 4: version (4)
# 8: numTiles (4)
# 12: DtNavMeshParams (origin 12, tileWidth 4, tileHeight 4, maxTiles 4, maxPolys 4) = 28 bytes
# offset 12: originX, originY, originZ (3 floats = 12 bytes)
# offset 24: tileWidth (4)
# offset 28: tileHeight (4)
# offset 32: maxTiles (4)
# offset 36: maxPolys (4)
# Total header = 40 bytes

$numTiles = [System.BitConverter]::ToInt32($bytes, 8)
Write-Host "NumTiles: $numTiles"

$originX = [System.BitConverter]::ToSingle($bytes, 12)
$originY = [System.BitConverter]::ToSingle($bytes, 16)
$originZ = [System.BitConverter]::ToSingle($bytes, 20)
$tileWidth = [System.BitConverter]::ToSingle($bytes, 24)
$tileHeight = [System.BitConverter]::ToSingle($bytes, 28)
$maxTiles = [System.BitConverter]::ToInt32($bytes, 32)
$maxPolys = [System.BitConverter]::ToInt32($bytes, 36)
Write-Host "NavMeshParams: origin=($originX, $originY, $originZ), tileWidth=$tileWidth, tileHeight=$tileHeight, maxTiles=$maxTiles, maxPolys=$maxPolys"

# For 32-bit format (version=1 / NAVMESHSET_VERSION), tile header:
# offset 40: tileRef (4 bytes, int32 in 32-bit)
# offset 44: dataSize (4 bytes)
$tileRef32 = [System.BitConverter]::ToInt32($bytes, 40)
$dataSize = [System.BitConverter]::ToInt32($bytes, 44)
Write-Host "Tile[0] tileRef32=$tileRef32, dataSize=$dataSize"

# DtMeshData header starts at offset 48
# DtMeshHeader layout:
# magic(4), version(4), x(4), y(4), layer(4), userId(4), polyCount(4), vertCount(4),
# maxLinkCount(4), detailMeshCount(4), detailVertCount(4), detailTriCount(4),
# bvNodeCount(4), offMeshConCount(4), offMeshBase(4), walkableHeight(4),
# walkableRadius(4), walkableClimb(4), bmin(12), bmax(12), bvQuantFactor(4)
$o = 48
$dmMagic = [System.BitConverter]::ToInt32($bytes, $o)
$dmVersion = [System.BitConverter]::ToInt32($bytes, $o+4)
$dmX = [System.BitConverter]::ToInt32($bytes, $o+8)
$dmY = [System.BitConverter]::ToInt32($bytes, $o+12)
$dmPolyCount = [System.BitConverter]::ToInt32($bytes, $o+24)
$dmVertCount = [System.BitConverter]::ToInt32($bytes, $o+28)
Write-Host "MeshData: magic=0x$($dmMagic.ToString('X8')), version=$dmVersion, x=$dmX, y=$dmY, polyCount=$dmPolyCount, vertCount=$dmVertCount"
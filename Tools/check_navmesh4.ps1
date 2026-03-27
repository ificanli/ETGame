$path = 'Packages/cn.etetet.map/Bundles/Recast/SDCMap.bytes'
$bytes = [System.IO.File]::ReadAllBytes($path)

# MeshData header starts at offset 56
# magic(4) version(4) x(4) y(4) layer(4) userId(4) polyCount(4) vertCount(4) maxLinkCount(4)
$o = 56
$polyCount   = [System.BitConverter]::ToInt32($bytes, $o + 24)
$vertCount   = [System.BitConverter]::ToInt32($bytes, $o + 28)
$maxLinkCount= [System.BitConverter]::ToInt32($bytes, $o + 32)
$detailMeshCount = [System.BitConverter]::ToInt32($bytes, $o + 36)
$detailVertCount = [System.BitConverter]::ToInt32($bytes, $o + 40)
$detailTriCount  = [System.BitConverter]::ToInt32($bytes, $o + 44)
$bvNodeCount     = [System.BitConverter]::ToInt32($bytes, $o + 48)
$offMeshConCount = [System.BitConverter]::ToInt32($bytes, $o + 52)
Write-Host "polyCount=$polyCount, vertCount=$vertCount, maxLinkCount=$maxLinkCount"
Write-Host "detailMeshCount=$detailMeshCount, detailVertCount=$detailVertCount, detailTriCount=$detailTriCount"
Write-Host "bvNodeCount=$bvNodeCount, offMeshConCount=$offMeshConCount"

# 计算 64-bit 读取时跳过的字节数 vs 实际应该跳过的字节数
$skip64 = $maxLinkCount * 16
$skip32 = $maxLinkCount * 12
Write-Host "link skip (64-bit wrong): $skip64 bytes"
Write-Host "link skip (32-bit correct): $skip32 bytes"
Write-Host "extra bytes skipped wrongly: $($skip64 - $skip32)"

# 计算 MeshData 理论大小（cCompatibility, is32Bit=false）
$headerSize = 100  # DtMeshHeader
$vertsSize = $vertCount * 3 * 4
# poly size: cCompatibility → has firstLink(4), verts(maxVert*2), neis(maxVert*2), flags(2), vertCount(1), areaAndtype(1)
$maxVert = 6
$polySize = 4 + $maxVert*2 + $maxVert*2 + 2 + 1 + 1  # = 30 bytes per poly
$polysSize = $polyCount * $polySize
$linksSkip64 = $maxLinkCount * 16
$linksSkip32 = $maxLinkCount * 12
$detailSize = $detailMeshCount * (4+4+1+1+2)  # vertBase(4)+triBase(4)+vertCount(1)+triCount(1)+padding(2)
$detailVertsSize = $detailVertCount * 3 * 4
$detailTrisSize = $detailTriCount * 4
$bvSize = $bvNodeCount * (3*4 + 3*4 + 4)  # bmin(12)+bmax(12)+i(4) = 28
$offMeshSize = $offMeshConCount * (6*4 + 4 + 2 + 1 + 1 + 4)  # = 30 bytes

$total64 = $headerSize + $vertsSize + $polysSize + $linksSkip64 + $detailSize + $detailVertsSize + $detailTrisSize + $bvSize + $offMeshSize
$total32 = $headerSize + $vertsSize + $polysSize + $linksSkip32 + $detailSize + $detailVertsSize + $detailTrisSize + $bvSize + $offMeshSize
$available = $bytes.Length - 56
Write-Host ""
Write-Host "可用字节 (offset 56 起): $available"
Write-Host "64-bit 计算需要字节: $total64"
Write-Host "32-bit 计算需要字节: $total32"
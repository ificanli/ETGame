param([string]$FilePath = 'Packages/cn.etetet.map/Bundles/Recast/SDCMap.bytes')

$bytes = [System.IO.File]::ReadAllBytes($FilePath)
$script:pos = 0

function ReadInt32 {
    $v = [BitConverter]::ToInt32($script:bytes, $script:pos)
    $script:pos += 4
    return $v
}
function ReadInt64 {
    $v = [BitConverter]::ToInt64($script:bytes, $script:pos)
    $script:pos += 8
    return $v
}
function ReadFloat {
    $v = [BitConverter]::ToSingle($script:bytes, $script:pos)
    $script:pos += 4
    return $v
}
function ReadUInt16 {
    $v = [BitConverter]::ToUInt16($script:bytes, $script:pos)
    $script:pos += 2
    return $v
}
function ReadByte2 {
    $v = $script:bytes[$script:pos]
    $script:pos += 1
    return $v
}

# NavMeshSet header
$magic   = ReadInt32
$version = ReadInt32
$numTiles = ReadInt32
Write-Host ('magic=0x{0:X8} version=0x{1:X8} numTiles={2}' -f $magic, $version, $numTiles)

# DtNavMeshParams (28 bytes: 3 floats + 2 floats + 2 ints)
$origX     = ReadFloat; $origY = ReadFloat; $origZ = ReadFloat
$tileWidth = ReadFloat; $tileHeight = ReadFloat
$maxTiles  = ReadInt32; $maxPolys = ReadInt32
Write-Host ('params: orig=({0:F2},{1:F2},{2:F2}) tileW={3:F2} tileH={4:F2} maxTiles={5} maxPolys={6}' -f $origX,$origY,$origZ,$tileWidth,$tileHeight,$maxTiles,$maxPolys)

$NAVMESHSET_VERSION         = 1
$NAVMESHSET_VERSION_RECAST4J = 0x8802
$DT_NAVMESH_VERSION_NO_FIRSTLINK = 0x8808
$NULL_NEI = 0xFFFF

$maxVPP = 6  # default passed to Read()
if ($version -eq $NAVMESHSET_VERSION_RECAST4J) {
    $maxVPP = ReadInt32
    Write-Host "maxVertsPerPoly=$maxVPP"
}

$cCompat = ($version -eq $NAVMESHSET_VERSION)

$totalPolys = 0
$polysWithNoNeighbors = 0
$polysWithSomeNeighbors = 0

for ($t = 0; $t -lt $numTiles; $t++) {
    if ($cCompat) {
        $tileRef = ReadInt64
    } else {
        $tileRef = ReadInt64
    }
    $dataSize = ReadInt32
    if ($tileRef -eq 0 -or $dataSize -eq 0) { break }
    if ($cCompat) { $null = ReadInt32 }  # C struct padding

    $tileStart = $script:pos

    # DtMeshHeader (25 fields x 4 bytes = 100 bytes)
    $dnavMagic       = ReadInt32
    $dver            = ReadInt32
    $tx              = ReadInt32
    $ty              = ReadInt32
    $tlayer          = ReadInt32
    $userId          = ReadInt32
    $polyCount       = ReadInt32
    $vertCount       = ReadInt32
    $maxLinkCount    = ReadInt32
    $detailMeshCount = ReadInt32
    $detailVertCount = ReadInt32
    $detailTriCount  = ReadInt32
    $bvNodeCount     = ReadInt32
    $offMeshConCount = ReadInt32
    $offMeshBase     = ReadInt32
    $wh     = ReadFloat
    $wr     = ReadFloat
    $wc     = ReadFloat
    $bminX  = ReadFloat; $bminY = ReadFloat; $bminZ = ReadFloat
    $bmaxX  = ReadFloat; $bmaxY = ReadFloat; $bmaxZ = ReadFloat
    $bvQF   = ReadFloat

    if ($t -lt 8) {
        Write-Host ('Tile[{0}]: coord=({1},{2}) polys={3} verts={4} bmin=({5:F1},{6:F1},{7:F1}) bmax=({8:F1},{9:F1},{10:F1}) dver=0x{11:X4}' -f $t,$tx,$ty,$polyCount,$vertCount,$bminX,$bminY,$bminZ,$bmaxX,$bmaxY,$bmaxZ,$dver)
    }

    # skip verts: vertCount * 3 floats
    $script:pos += $vertCount * 12

    # read polys
    $neisZero = 0
    $neisSome = 0
    $hasFirstLink = ($dver -lt $DT_NAVMESH_VERSION_NO_FIRSTLINK)
    for ($p = 0; $p -lt $polyCount; $p++) {
        if ($hasFirstLink) { $null = ReadInt32 }  # firstLink
        for ($j = 0; $j -lt $maxVPP; $j++) { $null = ReadUInt16 }  # verts[]
        $anyNei = $false
        for ($j = 0; $j -lt $maxVPP; $j++) {
            $nei = ReadUInt16
            if ($nei -ne 0 -and $nei -ne $NULL_NEI) { $anyNei = $true }
        }
        $null = ReadUInt16  # flags
        $null = ReadByte2   # vertCount
        $null = ReadByte2   # areaAndtype

        if ($anyNei) { $neisSome++ } else { $neisZero++ }
    }

    $totalPolys += $polyCount
    $polysWithNoNeighbors += $neisZero
    $polysWithSomeNeighbors += $neisSome

    if ($t -lt 8) {
        Write-Host ('  neis: zero={0} hasNei={1}' -f $neisZero, $neisSome)
    }

    # skip rest of tile (links, detail, bvtree, offmesh)
    $consumed = $script:pos - $tileStart
    $remaining = $dataSize - $consumed
    if ($remaining -lt 0) {
        Write-Host "ERROR: negative remaining=$remaining at tile $t (consumed=$consumed dataSize=$dataSize)"
        break
    }
    $script:pos += $remaining
}

Write-Host ''
Write-Host '=== SUMMARY ==='
Write-Host ('totalPolys            = {0}' -f $totalPolys)
Write-Host ('polysWithNoNeighbors  = {0} ({1:F1}%)' -f $polysWithNoNeighbors, (100.0*$polysWithNoNeighbors/$totalPolys))
Write-Host ('polysWithSomeNeighbors= {0} ({1:F1}%)' -f $polysWithSomeNeighbors, (100.0*$polysWithSomeNeighbors/$totalPolys))
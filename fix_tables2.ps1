Add-Type -AssemblyName System.IO.Compression.FileSystem

$tablesPath = 'D:\05ET\LoadOutTest\ETGame\Packages\cn.etetet.equipment\Luban\Config\Base\__tables__.xlsx'
$tempDir    = 'D:\05ET\LoadOutTest\ETGame\_tmp_tables'
$newXlsx    = 'D:\05ET\LoadOutTest\ETGame\_tmp_tables_new.xlsx'

if (Test-Path $tempDir) { Remove-Item $tempDir -Recurse -Force }
[System.IO.Compression.ZipFile]::ExtractToDirectory($tablesPath, $tempDir)

# --- 1. 更新 sharedStrings.xml ---
$ssPath = "$tempDir\xl\sharedStrings.xml"
[xml]$ss = Get-Content $ssPath -Raw -Encoding UTF8

# 获取或添加字符串，返回索引
function Get-OrAddString([xml]$ss, [string]$val) {
    $ssNs = $ss.DocumentElement.NamespaceURI
    $nodes = $ss.sst.si
    for ($i = 0; $i -lt $nodes.Count; $i++) {
        if ($nodes[$i].t -eq $val) { return $i }
    }
    $si = $ss.CreateElement("si", $ssNs)
    $t  = $ss.CreateElement("t",  $ssNs)
    $t.InnerText = $val
    $si.AppendChild($t) | Out-Null
    $ss.sst.AppendChild($si) | Out-Null
    $count = $ss.sst.si.Count
    $ss.sst.SetAttribute("count", $count)
    $ss.sst.SetAttribute("uniqueCount", $count)
    return $count - 1
}

$idxLabel    = Get-OrAddString $ss "HeroConfig"
$idxFullName = Get-OrAddString $ss "ET.HeroConfigCategory"
$idxValType  = Get-OrAddString $ss "HeroConfig"   # same as label
$idxInput    = Get-OrAddString $ss "../../../../cn.etetet.equipment/Luban/Config/Datas/Hero.xlsx"
$idxOutput   = Get-OrAddString $ss "HeroConfigCategory"

Write-Host ("Indices: label=$idxLabel fullName=$idxFullName valType=$idxValType input=$idxInput output=$idxOutput")

$ss.Save($ssPath)

# --- 2. 更新 sheet1.xml ---
$sheetPath = "$tempDir\xl\worksheets\sheet1.xml"
[xml]$xml = Get-Content $sheetPath -Raw -Encoding UTF8

$ns = $xml.DocumentElement.NamespaceURI

function New-Cell([xml]$doc, [string]$ref, [string]$type, [string]$val) {
    $c = $doc.CreateElement("c", $ns)
    $c.SetAttribute("r", $ref)
    if ($type -ne "") { $c.SetAttribute("t", $type) }
    $v = $doc.CreateElement("v", $ns)
    $v.InnerText = $val
    $c.AppendChild($v) | Out-Null
    return $c
}

$sheetData = $xml.worksheet.sheetData

# 删除所有 r>=5 的行（包括空行和之前重复追加的行）
$toRemove = @($sheetData.row | Where-Object { [int]$_.r -ge 5 })
foreach ($r in $toRemove) { $sheetData.RemoveChild($r) | Out-Null }
Write-Host ("Rows after cleanup: " + $sheetData.row.Count)

# 添加 HeroConfig 行（row 5）
$row5 = $xml.CreateElement("row", $ns)
$row5.SetAttribute("r", "5")
$row5.AppendChild((New-Cell $xml "B5" "s" $idxLabel))    | Out-Null
$row5.AppendChild((New-Cell $xml "C5" "s" $idxFullName)) | Out-Null
$row5.AppendChild((New-Cell $xml "D5" "s" $idxValType))  | Out-Null
$row5.AppendChild((New-Cell $xml "E5" ""  "1"))           | Out-Null
$row5.AppendChild((New-Cell $xml "F5" "s" $idxInput))    | Out-Null
$row5.AppendChild((New-Cell $xml "L5" "s" $idxOutput))   | Out-Null
$sheetData.AppendChild($row5) | Out-Null

$xml.Save($sheetPath)
Write-Host "sheet1.xml updated"

# --- 3. 重新打包 ---
if (Test-Path $newXlsx) { Remove-Item $newXlsx -Force }
[System.IO.Compression.ZipFile]::CreateFromDirectory($tempDir, $newXlsx)

# 替换原文件
Copy-Item $newXlsx $tablesPath -Force
Remove-Item $tempDir -Recurse -Force
Remove-Item $newXlsx -Force
Write-Host "Done - __tables__.xlsx updated"

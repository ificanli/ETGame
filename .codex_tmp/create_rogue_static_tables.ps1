$ErrorActionPreference = 'Stop'
function Set-ExcelCellValue($cell, $value) {
    if ($value -is [bool]) { $cell.Value2 = $(if($value){'TRUE'}else{'FALSE'}); return }
    if ($null -eq $value) { $cell.Clear() | Out-Null; return }
    $cell.Value2 = [string]$value
}
function New-LubanSheet($path, $headers, $types, $comments, $rows) {
    $excel = New-Object -ComObject Excel.Application
    $excel.Visible = $false
    $excel.DisplayAlerts = $false
    $wb = $null
    $ws = $null
    try {
        $wb = $excel.Workbooks.Add()
        $ws = $wb.Worksheets.Item(1)
        $ws.Name = 'Sheet1'
        Set-ExcelCellValue $ws.Cells.Item(1,1) '##var'
        for($i=0; $i -lt $headers.Count; $i++){ Set-ExcelCellValue $ws.Cells.Item(1,$i+2) $headers[$i] }
        Set-ExcelCellValue $ws.Cells.Item(2,1) '##type'
        for($i=0; $i -lt $types.Count; $i++){ Set-ExcelCellValue $ws.Cells.Item(2,$i+2) $types[$i] }
        Set-ExcelCellValue $ws.Cells.Item(3,1) '##group'
        Set-ExcelCellValue $ws.Cells.Item(4,1) '##'
        for($i=0; $i -lt $comments.Count; $i++){ Set-ExcelCellValue $ws.Cells.Item(4,$i+2) $comments[$i] }
        for($r=0; $r -lt $rows.Count; $r++){
            $row = $rows[$r]
            for($c=0; $c -lt $row.Count; $c++){
                Set-ExcelCellValue $ws.Cells.Item($r+5,$c+2) $row[$c]
            }
        }
        $dir = Split-Path $path -Parent
        if(!(Test-Path $dir)){ New-Item -ItemType Directory -Path $dir | Out-Null }
        $fullPath = Join-Path (Resolve-Path $dir).Path (Split-Path $path -Leaf)
        if(Test-Path $fullPath){ Remove-Item $fullPath -Force }
        $wb.SaveAs($fullPath)
        $wb.Close($true)
    }
    finally {
        if($ws){ [System.Runtime.InteropServices.Marshal]::ReleaseComObject($ws) | Out-Null }
        if($wb){ [System.Runtime.InteropServices.Marshal]::ReleaseComObject($wb) | Out-Null }
        $excel.Quit()
        [System.Runtime.InteropServices.Marshal]::ReleaseComObject($excel) | Out-Null
        [GC]::Collect(); [GC]::WaitForPendingFinalizers()
    }
}
$base = '.\Packages\cn.etetet.statesync\Luban\Config\Datas'
New-LubanSheet (Join-Path $base 'RogueLevelEntry.xlsx') @('Id','Level','NeedExp','TriggerChoice') @('int','int','int','bool') @('ID','等级','升级所需经验','是否触发选牌') @(
    ([object[]](1,1,100,$false)), ([object[]](2,2,130,$true)), ([object[]](3,3,160,$false)), ([object[]](4,4,200,$true)), ([object[]](5,5,240,$false)),
    ([object[]](6,6,280,$true)), ([object[]](7,7,330,$false)), ([object[]](8,8,380,$false)), ([object[]](9,9,430,$true)), ([object[]](10,10,500,$false))
)
New-LubanSheet (Join-Path $base 'RogueLevelNumericEntry.xlsx') @('Id','Level','NumericType','Value') @('int','int','int','long') @('ID','等级','数值类型','增加值') @(
    ([object[]](1,1,10021,20)), ([object[]](2,1,10001,50)), ([object[]](3,2,10021,25)), ([object[]](4,2,10001,50)), ([object[]](5,3,10021,30)), ([object[]](6,3,10001,60)),
    ([object[]](7,4,10021,35)), ([object[]](8,4,10001,60)), ([object[]](9,5,10021,40)), ([object[]](10,5,10001,70)), ([object[]](11,6,10021,45)), ([object[]](12,6,10001,70)),
    ([object[]](13,7,10021,50)), ([object[]](14,7,10001,80)), ([object[]](15,8,10021,55)), ([object[]](16,8,10001,80)), ([object[]](17,9,10021,60)), ([object[]](18,9,10001,90)),
    ([object[]](19,10,10021,70)), ([object[]](20,10,10001,100))
)
New-LubanSheet (Join-Path $base 'RogueKillExpEntry.xlsx') @('Id','UnitType','Exp') @('int','int','int') @('ID','单位类型','击杀经验') @(
    ([object[]](1,2,60)), ([object[]](2,8,20))
)
New-LubanSheet (Join-Path $base 'RogueGlobalConfig.xlsx') @('Id','DefaultKillExp','ChoiceOptionCount') @('int','int','int') @('ID','默认击杀经验','选牌数量') @(
    ([object[]](1,20,3))
)
$tablesPath = Resolve-Path '.\Packages\cn.etetet.statesync\Luban\Config\Base\__tables__.xlsx'
$excel = New-Object -ComObject Excel.Application
$excel.Visible = $false
$excel.DisplayAlerts = $false
$wb = $null
$ws = $null
try {
    $wb = $excel.Workbooks.Open($tablesPath)
    $ws = $wb.Worksheets.Item(1)
    $rows = @(
        ([object[]]('RogueLevelEntryConfig','ET.RogueLevelEntryConfigCategory','RogueLevelEntryConfig','TRUE','../../../../cn.etetet.statesync/Luban/Config/Datas/RogueLevelEntry.xlsx','','')),
        ([object[]]('RogueLevelNumericEntryConfig','ET.RogueLevelNumericEntryConfigCategory','RogueLevelNumericEntryConfig','TRUE','../../../../cn.etetet.statesync/Luban/Config/Datas/RogueLevelNumericEntry.xlsx','','')),
        ([object[]]('RogueKillExpEntryConfig','ET.RogueKillExpEntryConfigCategory','RogueKillExpEntryConfig','TRUE','../../../../cn.etetet.statesync/Luban/Config/Datas/RogueKillExpEntry.xlsx','','')),
        ([object[]]('RogueGlobalConfig','ET.RogueGlobalConfigCategory','RogueGlobalConfig','TRUE','../../../../cn.etetet.statesync/Luban/Config/Datas/RogueGlobalConfig.xlsx','','one'))
    )
    $start = 7
    for($r=0; $r -lt $rows.Count; $r++){
        $rowIndex = $start + $r
        for($c=0; $c -lt $rows[$r].Count; $c++){
            $value = $rows[$r][$c]
            if([string]::IsNullOrEmpty([string]$value)){ $ws.Cells.Item($rowIndex,$c+2).Clear() | Out-Null }
            else { Set-ExcelCellValue $ws.Cells.Item($rowIndex,$c+2) $value }
        }
    }
    $wb.Save()
    $wb.Close($true)
}
finally {
    if($ws){ [System.Runtime.InteropServices.Marshal]::ReleaseComObject($ws) | Out-Null }
    if($wb){ [System.Runtime.InteropServices.Marshal]::ReleaseComObject($wb) | Out-Null }
    $excel.Quit()
    [System.Runtime.InteropServices.Marshal]::ReleaseComObject($excel) | Out-Null
    [GC]::Collect(); [GC]::WaitForPendingFinalizers()
}
Write-Host 'created rogue static config tables.'

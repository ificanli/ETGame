$excel = New-Object -ComObject Excel.Application
$excel.Visible = $false
$excel.DisplayAlerts = $false

# Read Hero.xlsx
$heroPath = 'D:\05ET\LoadOutTest\ETGame\Packages\cn.etetet.equipment\Luban\Config\Datas\Hero.xlsx'
$wb = $excel.Workbooks.Open($heroPath)
$ws = $wb.Sheets.Item(1)
$lastRow = $ws.UsedRange.Rows.Count
Write-Host ("Hero.xlsx rows: " + $lastRow)
for ($i = 1; $i -le $lastRow; $i++) {
    $row = ""
    for ($j = 1; $j -le 5; $j++) {
        $row += "[" + $ws.Cells.Item($i, $j).Value2 + "]"
    }
    Write-Host ("Row " + $i + ": " + $row)
}
$wb.Close($false)

# Read __tables__.xlsx row 5
$tablesPath = 'D:\05ET\LoadOutTest\ETGame\Packages\cn.etetet.equipment\Luban\Config\Base\__tables__.xlsx'
$wb2 = $excel.Workbooks.Open($tablesPath)
$ws2 = $wb2.Sheets.Item(1)
$row5 = ""
for ($j = 1; $j -le 12; $j++) {
    $row5 += "[" + $ws2.Cells.Item(5, $j).Value2 + "]"
}
Write-Host ("Tables row 5: " + $row5)
$wb2.Close($false)

$excel.Quit()
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($excel) | Out-Null

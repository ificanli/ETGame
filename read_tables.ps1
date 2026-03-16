$excel = New-Object -ComObject Excel.Application
$excel.Visible = $false
$excel.DisplayAlerts = $false
$wb = $excel.Workbooks.Open('D:\05ET\LoadOutTest\ETGame\Packages\cn.etetet.equipment\Luban\Config\Base\__tables__.xlsx')
$ws = $wb.Sheets.Item(1)
$lastRow = $ws.UsedRange.Rows.Count
Write-Host ("Last row: " + $lastRow)
for ($i = 1; $i -le $lastRow; $i++) {
    $row = ""
    for ($j = 1; $j -le 12; $j++) {
        $val = $ws.Cells.Item($i, $j).Text
        $row += "[" + $val + "]"
    }
    if ($row -ne "[][][][][][][][][][][][]") {
        Write-Host ("Row " + $i + ": " + $row)
    }
}
$wb.Close($false)
$excel.Quit()
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($excel) | Out-Null

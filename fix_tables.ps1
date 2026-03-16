$excel = New-Object -ComObject Excel.Application
$excel.Visible = $false
$excel.DisplayAlerts = $false
$tablesPath = 'D:\05ET\LoadOutTest\ETGame\Packages\cn.etetet.equipment\Luban\Config\Base\__tables__.xlsx'
$wb = $excel.Workbooks.Open($tablesPath)
$ws = $wb.Sheets.Item(1)

# Read row 5 specifically
$row5 = ""
for ($j = 1; $j -le 12; $j++) {
    $row5 += "[" + $ws.Cells.Item(5, $j).Value2 + "]"
}
Write-Host ("Row 5 before: " + $row5)

# Write HeroConfig row at row 5
$ws.Cells.Item(5, 3).Value2 = "ET.HeroConfigCategory"
$ws.Cells.Item(5, 4).Value2 = "HeroConfig"
$ws.Cells.Item(5, 5).Value2 = "TRUE"
$ws.Cells.Item(5, 6).Value2 = "../../../../cn.etetet.equipment/Luban/Config/Datas/Hero.xlsx"
$ws.Cells.Item(5, 12).Value2 = "HeroConfigCategory"

# Verify before save
$row5after = ""
for ($j = 1; $j -le 12; $j++) {
    $row5after += "[" + $ws.Cells.Item(5, $j).Value2 + "]"
}
Write-Host ("Row 5 after write: " + $row5after)

$wb.Save()
Write-Host "Saved"
$wb.Close($false)
$excel.Quit()
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($excel) | Out-Null

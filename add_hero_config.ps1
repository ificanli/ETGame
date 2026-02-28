$excel = New-Object -ComObject Excel.Application
$excel.Visible = $false
$excel.DisplayAlerts = $false

# 1. 在 __tables__.xlsx 加 HeroConfig 行
$tablesPath = 'D:\05ET\LoadOutTest\ETGame\Packages\cn.etetet.equipment\Luban\Config\Base\__tables__.xlsx'
$wb = $excel.Workbooks.Open($tablesPath)
$ws = $wb.Sheets.Item(1)
$lastRow = $ws.UsedRange.Rows.Count

# 找第一个空行（跳过注释行）
$insertRow = $lastRow + 1
for ($i = 4; $i -le $lastRow; $i++) {
    if ($ws.Cells.Item($i, 3).Text -eq "") {
        $insertRow = $i
        break
    }
}

$ws.Cells.Item($insertRow, 3).Value2 = "ET.HeroConfigCategory"
$ws.Cells.Item($insertRow, 4).Value2 = "HeroConfig"
$ws.Cells.Item($insertRow, 5).Value2 = "TRUE"
$ws.Cells.Item($insertRow, 6).Value2 = "../../../../cn.etetet.equipment/Luban/Config/Datas/Hero.xlsx"
$ws.Cells.Item($insertRow, 12).Value2 = "HeroConfigCategory"

$wb.Save()
$wb.Close($false)
Write-Host ("Added HeroConfig at row " + $insertRow)

# 2. 创建 Hero.xlsx 数据文件
$heroPath = 'D:\05ET\LoadOutTest\ETGame\Packages\cn.etetet.equipment\Luban\Config\Datas\Hero.xlsx'
$wb2 = $excel.Workbooks.Add()
$ws2 = $wb2.Sheets.Item(1)

# 表头行（Luban 格式）
$ws2.Cells.Item(1, 1).Value2 = "##var"
$ws2.Cells.Item(1, 2).Value2 = "Id"
$ws2.Cells.Item(1, 3).Value2 = "Name"
$ws2.Cells.Item(1, 4).Value2 = "UnitConfigId"

$ws2.Cells.Item(2, 1).Value2 = "##type"
$ws2.Cells.Item(2, 2).Value2 = "int"
$ws2.Cells.Item(2, 3).Value2 = "string"
$ws2.Cells.Item(2, 4).Value2 = "int"

$ws2.Cells.Item(3, 1).Value2 = "##group"
$ws2.Cells.Item(3, 2).Value2 = "c,s"
$ws2.Cells.Item(3, 3).Value2 = "c,s"
$ws2.Cells.Item(3, 4).Value2 = "c,s"

# 数据行
$ws2.Cells.Item(4, 2).Value2 = 1001
$ws2.Cells.Item(4, 3).Value2 = "Assault"
$ws2.Cells.Item(4, 4).Value2 = 1001

$ws2.Cells.Item(5, 2).Value2 = 1002
$ws2.Cells.Item(5, 3).Value2 = "Sniper"
$ws2.Cells.Item(5, 4).Value2 = 1001

$wb2.SaveAs($heroPath)
$wb2.Close($false)
Write-Host "Created Hero.xlsx"

$excel.Quit()
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($excel) | Out-Null
Write-Host "Done"

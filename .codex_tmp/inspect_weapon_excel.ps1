$ErrorActionPreference = 'Stop'

$excelPath = Join-Path $PSScriptRoot '..\Packages\cn.etetet.statesync\Luban\Config\Datas\Weapon.xlsx'
$excelPath = (Resolve-Path $excelPath).Path

Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead($excelPath)
try {
    $sheetEntry = $zip.Entries | Where-Object { $_.FullName -eq 'xl/worksheets/sheet1.xml' }
    [xml]$sheetXml = (New-Object System.IO.StreamReader($sheetEntry.Open())).ReadToEnd()

    foreach ($rowNumber in 5..9) {
        $cellRef = "Q$rowNumber"
        $rowNode = $sheetXml.worksheet.sheetData.row | Where-Object { $_.r -eq [string]$rowNumber }
        $cellNode = $rowNode.c | Where-Object { $_.r -eq $cellRef }
        if ($null -eq $cellNode) {
            Write-Output "$cellRef <missing>"
            continue
        }

        $value = ''
        if ($cellNode.is -and $cellNode.is.t) {
            $value = [string]$cellNode.is.t
        } elseif ($cellNode.v) {
            $value = [string]$cellNode.v
        }

        Write-Output "$cellRef t=$($cellNode.t) value=$value"
    }
} finally {
    $zip.Dispose()
}

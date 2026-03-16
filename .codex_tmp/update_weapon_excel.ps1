$ErrorActionPreference = 'Stop'

$excelPath = Join-Path $PSScriptRoot '..\Packages\cn.etetet.statesync\Luban\Config\Datas\Weapon.xlsx'
$excelPath = (Resolve-Path $excelPath).Path
$tempRoot = Join-Path $env:TEMP ('weapon_xlsx_' + [guid]::NewGuid().ToString('N'))

function Get-ColumnLetters([int]$columnNumber) {
    $letters = ''
    while ($columnNumber -gt 0) {
        $columnNumber--
        $letters = [char](65 + ($columnNumber % 26)) + $letters
        $columnNumber = [math]::Floor($columnNumber / 26)
    }
    return $letters
}

New-Item -ItemType Directory -Path $tempRoot | Out-Null
try {
    Copy-Item $excelPath (Join-Path $tempRoot 'Weapon.zip')
    Expand-Archive -Path (Join-Path $tempRoot 'Weapon.zip') -DestinationPath (Join-Path $tempRoot 'unzipped')

    $sheetPath = Join-Path $tempRoot 'unzipped\xl\worksheets\sheet1.xml'
    [xml]$sheetXml = Get-Content -Path $sheetPath -Raw

    for ($rowNumber = 5; $rowNumber -le 9; $rowNumber++) {
        $rowNode = $sheetXml.worksheet.sheetData.row | Where-Object { $_.r -eq [string]$rowNumber }
        if ($null -eq $rowNode) {
            throw "Row $rowNumber not found in sheet1.xml"
        }

        $cellRef = "Q$rowNumber"
        $cellNode = $rowNode.c | Where-Object { $_.r -eq $cellRef }
        if ($null -eq $cellNode) {
            $cellNode = $sheetXml.CreateElement('c', $sheetXml.DocumentElement.NamespaceURI)
            $cellNode.SetAttribute('r', $cellRef)
            $cellNode.SetAttribute('t', 'inlineStr')

            $inserted = $false
            foreach ($existingCell in @($rowNode.c)) {
                $columnLetters = ($existingCell.r -replace '\d', '')
                if ([string]::CompareOrdinal($columnLetters, 'Q') -gt 0) {
                    [void]$rowNode.InsertBefore($cellNode, $existingCell)
                    $inserted = $true
                    break
                }
            }

            if (-not $inserted) {
                [void]$rowNode.AppendChild($cellNode)
            }
        } else {
            $cellNode.RemoveAll()
            $cellNode.SetAttribute('r', $cellRef)
            $cellNode.SetAttribute('t', 'inlineStr')
        }

        $inlineStr = $sheetXml.CreateElement('is', $sheetXml.DocumentElement.NamespaceURI)
        $textNode = $sheetXml.CreateElement('t', $sheetXml.DocumentElement.NamespaceURI)
        $textNode.InnerText = 'Gun1'
        [void]$inlineStr.AppendChild($textNode)
        [void]$cellNode.AppendChild($inlineStr)
    }

    $sheetXml.Save($sheetPath)

    Remove-Item (Join-Path $tempRoot 'Weapon.zip') -Force
    Compress-Archive -Path (Join-Path $tempRoot 'unzipped\*') -DestinationPath (Join-Path $tempRoot 'Weapon.zip')
    Copy-Item (Join-Path $tempRoot 'Weapon.zip') $excelPath -Force

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [System.IO.Compression.ZipFile]::OpenRead($excelPath)
    try {
        $sheetEntry = $zip.Entries | Where-Object { $_.FullName -eq 'xl/worksheets/sheet1.xml' }
        [xml]$verifyXml = (New-Object System.IO.StreamReader($sheetEntry.Open())).ReadToEnd()
        foreach ($rowNumber in 5..9) {
            $cellRef = "Q$rowNumber"
            $rowNode = $verifyXml.worksheet.sheetData.row | Where-Object { $_.r -eq [string]$rowNumber }
            $cellNode = $rowNode.c | Where-Object { $_.r -eq $cellRef }
            if ($null -eq $cellNode -or $cellNode.is.t -ne 'Gun1') {
                throw "Verification failed for $cellRef"
            }
        }
    } finally {
        $zip.Dispose()
    }

    Write-Output 'UPDATED_OK'
} finally {
    Remove-Item $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}

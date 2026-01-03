using System.Text.Json;
using System.Text.Json.Nodes;
using ET.Test;
using ET.Tools.Excel;
using OfficeOpenXml;

namespace ET.Test;

public class ExcelMergeCellsToolTests : ExcelTestBase
{
    private readonly ExcelMergeCellsTool _tool = new();

    [Fact]
    public async Task MergeCells_ShouldMergeRange()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbookWithData("test_merge_cells.xlsx", 3);
        var outputPath = CreateTestFilePath("test_merge_cells_output.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "merge",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["range"] = "A1:C1"
        };

        // Act
        var result = await _tool.ExecuteAsync(arguments);

        // Assert
        Assert.Contains("已合并单元格范围", result);
        Assert.Contains("A1:C1", result);

        using var package = new ExcelPackage(new FileInfo(outputPath));
        var worksheet = package.Workbook.Worksheets[0];
        Assert.True(worksheet.Cells["A1:C1"].Merge);
    }

    [Fact]
    public async Task MergeCells_MultipleRows_ShouldMerge()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbookWithData("test_merge_multi.xlsx", 5, 5);
        var outputPath = CreateTestFilePath("test_merge_multi_output.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "merge",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["range"] = "A1:B3"
        };

        // Act
        var result = await _tool.ExecuteAsync(arguments);

        // Assert
        Assert.Contains("已合并单元格范围", result);
        Assert.Contains("A1:B3", result);

        using var package = new ExcelPackage(new FileInfo(outputPath));
        var worksheet = package.Workbook.Worksheets[0];
        Assert.True(worksheet.Cells["A1:B3"].Merge);
    }

    [Fact]
    public async Task MergeCells_InvalidSheetIndex_ShouldThrowException()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbookWithData("test_merge_invalid_sheet.xlsx", 3);
        var arguments = new JsonObject
        {
            ["operation"] = "merge",
            ["path"] = workbookPath,
            ["sheetIndex"] = 99,
            ["range"] = "A1:C1"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _tool.ExecuteAsync(arguments));
    }

    [Fact]
    public async Task UnmergeCells_ShouldUnmergeRange()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbookWithData("test_unmerge_cells.xlsx", 3);
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            var worksheet = package.Workbook.Worksheets[0];
            worksheet.Cells["A1:C1"].Merge = true;
            package.Save();
        }

        var outputPath = CreateTestFilePath("test_unmerge_cells_output.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "unmerge",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["range"] = "A1:C1"
        };

        // Act
        var result = await _tool.ExecuteAsync(arguments);

        // Assert
        Assert.Contains("已取消合并单元格范围", result);
        Assert.Contains("A1:C1", result);

        using var resultPackage = new ExcelPackage(new FileInfo(outputPath));
        var resultWorksheet = resultPackage.Workbook.Worksheets[0];
        Assert.False(resultWorksheet.Cells["A1:C1"].Merge);
    }

    [Fact]
    public async Task UnmergeCells_WithKeepValue_ShouldKeepValue()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbookWithData("test_unmerge_keep_value.xlsx", 3);
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            var worksheet = package.Workbook.Worksheets[0];
            worksheet.Cells["A1"].Value = "MergedValue";
            worksheet.Cells["A1:C1"].Merge = true;
            package.Save();
        }

        var outputPath = CreateTestFilePath("test_unmerge_keep_value_output.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "unmerge",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["range"] = "A1:C1",
            ["keepValue"] = true
        };

        // Act
        await _tool.ExecuteAsync(arguments);

        // Assert
        using var resultPackage = new ExcelPackage(new FileInfo(outputPath));
        var resultWorksheet = resultPackage.Workbook.Worksheets[0];
        Assert.Equal("MergedValue", resultWorksheet.Cells["A1"].Value?.ToString());
        Assert.Equal("MergedValue", resultWorksheet.Cells["B1"].Value?.ToString());
        Assert.Equal("MergedValue", resultWorksheet.Cells["C1"].Value?.ToString());
    }

    [Fact]
    public async Task UnmergeCells_InvalidSheetIndex_ShouldThrowException()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbookWithData("test_unmerge_invalid_sheet.xlsx", 3);
        var arguments = new JsonObject
        {
            ["operation"] = "unmerge",
            ["path"] = workbookPath,
            ["sheetIndex"] = 99,
            ["range"] = "A1:C1"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _tool.ExecuteAsync(arguments));
    }

    [Fact]
    public async Task GetMergedCells_ShouldReturnMergedCells()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbookWithData("test_get_merged_cells.xlsx", 3);
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            var worksheet = package.Workbook.Worksheets[0];
            worksheet.Cells["A1"].Value = "Header";
            worksheet.Cells["A1:C1"].Merge = true;
            package.Save();
        }

        var arguments = new JsonObject
        {
            ["operation"] = "get_merged",
            ["path"] = workbookPath
        };

        // Act
        var result = await _tool.ExecuteAsync(arguments);

        // Assert
        var json = JsonDocument.Parse(result);
        var root = json.RootElement;

        Assert.Equal(1, root.GetProperty("mergedCellsCount").GetInt32());
        var items = root.GetProperty("mergedCells");
        Assert.Equal(1, items.GetArrayLength());

        var firstItem = items[0];
        Assert.Equal("A1:C1", firstItem.GetProperty("address").GetString());
        Assert.Equal(1, firstItem.GetProperty("startRow").GetInt32());
        Assert.Equal(1, firstItem.GetProperty("startColumn").GetInt32());
        Assert.Equal(1, firstItem.GetProperty("endRow").GetInt32());
        Assert.Equal(3, firstItem.GetProperty("endColumn").GetInt32());
        Assert.Equal(1, firstItem.GetProperty("rowCount").GetInt32());
        Assert.Equal(3, firstItem.GetProperty("columnCount").GetInt32());
        Assert.Equal("Header", firstItem.GetProperty("value").GetString());
    }

    [Fact]
    public async Task GetMergedCells_EmptyWorksheet_ShouldReturnEmptyResult()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbook("test_get_no_merged.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "get_merged",
            ["path"] = workbookPath
        };

        // Act
        var result = await _tool.ExecuteAsync(arguments);

        // Assert
        var json = JsonDocument.Parse(result);
        var root = json.RootElement;

        Assert.Equal(0, root.GetProperty("mergedCellsCount").GetInt32());
    }

    [Fact]
    public async Task GetMergedCells_MultipleMergedRanges_ShouldReturnAll()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbookWithData("test_get_multi_merged.xlsx", 10, 5);
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            var worksheet = package.Workbook.Worksheets[0];
            worksheet.Cells["A1:C1"].Merge = true;  // A1:C1
            worksheet.Cells["A3:B4"].Merge = true;  // A3:B4
            package.Save();
        }

        var arguments = new JsonObject
        {
            ["operation"] = "get_merged",
            ["path"] = workbookPath
        };

        // Act
        var result = await _tool.ExecuteAsync(arguments);

        // Assert
        var json = JsonDocument.Parse(result);
        var root = json.RootElement;

        Assert.Equal(2, root.GetProperty("mergedCellsCount").GetInt32());
        Assert.Equal(2, root.GetProperty("mergedCells").GetArrayLength());
    }

    [Fact]
    public async Task GetMergedCells_InvalidSheetIndex_ShouldThrowException()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbook("test_get_invalid_sheet.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "get_merged",
            ["path"] = workbookPath,
            ["sheetIndex"] = 99
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _tool.ExecuteAsync(arguments));
    }

    [Fact]
    public async Task ExecuteAsync_InvalidOperation_ShouldThrowException()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbook("test_invalid_op.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "invalid",
            ["path"] = workbookPath
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _tool.ExecuteAsync(arguments));
        Assert.Contains("未知操作", exception.Message);
    }

    [Fact]
    public async Task MergeCells_WithSheetIndex_ShouldMergeCorrectSheet()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbookWithData("test_merge_sheet_index.xlsx", 3);
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            var ws2 = package.Workbook.Worksheets.Add("Sheet2");
            ws2.Cells["A1"].Value = "Test";
            package.Save();
        }

        var outputPath = CreateTestFilePath("test_merge_sheet_index_output.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "merge",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["sheetIndex"] = 1,
            ["range"] = "A1:C1"
        };

        // Act
        var result = await _tool.ExecuteAsync(arguments);

        // Assert
        Assert.Contains("已合并", result);

        using var package2 = new ExcelPackage(new FileInfo(outputPath));
        // Sheet1 (index 0) 不应该有合并单元格
        Assert.False(package2.Workbook.Worksheets[0].Cells["A1:C1"].Merge);
        // Sheet2 (index 1) 应该有合并单元格
        Assert.True(package2.Workbook.Worksheets[1].Cells["A1:C1"].Merge);
    }
}

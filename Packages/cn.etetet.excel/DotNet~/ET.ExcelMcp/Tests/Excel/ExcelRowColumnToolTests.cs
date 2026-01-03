using System.Text.Json.Nodes;
using ET.Test;
using ET.Tools.Excel;
using OfficeOpenXml;

namespace ET.Test;

public class ExcelRowColumnToolTests : ExcelTestBase
{
    private readonly ExcelRowColumnTool _tool = new();

    [Fact]
    public async Task InsertRows_ShouldInsertRow()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbookWithData("test_insert_row.xlsx", 3);
        var outputPath = CreateTestFilePath("test_insert_row_output.xlsx");
        var arguments = CreateArguments("insert_rows", workbookPath, outputPath);
        arguments["startRow"] = 2;
        arguments["count"] = 1;

        // Act
        await _tool.ExecuteAsync(arguments);

        // Assert
        using var package = new ExcelPackage(new FileInfo(outputPath));
        var worksheet = package.Workbook.Worksheets[0];

        // 验证行被插入了 (原始第2行的数据应该移到第3行)
        var originalR2C1 = "R2C1";
        var newR3C1 = worksheet.Cells[3, 1].Value?.ToString() ?? "";
        Assert.Equal(originalR2C1, newR3C1);
    }

    [Fact]
    public async Task DeleteRows_ShouldDeleteRow()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbookWithData("test_delete_row.xlsx", 5);
        var outputPath = CreateTestFilePath("test_delete_row_output.xlsx");
        var arguments = CreateArguments("delete_rows", workbookPath, outputPath);
        arguments["startRow"] = 2;
        arguments["count"] = 1;

        // Act
        await _tool.ExecuteAsync(arguments);

        // Assert
        using var package = new ExcelPackage(new FileInfo(outputPath));
        var worksheet = package.Workbook.Worksheets[0];

        // 验证第2行被删除，原第3行数据移到第2行
        var expectedR2C1 = "R3C1";
        var actualR2C1 = worksheet.Cells[2, 1].Value?.ToString() ?? "";
        Assert.Equal(expectedR2C1, actualR2C1);
    }

    [Fact]
    public async Task InsertColumns_ShouldInsertColumn()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbookWithData("test_insert_column.xlsx", 3, 3);
        var outputPath = CreateTestFilePath("test_insert_column_output.xlsx");
        var arguments = CreateArguments("insert_columns", workbookPath, outputPath);
        arguments["startColumn"] = 2;
        arguments["count"] = 1;

        // Act
        await _tool.ExecuteAsync(arguments);

        // Assert
        using var package = new ExcelPackage(new FileInfo(outputPath));
        var worksheet = package.Workbook.Worksheets[0];

        // 验证列被插入 (原第2列的数据应该移到第3列)
        var originalR1C2 = "R1C2";
        var newR1C3 = worksheet.Cells[1, 3].Value?.ToString() ?? "";
        Assert.Equal(originalR1C2, newR1C3);
    }

    [Fact]
    public async Task DeleteColumns_ShouldDeleteColumn()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbookWithData("test_delete_column.xlsx", 3, 5);
        var outputPath = CreateTestFilePath("test_delete_column_output.xlsx");
        var arguments = CreateArguments("delete_columns", workbookPath, outputPath);
        arguments["startColumn"] = 2;
        arguments["count"] = 1;

        // Act
        await _tool.ExecuteAsync(arguments);

        // Assert
        using var package = new ExcelPackage(new FileInfo(outputPath));
        var worksheet = package.Workbook.Worksheets[0];

        // 验证第2列被删除，原第3列数据移到第2列
        var expectedR1C2 = "R1C3";
        var actualR1C2 = worksheet.Cells[1, 2].Value?.ToString() ?? "";
        Assert.Equal(expectedR1C2, actualR1C2);
    }

    [Fact]
    public async Task InsertMultipleRows_ShouldInsertMultipleRows()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbookWithData("test_insert_multiple_rows.xlsx", 3);
        var outputPath = CreateTestFilePath("test_insert_multiple_rows_output.xlsx");
        var arguments = CreateArguments("insert_rows", workbookPath, outputPath);
        arguments["startRow"] = 2;
        arguments["count"] = 3;

        // Act
        var result = await _tool.ExecuteAsync(arguments);

        // Assert
        Assert.Contains("3", result);
        Assert.Contains("行", result);
        using var package = new ExcelPackage(new FileInfo(outputPath));
        var worksheet = package.Workbook.Worksheets[0];
        Assert.True(worksheet.Dimension.Rows >= 6);
    }

    [Fact]
    public async Task DeleteMultipleRows_ShouldDeleteMultipleRows()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbookWithData("test_delete_multiple_rows.xlsx", 5);
        var outputPath = CreateTestFilePath("test_delete_multiple_rows_output.xlsx");
        var arguments = CreateArguments("delete_rows", workbookPath, outputPath);
        arguments["startRow"] = 2;
        arguments["count"] = 2;

        // Act
        var result = await _tool.ExecuteAsync(arguments);

        // Assert
        Assert.Contains("2", result);
        Assert.Contains("行", result);
        using var package = new ExcelPackage(new FileInfo(outputPath));
        var worksheet = package.Workbook.Worksheets[0];
        Assert.True(worksheet.Dimension.Rows <= 3);
    }

    [Fact]
    public async Task HideRows_ShouldHideRows()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbookWithData("test_hide_rows.xlsx", 5);
        var outputPath = CreateTestFilePath("test_hide_rows_output.xlsx");
        var arguments = CreateArguments("hide_rows", workbookPath, outputPath);
        arguments["startRow"] = 2;
        arguments["endRow"] = 3;

        // Act
        await _tool.ExecuteAsync(arguments);

        // Assert
        using var package = new ExcelPackage(new FileInfo(outputPath));
        var worksheet = package.Workbook.Worksheets[0];
        Assert.True(worksheet.Row(2).Hidden);
        Assert.True(worksheet.Row(3).Hidden);
        Assert.False(worksheet.Row(1).Hidden);
        Assert.False(worksheet.Row(4).Hidden);
    }

    [Fact]
    public async Task ShowRows_ShouldShowRows()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbookWithData("test_show_rows.xlsx", 5);
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            var worksheet = package.Workbook.Worksheets[0];
            worksheet.Row(2).Hidden = true;
            worksheet.Row(3).Hidden = true;
            package.Save();
        }

        var outputPath = CreateTestFilePath("test_show_rows_output.xlsx");
        var arguments = CreateArguments("show_rows", workbookPath, outputPath);
        arguments["startRow"] = 2;
        arguments["endRow"] = 3;

        // Act
        await _tool.ExecuteAsync(arguments);

        // Assert
        using var package2 = new ExcelPackage(new FileInfo(outputPath));
        var worksheet2 = package2.Workbook.Worksheets[0];
        Assert.False(worksheet2.Row(2).Hidden);
        Assert.False(worksheet2.Row(3).Hidden);
    }

    [Fact]
    public async Task HideColumns_ShouldHideColumns()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbookWithData("test_hide_columns.xlsx", 3, 5);
        var outputPath = CreateTestFilePath("test_hide_columns_output.xlsx");
        var arguments = CreateArguments("hide_columns", workbookPath, outputPath);
        arguments["startColumn"] = 2;
        arguments["endColumn"] = 3;

        // Act
        await _tool.ExecuteAsync(arguments);

        // Assert
        using var package = new ExcelPackage(new FileInfo(outputPath));
        var worksheet = package.Workbook.Worksheets[0];
        Assert.True(worksheet.Column(2).Hidden);
        Assert.True(worksheet.Column(3).Hidden);
        Assert.False(worksheet.Column(1).Hidden);
        Assert.False(worksheet.Column(4).Hidden);
    }

    [Fact]
    public async Task ShowColumns_ShouldShowColumns()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbookWithData("test_show_columns.xlsx", 3, 5);
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            var worksheet = package.Workbook.Worksheets[0];
            worksheet.Column(2).Hidden = true;
            worksheet.Column(3).Hidden = true;
            package.Save();
        }

        var outputPath = CreateTestFilePath("test_show_columns_output.xlsx");
        var arguments = CreateArguments("show_columns", workbookPath, outputPath);
        arguments["startColumn"] = 2;
        arguments["endColumn"] = 3;

        // Act
        await _tool.ExecuteAsync(arguments);

        // Assert
        using var package2 = new ExcelPackage(new FileInfo(outputPath));
        var worksheet2 = package2.Workbook.Worksheets[0];
        Assert.False(worksheet2.Column(2).Hidden);
        Assert.False(worksheet2.Column(3).Hidden);
    }

    [Fact]
    public async Task SetSize_ShouldSetRowHeightAndColumnWidth()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbookWithData("test_set_size.xlsx", 3);
        var outputPath = CreateTestFilePath("test_set_size_output.xlsx");
        var arguments = CreateArguments("set_size", workbookPath, outputPath);
        arguments["rowIndex"] = 2;
        arguments["rowHeight"] = 30;
        arguments["columnIndex"] = 2;
        arguments["columnWidth"] = 20;

        // Act
        await _tool.ExecuteAsync(arguments);

        // Assert
        using var package = new ExcelPackage(new FileInfo(outputPath));
        var worksheet = package.Workbook.Worksheets[0];
        Assert.Equal(30, worksheet.Row(2).Height);
        Assert.Equal(20, worksheet.Column(2).Width);
    }

    [Fact]
    public async Task AutoFit_ShouldAutoFitColumns()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbookWithData("test_auto_fit.xlsx", 3);
        var outputPath = CreateTestFilePath("test_auto_fit_output.xlsx");
        var arguments = CreateArguments("auto_fit", workbookPath, outputPath);

        // Act
        await _tool.ExecuteAsync(arguments);

        // Assert
        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public async Task GetInfo_ShouldReturnRowInfo()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbookWithData("test_get_info_row.xlsx", 3);
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            var worksheet = package.Workbook.Worksheets[0];
            worksheet.Row(2).Height = 25;
            package.Save();
        }

        var arguments = CreateArguments("get_info", workbookPath);
        arguments["rowIndex"] = 2;

        // Act
        var result = await _tool.ExecuteAsync(arguments);

        // Assert
        Assert.Contains("\"height\": 25", result);
        Assert.Contains("\"index\": 2", result);
    }

    [Fact]
    public async Task GetInfo_ShouldReturnColumnInfo()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbookWithData("test_get_info_col.xlsx", 3);
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            var worksheet = package.Workbook.Worksheets[0];
            worksheet.Column(2).Width = 15;
            package.Save();
        }

        var arguments = CreateArguments("get_info", workbookPath);
        arguments["columnIndex"] = 2;

        // Act
        var result = await _tool.ExecuteAsync(arguments);

        // Assert
        Assert.Contains("\"width\": 15", result);
        Assert.Contains("\"index\": 2", result);
    }

    [Fact]
    public async Task InvalidOperation_ShouldThrowArgumentException()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbookWithData("test_invalid_op.xlsx", 3);
        var arguments = CreateArguments("invalid_operation", workbookPath);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _tool.ExecuteAsync(arguments));
    }

    [Fact]
    public async Task InsertRows_WithSheetIndex_ShouldInsertInCorrectSheet()
    {
        // Arrange
        var workbookPath = CreateTestFilePath("test_insert_row_sheet.xlsx");
        using (var package = new ExcelPackage())
        {
            var ws1 = package.Workbook.Worksheets.Add("Sheet1");
            ws1.Cells["A1"].Value = "Sheet1";

            var ws2 = package.Workbook.Worksheets.Add("Sheet2");
            ws2.Cells["A1"].Value = "Sheet2-R1";
            ws2.Cells["A2"].Value = "Sheet2-R2";

            package.SaveAs(new FileInfo(workbookPath));
        }

        var outputPath = CreateTestFilePath("test_insert_row_sheet_output.xlsx");
        var arguments = CreateArguments("insert_rows", workbookPath, outputPath);
        arguments["sheetIndex"] = 1;
        arguments["startRow"] = 2;
        arguments["count"] = 1;

        // Act
        await _tool.ExecuteAsync(arguments);

        // Assert
        using var package2 = new ExcelPackage(new FileInfo(outputPath));
        var ws = package2.Workbook.Worksheets[1];
        Assert.Equal("Sheet2-R1", ws.Cells["A1"].Value?.ToString());
        Assert.Equal("Sheet2-R2", ws.Cells["A3"].Value?.ToString());
    }
}

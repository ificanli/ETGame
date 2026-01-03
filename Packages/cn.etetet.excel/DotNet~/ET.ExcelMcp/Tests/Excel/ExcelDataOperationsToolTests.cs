using System.Text.Json.Nodes;
using OfficeOpenXml;
using ET.Test;
using ET.Tools.Excel;

namespace ET.Test;

public class ExcelDataOperationsToolTests : ExcelTestBase
{
    private readonly ExcelDataOperationsTool _tool = new();

    #region Sort Tests

    [Fact]
    public async Task SortData_ShouldSortRange()
    {
        var workbookPath = CreateExcelWorkbook("test_sort.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            var worksheet = package.Workbook.Worksheets[0];
            worksheet.Cells["A1"].Value = "C";
            worksheet.Cells["A2"].Value = "A";
            worksheet.Cells["A3"].Value = "B";
            package.Save();
        }

        var outputPath = CreateTestFilePath("test_sort_output.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "sort",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["range"] = "A1:A3",
            ["sortColumn"] = 0,
            ["ascending"] = true
        };

        await _tool.ExecuteAsync(arguments);

        using var resultPackage = new ExcelPackage(new FileInfo(outputPath));
        var resultWorksheet = resultPackage.Workbook.Worksheets[0];
        Assert.Equal("A", resultWorksheet.Cells["A1"].Value);
        Assert.Equal("B", resultWorksheet.Cells["A2"].Value);
        Assert.Equal("C", resultWorksheet.Cells["A3"].Value);
    }

    [Fact]
    public async Task SortData_WithHasHeader_ShouldSkipHeaderRow()
    {
        var workbookPath = CreateExcelWorkbook("test_sort_with_header.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            var worksheet = package.Workbook.Worksheets[0];
            worksheet.Cells["A1"].Value = "Name";
            worksheet.Cells["A2"].Value = "C";
            worksheet.Cells["A3"].Value = "A";
            worksheet.Cells["A4"].Value = "B";
            package.Save();
        }

        var outputPath = CreateTestFilePath("test_sort_with_header_output.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "sort",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["range"] = "A1:A4",
            ["sortColumn"] = 0,
            ["ascending"] = true,
            ["hasHeader"] = true
        };

        await _tool.ExecuteAsync(arguments);

        using var resultPackage = new ExcelPackage(new FileInfo(outputPath));
        var resultWorksheet = resultPackage.Workbook.Worksheets[0];
        Assert.Equal("Name", resultWorksheet.Cells["A1"].Value);
        Assert.Equal("A", resultWorksheet.Cells["A2"].Value);
        Assert.Equal("B", resultWorksheet.Cells["A3"].Value);
        Assert.Equal("C", resultWorksheet.Cells["A4"].Value);
    }

    [Fact]
    public async Task SortData_Descending_ShouldSortDescending()
    {
        var workbookPath = CreateExcelWorkbook("test_sort_desc.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            var worksheet = package.Workbook.Worksheets[0];
            worksheet.Cells["A1"].Value = "A";
            worksheet.Cells["A2"].Value = "C";
            worksheet.Cells["A3"].Value = "B";
            package.Save();
        }

        var outputPath = CreateTestFilePath("test_sort_desc_output.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "sort",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["range"] = "A1:A3",
            ["sortColumn"] = 0,
            ["ascending"] = false
        };

        await _tool.ExecuteAsync(arguments);

        using var resultPackage = new ExcelPackage(new FileInfo(outputPath));
        var resultWorksheet = resultPackage.Workbook.Worksheets[0];
        Assert.Equal("C", resultWorksheet.Cells["A1"].Value);
        Assert.Equal("B", resultWorksheet.Cells["A2"].Value);
        Assert.Equal("A", resultWorksheet.Cells["A3"].Value);
    }

    #endregion

    #region FindReplace Tests

    [Fact]
    public async Task FindReplace_ShouldReplaceText()
    {
        var workbookPath = CreateExcelWorkbookWithData("test_find_replace.xlsx", 3);
        var outputPath = CreateTestFilePath("test_find_replace_output.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "find_replace",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["findText"] = "R1C1",
            ["replaceText"] = "Replaced"
        };

        await _tool.ExecuteAsync(arguments);

        using var package = new ExcelPackage(new FileInfo(outputPath));
        var worksheet = package.Workbook.Worksheets[0];
        Assert.Equal("Replaced", worksheet.Cells["A1"].Value);
    }

    [Fact]
    public async Task FindReplace_WithSubstring_ShouldNotLoopInfinitely()
    {
        var workbookPath = CreateExcelWorkbook("test_find_replace_substring.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            var worksheet = package.Workbook.Worksheets[0];
            worksheet.Cells["A1"].Value = "Apple";
            worksheet.Cells["A2"].Value = "Apple Pie";
            package.Save();
        }

        var outputPath = CreateTestFilePath("test_find_replace_substring_output.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "find_replace",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["findText"] = "Apple",
            ["replaceText"] = "AppleTree"
        };

        var result = await _tool.ExecuteAsync(arguments);

        Assert.Contains("2", result);
        using var resultPackage = new ExcelPackage(new FileInfo(outputPath));
        var resultWorksheet = resultPackage.Workbook.Worksheets[0];
        Assert.Equal("AppleTree", resultWorksheet.Cells["A1"].Value);
        Assert.Equal("AppleTree Pie", resultWorksheet.Cells["A2"].Value);
    }

    #endregion

    #region BatchWrite Tests

    [Fact]
    public async Task BatchWrite_ShouldWriteMultipleValues()
    {
        var workbookPath = CreateExcelWorkbook("test_batch_write.xlsx");
        var outputPath = CreateTestFilePath("test_batch_write_output.xlsx");
        var data = new JsonObject
        {
            ["A1"] = "Value1",
            ["B1"] = "Value2",
            ["A2"] = "Value3"
        };
        var arguments = new JsonObject
        {
            ["operation"] = "batch_write",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["data"] = data
        };

        await _tool.ExecuteAsync(arguments);

        using var package = new ExcelPackage(new FileInfo(outputPath));
        var worksheet = package.Workbook.Worksheets[0];
        Assert.Equal("Value1", worksheet.Cells["A1"].Value);
        Assert.Equal("Value2", worksheet.Cells["B1"].Value);
        Assert.Equal("Value3", worksheet.Cells["A2"].Value);
    }

    [Fact]
    public async Task BatchWrite_WithArrayFormat_ShouldWriteValues()
    {
        var workbookPath = CreateExcelWorkbook("test_batch_write_array.xlsx");
        var outputPath = CreateTestFilePath("test_batch_write_array_output.xlsx");
        var data = new JsonArray
        {
            new JsonObject { ["cell"] = "A1", ["value"] = "Value1" },
            new JsonObject { ["cell"] = "B1", ["value"] = "Value2" }
        };
        var arguments = new JsonObject
        {
            ["operation"] = "batch_write",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["data"] = data
        };

        await _tool.ExecuteAsync(arguments);

        using var package = new ExcelPackage(new FileInfo(outputPath));
        var worksheet = package.Workbook.Worksheets[0];
        Assert.Equal("Value1", worksheet.Cells["A1"].Value);
        Assert.Equal("Value2", worksheet.Cells["B1"].Value);
    }

    #endregion

    #region GetContent Tests

    [Fact]
    public async Task GetContent_ShouldReturnContent()
    {
        var workbookPath = CreateExcelWorkbookWithData("test_get_content.xlsx", 3);
        var arguments = new JsonObject
        {
            ["operation"] = "get_content",
            ["path"] = workbookPath,
            ["range"] = "A1:B2"
        };

        var result = await _tool.ExecuteAsync(arguments);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("R1C1", result);
    }

    #endregion

    #region GetStatistics Tests

    [Fact]
    public async Task GetStatistics_ShouldReturnStatistics()
    {
        var workbookPath = CreateExcelWorkbook("test_get_statistics.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            var worksheet = package.Workbook.Worksheets[0];
            worksheet.Cells["A1"].Value = 10;
            worksheet.Cells["A2"].Value = 20;
            worksheet.Cells["A3"].Value = 30;
            package.Save();
        }

        var arguments = new JsonObject
        {
            ["operation"] = "get_statistics",
            ["path"] = workbookPath,
            ["range"] = "A1:A3"
        };

        var result = await _tool.ExecuteAsync(arguments);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("sum", result, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region GetUsedRange Tests

    [Fact]
    public async Task GetUsedRange_ShouldReturnUsedRange()
    {
        var workbookPath = CreateExcelWorkbookWithData("test_get_used_range.xlsx", 3);
        var arguments = new JsonObject
        {
            ["operation"] = "get_used_range",
            ["path"] = workbookPath
        };

        var result = await _tool.ExecuteAsync(arguments);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("range", result, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task UnknownOperation_ShouldThrowException()
    {
        var workbookPath = CreateExcelWorkbook("test_unknown_op.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "unknown",
            ["path"] = workbookPath
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _tool.ExecuteAsync(arguments));
        Assert.Contains("未知操作", ex.Message);
    }

    #endregion
}

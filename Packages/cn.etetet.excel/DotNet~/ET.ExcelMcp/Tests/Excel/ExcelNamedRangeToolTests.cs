using System.Text.Json;
using System.Text.Json.Nodes;
using OfficeOpenXml;
using ET.Test;
using ET.Tools.Excel;

namespace ET.Test;

[TestClass]
public class ExcelNamedRangeToolTests : ExcelTestBase
{
    private readonly ExcelNamedRangeTool _tool = new();

    #region Add Tests

    [TestMethod]
    public async Task AddNamedRange_ShouldAddNamedRange()
    {
        var workbookPath = CreateExcelWorkbookWithData("test_add_named_range.xlsx", 5, 5);
        var outputPath = CreateTestFilePath("test_add_named_range_output.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "add",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["name"] = "TestRange",
            ["range"] = "A1:C5"
        };

        var result = await _tool.ExecuteAsync(arguments);

        Assert.Contains("命名范围 'TestRange' 已添加", result);
        Assert.Contains("引用:", result);

        using var package = new ExcelPackage(new FileInfo(outputPath));
        Assert.NotNull(package.Workbook.Names["TestRange"]);
    }

    [TestMethod]
    public async Task AddNamedRange_SingleCell_ShouldAddRange()
    {
        var workbookPath = CreateExcelWorkbook("test_add_single_cell.xlsx");
        var outputPath = CreateTestFilePath("test_add_single_cell_output.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "add",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["name"] = "SingleCell",
            ["range"] = "A1"
        };

        var result = await _tool.ExecuteAsync(arguments);

        Assert.Contains("命名范围 'SingleCell' 已添加", result);

        using var package = new ExcelPackage(new FileInfo(outputPath));
        Assert.NotNull(package.Workbook.Names["SingleCell"]);
    }

    [TestMethod]
    public async Task AddNamedRange_WithSheetReference_ShouldAddToCorrectSheet()
    {
        var workbookPath = CreateExcelWorkbook("test_add_sheet_ref.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            package.Workbook.Worksheets.Add("DataSheet");
            package.Save();
        }

        var outputPath = CreateTestFilePath("test_add_sheet_ref_output.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "add",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["name"] = "SheetRange",
            ["range"] = "DataSheet!A1:C5"
        };

        var result = await _tool.ExecuteAsync(arguments);

        Assert.Contains("命名范围 'SheetRange' 已添加", result);

        using var resultPackage = new ExcelPackage(new FileInfo(outputPath));
        var namedRange = resultPackage.Workbook.Names["SheetRange"];
        Assert.NotNull(namedRange);
    }

    [TestMethod]
    public async Task AddNamedRange_DuplicateName_ShouldThrowException()
    {
        var workbookPath = CreateExcelWorkbook("test_add_duplicate.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            package.Workbook.Names.Add("ExistingRange", package.Workbook.Worksheets[0].Cells["A1:B2"]);
            package.Save();
        }

        var arguments = new JsonObject
        {
            ["operation"] = "add",
            ["path"] = workbookPath,
            ["name"] = "ExistingRange",
            ["range"] = "C1:D2"
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _tool.ExecuteAsync(arguments));
        Assert.Contains("已存在", exception.Message);
    }

    [TestMethod]
    public async Task AddNamedRange_InvalidSheetIndex_ShouldThrowException()
    {
        var workbookPath = CreateExcelWorkbook("test_add_invalid_sheet.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "add",
            ["path"] = workbookPath,
            ["name"] = "InvalidSheet",
            ["range"] = "A1:B2",
            ["sheetIndex"] = 99
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _tool.ExecuteAsync(arguments));
    }

    [TestMethod]
    public async Task AddNamedRange_InvalidSheetReference_ShouldThrowException()
    {
        var workbookPath = CreateExcelWorkbook("test_add_invalid_sheet_ref.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "add",
            ["path"] = workbookPath,
            ["name"] = "InvalidRef",
            ["range"] = "NonExistentSheet!A1:B2"
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _tool.ExecuteAsync(arguments));
        Assert.Contains("不存在", exception.Message);
    }

    [TestMethod]
    public async Task AddNamedRange_WithSheetIndex_ShouldAddToCorrectSheet()
    {
        var workbookPath = CreateExcelWorkbook("test_add_with_sheet_index.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            package.Workbook.Worksheets.Add("Sheet2");
            package.Save();
        }

        var outputPath = CreateTestFilePath("test_add_with_sheet_index_output.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "add",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["name"] = "Sheet2Range",
            ["range"] = "A1:C5",
            ["sheetIndex"] = 1
        };

        var result = await _tool.ExecuteAsync(arguments);

        Assert.Contains("命名范围 'Sheet2Range' 已添加", result);

        using var resultPackage2 = new ExcelPackage(new FileInfo(outputPath));
        var namedRange = resultPackage2.Workbook.Names["Sheet2Range"];
        Assert.NotNull(namedRange);
    }

    #endregion

    #region Delete Tests

    [TestMethod]
    public async Task DeleteNamedRange_ShouldDeleteNamedRange()
    {
        var workbookPath = CreateExcelWorkbook("test_delete_named_range.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            package.Workbook.Names.Add("RangeToDelete", package.Workbook.Worksheets[0].Cells["A1:B2"]);
            package.Save();
        }

        var outputPath = CreateTestFilePath("test_delete_named_range_output.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "delete",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["name"] = "RangeToDelete"
        };

        var result = await _tool.ExecuteAsync(arguments);

        Assert.Contains("命名范围 'RangeToDelete' 已删除", result);

        using var resultPackage = new ExcelPackage(new FileInfo(outputPath));
        Assert.False(resultPackage.Workbook.Names.ContainsKey("RangeToDelete"));
    }

    [TestMethod]
    public async Task DeleteNamedRange_NonExistent_ShouldThrowException()
    {
        var workbookPath = CreateExcelWorkbook("test_delete_nonexistent.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "delete",
            ["path"] = workbookPath,
            ["name"] = "NonExistentRange"
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _tool.ExecuteAsync(arguments));
        Assert.Contains("不存在", exception.Message);
    }

    #endregion

    #region Get Tests

    [TestMethod]
    public async Task GetNamedRanges_ShouldReturnAllNamedRanges()
    {
        var workbookPath = CreateExcelWorkbook("test_get_named_ranges.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            package.Workbook.Names.Add("Range1", package.Workbook.Worksheets[0].Cells["A1:B2"]);
            package.Workbook.Names.Add("Range2", package.Workbook.Worksheets[0].Cells["C1:D2"]);
            package.Save();
        }

        var arguments = new JsonObject
        {
            ["operation"] = "get",
            ["path"] = workbookPath
        };

        var result = await _tool.ExecuteAsync(arguments);

        var json = JsonDocument.Parse(result);
        var root = json.RootElement;

        Assert.Equal(2, root.GetProperty("count").GetInt32());
        var items = root.GetProperty("items");
        Assert.Equal(2, items.GetArrayLength());

        Assert.Contains("Range1", result);
        Assert.Contains("Range2", result);
    }

    [TestMethod]
    public async Task GetNamedRanges_WithNoNamedRanges_ShouldReturnEmptyMessage()
    {
        var workbookPath = CreateExcelWorkbook("test_get_empty_named_ranges.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "get",
            ["path"] = workbookPath
        };

        var result = await _tool.ExecuteAsync(arguments);

        var json = JsonDocument.Parse(result);
        var root = json.RootElement;

        Assert.Equal(0, root.GetProperty("count").GetInt32());
        Assert.Equal("未找到命名范围", root.GetProperty("message").GetString());
    }

    [TestMethod]
    public async Task GetNamedRanges_ShouldIncludeAllProperties()
    {
        var workbookPath = CreateExcelWorkbook("test_get_properties.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            package.Workbook.Names.Add("DetailedRange", package.Workbook.Worksheets[0].Cells["A1:B2"]);
            package.Save();
        }

        var arguments = new JsonObject
        {
            ["operation"] = "get",
            ["path"] = workbookPath
        };

        var result = await _tool.ExecuteAsync(arguments);

        var json = JsonDocument.Parse(result);
        var items = json.RootElement.GetProperty("items");
        var firstItem = items[0];

        Assert.True(firstItem.TryGetProperty("name", out _));
        Assert.True(firstItem.TryGetProperty("reference", out _));
        Assert.Equal("DetailedRange", firstItem.GetProperty("name").GetString());
    }

    #endregion

    #region Error Handling Tests

    [TestMethod]
    public async Task ExecuteAsync_InvalidOperation_ShouldThrowException()
    {
        var workbookPath = CreateExcelWorkbook("test_invalid_op.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "invalid",
            ["path"] = workbookPath
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _tool.ExecuteAsync(arguments));
        Assert.Contains("未知操作", exception.Message);
    }

    #endregion
}



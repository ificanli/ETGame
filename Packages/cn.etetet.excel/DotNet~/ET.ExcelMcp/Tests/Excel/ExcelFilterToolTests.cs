using System.Text.Json;
using System.Text.Json.Nodes;
using OfficeOpenXml;
using ET.Test;
using ET.Tools.Excel;

namespace ET.Test;

public class ExcelFilterToolTests : ExcelTestBase
{
    private readonly ExcelFilterTool _tool = new();

    #region Apply Tests

    [Fact]
    public async Task ApplyFilter_ShouldApplyAutoFilter()
    {
        var workbookPath = CreateExcelWorkbookWithData("test_apply_filter.xlsx");
        var outputPath = CreateTestFilePath("test_apply_filter_output.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "apply",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["range"] = "A1:C5"
        };

        var result = await _tool.ExecuteAsync(arguments);

        Assert.Contains("已应用自动筛选", result);
        Assert.Contains("A1:C5", result);

        using var package = new ExcelPackage(new FileInfo(outputPath));
        var worksheet = package.Workbook.Worksheets[0];
        Assert.NotNull(worksheet.AutoFilter.Address);
    }

    [Fact]
    public async Task ApplyFilter_WithSheetIndex_ShouldApplyToCorrectSheet()
    {
        var workbookPath = CreateExcelWorkbookWithData("test_apply_filter_sheet.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            package.Workbook.Worksheets.Add("Sheet2");
            package.Workbook.Worksheets[1].Cells["A1"].Value = "Header";
            package.Workbook.Worksheets[1].Cells["A2"].Value = "Data";
            package.Save();
        }

        var outputPath = CreateTestFilePath("test_apply_filter_sheet_output.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "apply",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["sheetIndex"] = 1,
            ["range"] = "A1:A2"
        };

        var result = await _tool.ExecuteAsync(arguments);

        Assert.Contains("工作表 1", result);
    }

    #endregion

    #region Remove Tests

    [Fact]
    public async Task RemoveFilter_ShouldRemoveAutoFilter()
    {
        var workbookPath = CreateExcelWorkbookWithData("test_remove_filter.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            package.Workbook.Worksheets[0].Cells["A1:C5"].AutoFilter = true;
            package.Save();
        }

        var outputPath = CreateTestFilePath("test_remove_filter_output.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "remove",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath
        };

        var result = await _tool.ExecuteAsync(arguments);

        Assert.Contains("已移除", result);

        using var resultPackage = new ExcelPackage(new FileInfo(outputPath));
        Assert.Null(resultPackage.Workbook.Worksheets[0].AutoFilter.Address);
    }

    [Fact]
    public async Task RemoveFilter_NoExistingFilter_ShouldSucceed()
    {
        var workbookPath = CreateExcelWorkbookWithData("test_remove_no_filter.xlsx", 3);
        var outputPath = CreateTestFilePath("test_remove_no_filter_output.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "remove",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath
        };

        var result = await _tool.ExecuteAsync(arguments);

        Assert.Contains("已移除", result);
    }

    #endregion

    #region Filter by Value Tests

    [Fact]
    public async Task Filter_ByValue_ShouldApplyCriteria()
    {
        var workbookPath = CreateExcelWorkbookWithData("test_filter_value.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            package.Workbook.Worksheets[0].Cells["A1"].Value = "Status";
            package.Workbook.Worksheets[0].Cells["A2"].Value = "Active";
            package.Workbook.Worksheets[0].Cells["A3"].Value = "Inactive";
            package.Workbook.Worksheets[0].Cells["A4"].Value = "Active";
            package.Workbook.Worksheets[0].Cells["A5"].Value = "Pending";
            package.Save();
        }

        var outputPath = CreateTestFilePath("test_filter_value_output.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "filter",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["range"] = "A1:A5",
            ["columnIndex"] = 0,
            ["criteria"] = "Active"
        };

        var result = await _tool.ExecuteAsync(arguments);

        Assert.Contains("已对列 0 应用筛选条件", result);
        Assert.Contains("'Active'", result);
    }

    [Fact]
    public async Task Filter_WithGreaterThanOperator_ShouldApplyCustomFilter()
    {
        var workbookPath = CreateExcelWorkbookWithData("test_filter_gt.xlsx", 5, 2);
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            package.Workbook.Worksheets[0].Cells["B1"].Value = "Amount";
            package.Workbook.Worksheets[0].Cells["B2"].Value = 50;
            package.Workbook.Worksheets[0].Cells["B3"].Value = 150;
            package.Workbook.Worksheets[0].Cells["B4"].Value = 75;
            package.Workbook.Worksheets[0].Cells["B5"].Value = 200;
            package.Save();
        }

        var outputPath = CreateTestFilePath("test_filter_gt_output.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "filter",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["range"] = "A1:B5",
            ["columnIndex"] = 1,
            ["criteria"] = "100",
            ["filterOperator"] = "GreaterThan"
        };

        var result = await _tool.ExecuteAsync(arguments);

        Assert.Contains("已对列 1 应用筛选条件", result);
        Assert.Contains("GreaterThan", result);
    }

    [Fact]
    public async Task Filter_InvalidOperator_ShouldThrowException()
    {
        var workbookPath = CreateExcelWorkbookWithData("test_filter_invalid_op.xlsx", 3, 2);
        var arguments = new JsonObject
        {
            ["operation"] = "filter",
            ["path"] = workbookPath,
            ["range"] = "A1:A3",
            ["columnIndex"] = 0,
            ["criteria"] = "test",
            ["filterOperator"] = "InvalidOperator"
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _tool.ExecuteAsync(arguments));
        Assert.Contains("不支持的筛选运算符", ex.Message);
    }

    #endregion

    #region Get Status Tests

    [Fact]
    public async Task GetFilterStatus_WithFilter_ShouldReturnEnabled()
    {
        var workbookPath = CreateExcelWorkbookWithData("test_get_status_enabled.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            package.Workbook.Worksheets[0].Cells["A1:C5"].AutoFilter = true;
            package.Save();
        }

        var arguments = new JsonObject
        {
            ["operation"] = "get_status",
            ["path"] = workbookPath
        };

        var result = await _tool.ExecuteAsync(arguments);

        Assert.NotNull(result);
        var json = JsonDocument.Parse(result);
        Assert.True(json.RootElement.GetProperty("isFilterEnabled").GetBoolean());
        Assert.Contains("A1:C5", json.RootElement.GetProperty("filterRange").GetString());
    }

    [Fact]
    public async Task GetFilterStatus_WithoutFilter_ShouldReturnDisabled()
    {
        var workbookPath = CreateExcelWorkbookWithData("test_get_status_disabled.xlsx", 3);
        var arguments = new JsonObject
        {
            ["operation"] = "get_status",
            ["path"] = workbookPath
        };

        var result = await _tool.ExecuteAsync(arguments);

        var json = JsonDocument.Parse(result);
        Assert.False(json.RootElement.GetProperty("isFilterEnabled").GetBoolean());
        Assert.Contains("未启用", json.RootElement.GetProperty("status").GetString());
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task UnknownOperation_ShouldThrowException()
    {
        var workbookPath = CreateExcelWorkbookWithData("test_unknown_op.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "unknown",
            ["path"] = workbookPath
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _tool.ExecuteAsync(arguments));
        Assert.Contains("未知操作", ex.Message);
    }

    [Fact]
    public async Task InvalidSheetIndex_ShouldThrowException()
    {
        var workbookPath = CreateExcelWorkbookWithData("test_invalid_sheet.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "apply",
            ["path"] = workbookPath,
            ["sheetIndex"] = 99,
            ["range"] = "A1:C5"
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _tool.ExecuteAsync(arguments));
        Assert.Contains("Invalid sheet index", ex.Message);
    }

    #endregion
}

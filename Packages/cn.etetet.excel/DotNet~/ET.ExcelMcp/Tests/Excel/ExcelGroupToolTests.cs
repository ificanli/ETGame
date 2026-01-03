using System.Text.Json.Nodes;
using ET.Test;
using ET.Tools.Excel;
using OfficeOpenXml;

namespace ET.Test;

[TestClass]
public class ExcelGroupToolTests : ExcelTestBase
{
    private readonly ExcelGroupTool _tool = new();

    [TestMethod]
    public async Task GroupRows_ShouldIncreaseOutlineLevel()
    {
        var workbookPath = CreateExcelWorkbookWithData("group_rows.xlsx", 10, 3);
        var outputPath = CreateTestFilePath("group_rows_out.xlsx");
        var args = new JsonObject
        {
            ["operation"] = "group_rows",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["startRow"] = 2,
            ["endRow"] = 4,
            ["isCollapsed"] = true
        };

        await _tool.ExecuteAsync(args);
        using var package = new ExcelPackage(new FileInfo(outputPath));
        var row = package.Workbook.Worksheets[0].Row(2);
        Assert.True(row.OutlineLevel > 0);
        Assert.True(row.Hidden);
    }

    [TestMethod]
    public async Task UngroupRows_ShouldResetOutline()
    {
        var workbookPath = CreateExcelWorkbookWithData("ungroup_rows.xlsx", 10, 3);
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            var ws = package.Workbook.Worksheets[0];
            ws.Row(2).OutlineLevel = 2;
            ws.Row(2).Hidden = true;
            package.Save();
        }

        var outputPath = CreateTestFilePath("ungroup_rows_out.xlsx");
        var args = new JsonObject
        {
            ["operation"] = "ungroup_rows",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["startRow"] = 2,
            ["endRow"] = 2
        };

        await _tool.ExecuteAsync(args);
        using var packageOut = new ExcelPackage(new FileInfo(outputPath));
        Assert.Equal(1, packageOut.Workbook.Worksheets[0].Row(2).OutlineLevel);
        Assert.False(packageOut.Workbook.Worksheets[0].Row(2).Hidden);
    }

    [TestMethod]
    public async Task GroupColumns_ShouldUpdateOutline()
    {
        var workbookPath = CreateExcelWorkbookWithData("group_cols.xlsx", 5, 10);
        var outputPath = CreateTestFilePath("group_cols_out.xlsx");
        var args = new JsonObject
        {
            ["operation"] = "group_columns",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["startColumn"] = 2,
            ["endColumn"] = 4
        };

        await _tool.ExecuteAsync(args);
        using var package = new ExcelPackage(new FileInfo(outputPath));
        Assert.True(package.Workbook.Worksheets[0].Column(2).OutlineLevel > 0);
    }
}



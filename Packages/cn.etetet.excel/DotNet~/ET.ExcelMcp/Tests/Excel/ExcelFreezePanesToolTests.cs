using System.Text.Json;
using System.Text.Json.Nodes;
using ET.Test;
using ET.Tools.Excel;
using OfficeOpenXml;

namespace ET.Test;

public class ExcelFreezePanesToolTests : ExcelTestBase
{
    private readonly ExcelFreezePanesTool _tool = new();

    [Fact]
    public async Task Freeze_ShouldUpdateView()
    {
        var workbookPath = CreateExcelWorkbookWithData("freeze.xlsx");
        var outputPath = CreateTestFilePath("freeze_out.xlsx");
        var args = new JsonObject
        {
            ["operation"] = "freeze",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["row"] = 2,
            ["column"] = 1
        };

        await _tool.ExecuteAsync(args);
        using var package = new ExcelPackage(new FileInfo(outputPath));
        var paneState = package.Workbook.Worksheets[0].View.PaneSettings.State;
        Assert.Equal(ePaneState.Frozen, paneState);
    }

    [Fact]
    public async Task Unfreeze_ShouldClearState()
    {
        var workbookPath = CreateExcelWorkbookWithData("unfreeze.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            package.Workbook.Worksheets[0].View.FreezePanes(2, 2);
            package.Save();
        }

        var outputPath = CreateTestFilePath("unfreeze_out.xlsx");
        var args = new JsonObject
        {
            ["operation"] = "unfreeze",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath
        };

        await _tool.ExecuteAsync(args);
        using var packageOut = new ExcelPackage(new FileInfo(outputPath));
        var paneSettings = packageOut.Workbook.Worksheets[0].View.PaneSettings;
        if (paneSettings == null)
        {
            Assert.True(true);
        }
        else
        {
            var state = paneSettings.State;
            Assert.NotEqual(ePaneState.Frozen, state);
            Assert.NotEqual(ePaneState.FrozenSplit, state);
        }
    }

    [Fact]
    public async Task GetStatus_ShouldReturnJson()
    {
        var workbookPath = CreateExcelWorkbookWithData("freeze_status.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            package.Workbook.Worksheets[0].View.FreezePanes(3, 2);
            package.Save();
        }

        var args = new JsonObject
        {
            ["operation"] = "get",
            ["path"] = workbookPath
        };

        var result = await _tool.ExecuteAsync(args);
        using var json = JsonDocument.Parse(result);
        Assert.True(json.RootElement.GetProperty("isFrozen").GetBoolean());
        Assert.Equal(2, json.RootElement.GetProperty("frozenRow").GetInt32());
        Assert.Equal(1, json.RootElement.GetProperty("frozenColumn").GetInt32());
    }
}

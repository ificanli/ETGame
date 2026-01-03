using System.Text.Json;
using System.Text.Json.Nodes;
using ET.Test;
using ET.Tools.Excel;
using OfficeOpenXml;

namespace ET.Test;

public class ExcelViewSettingsToolTests : ExcelTestBase
{
    private readonly ExcelViewSettingsTool _tool = new();

    [Fact]
    public async Task SetViewSettings_ShouldPersistChanges()
    {
        var workbookPath = CreateExcelWorkbook("view_set.xlsx");
        var outputPath = CreateTestFilePath("view_set_out.xlsx");

        var args = new JsonObject
        {
            ["operation"] = "set",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["zoom"] = 150,
            ["showGridLines"] = false,
            ["showHeadings"] = false,
            ["viewType"] = "pagebreak"
        };

        await _tool.ExecuteAsync(args);

        using var package = new ExcelPackage(new FileInfo(outputPath));
        var view = package.Workbook.Worksheets[0].View;
        Assert.Equal(150, view.ZoomScale);
        Assert.False(view.ShowGridLines);
        Assert.False(view.ShowHeaders);
        Assert.True(view.PageBreakView);
        Assert.False(view.PageLayoutView);
    }

    [Fact]
    public async Task GetViewSettings_ShouldReturnJsonSnapshot()
    {
        var workbookPath = CreateExcelWorkbook("view_get.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            var view = package.Workbook.Worksheets[0].View;
            view.ZoomScale = 125;
            view.ShowGridLines = false;
            view.PageBreakView = true;
            package.Save();
        }

        var args = new JsonObject
        {
            ["operation"] = "get",
            ["path"] = workbookPath
        };

        var result = await _tool.ExecuteAsync(args);
        using var json = JsonDocument.Parse(result);
        var root = json.RootElement;
        Assert.Equal(125, root.GetProperty("zoom").GetInt32());
        Assert.False(root.GetProperty("showGridLines").GetBoolean());
        Assert.Equal("PageBreak", root.GetProperty("viewType").GetString());
    }
}

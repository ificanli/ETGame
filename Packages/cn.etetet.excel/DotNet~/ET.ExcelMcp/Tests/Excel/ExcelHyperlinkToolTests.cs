using System.Text.Json;
using System.Text.Json.Nodes;
using ET.Test;
using ET.Tools.Excel;
using OfficeOpenXml;

namespace ET.Test;

public class ExcelHyperlinkToolTests : ExcelTestBase
{
    private readonly ExcelHyperlinkTool _tool = new();

    [Fact]
    public async Task AddHyperlink_ShouldPersist()
    {
        var workbookPath = CreateExcelWorkbook("hyperlink_add.xlsx");
        var outputPath = CreateTestFilePath("hyperlink_add_out.xlsx");

        var args = new JsonObject
        {
            ["operation"] = "add",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["cell"] = "A1",
            ["url"] = "https://example.com",
            ["displayText"] = "Example"
        };

        await _tool.ExecuteAsync(args);

        using var package = new ExcelPackage(new FileInfo(outputPath));
        var hyperlink = package.Workbook.Worksheets[0].Cells["A1"].Hyperlink as Uri;
        Assert.Equal("https://example.com", hyperlink?.ToString().TrimEnd('/'));
    }

    [Fact]
    public async Task GetHyperlinks_ShouldReturnItems()
    {
        var workbookPath = CreateExcelWorkbook("hyperlink_get.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            var ws = package.Workbook.Worksheets[0];
            ws.Cells["A1"].Hyperlink = new Uri("https://one.com");
            ws.Cells["A1"].Value = "One";
            ws.Cells["B2"].Hyperlink = new Uri("https://two.com");
            ws.Cells["B2"].Value = "Two";
            package.Save();
        }

        var args = new JsonObject
        {
            ["operation"] = "get",
            ["path"] = workbookPath
        };

        var result = await _tool.ExecuteAsync(args);
        using var json = JsonDocument.Parse(result);
        Assert.Equal(2, json.RootElement.GetProperty("count").GetInt32());
    }

    [Fact]
    public async Task EditHyperlink_ShouldUpdateUrl()
    {
        var workbookPath = CreateExcelWorkbook("hyperlink_edit.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            var ws = package.Workbook.Worksheets[0];
            ws.Cells["A1"].Hyperlink = new Uri("https://old.com");
            ws.Cells["A1"].Value = "Old";
            package.Save();
        }

        var outputPath = CreateTestFilePath("hyperlink_edit_out.xlsx");
        var args = new JsonObject
        {
            ["operation"] = "edit",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["cell"] = "A1",
            ["url"] = "https://new.com",
            ["displayText"] = "New"
        };

        await _tool.ExecuteAsync(args);
        using var packageOut = new ExcelPackage(new FileInfo(outputPath));
        var cell = packageOut.Workbook.Worksheets[0].Cells["A1"];
        Assert.Equal("New", cell.Text);
        Assert.Equal("https://new.com", (cell.Hyperlink as Uri)?.ToString().TrimEnd('/'));
    }

    [Fact]
    public async Task DeleteHyperlink_ShouldRemoveLink()
    {
        var workbookPath = CreateExcelWorkbook("hyperlink_delete.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            var ws = package.Workbook.Worksheets[0];
            ws.Cells["A1"].Hyperlink = new Uri("https://test.com");
            ws.Cells["A1"].Value = "Test";
            package.Save();
        }

        var outputPath = CreateTestFilePath("hyperlink_delete_out.xlsx");
        var args = new JsonObject
        {
            ["operation"] = "delete",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["cell"] = "A1"
        };

        await _tool.ExecuteAsync(args);
        using var packageOut = new ExcelPackage(new FileInfo(outputPath));
        Assert.Null(packageOut.Workbook.Worksheets[0].Cells["A1"].Hyperlink);
    }

    [Fact]
    public async Task AddExistingHyperlink_ShouldThrow()
    {
        var workbookPath = CreateExcelWorkbook("hyperlink_existing.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            var ws = package.Workbook.Worksheets[0];
            ws.Cells["A1"].Hyperlink = new Uri("https://test.com");
            package.Save();
        }

        var args = new JsonObject
        {
            ["operation"] = "add",
            ["path"] = workbookPath,
            ["cell"] = "A1",
            ["url"] = "https://new.com"
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _tool.ExecuteAsync(args));
    }
}

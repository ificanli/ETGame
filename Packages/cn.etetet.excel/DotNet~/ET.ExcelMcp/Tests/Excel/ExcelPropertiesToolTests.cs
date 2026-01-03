using System.Drawing;
using System.Text.Json;
using System.Text.Json.Nodes;
using ET.Test;
using ET.Tools.Excel;
using OfficeOpenXml;

namespace ET.Test;

[TestClass]
public class ExcelPropertiesToolTests : ExcelTestBase
{
    private readonly ExcelPropertiesTool _tool = new();

    private string CreateWorkbook(string fileName, Action<ExcelPackage>? configure = null)
    {
        var path = CreateTestFilePath(fileName);
        using var package = new ExcelPackage();
        package.Workbook.Worksheets.Add("Sheet1");
        configure?.Invoke(package);
        package.SaveAs(new FileInfo(path));
        return path;
    }

    [TestMethod]
    public async Task GetWorkbookProperties_ShouldReturnMetadata()
    {
        var path = CreateWorkbook("props_get.xlsx", package =>
        {
            var props = package.Workbook.Properties;
            props.Title = "月度报告";
            props.Author = "Tester";
            props.SetCustomPropertyValue("Department", "QA");
        });

        var args = CreateArguments("get_workbook_properties", path);
        var result = await _tool.ExecuteAsync(args);

        using var json = JsonDocument.Parse(result);
        var root = json.RootElement;
        Assert.Equal("月度报告", root.GetProperty("title").GetString());
        Assert.Equal("Tester", root.GetProperty("author").GetString());
        Assert.Equal(1, root.GetProperty("customProperties").GetArrayLength());
    }

    [TestMethod]
    public async Task SetWorkbookProperties_ShouldPersistValues()
    {
        var path = CreateWorkbook("props_set.xlsx");
        var outputPath = CreateTestFilePath("props_set_out.xlsx");
        var args = CreateArguments("set_workbook_properties", path, outputPath);
        args["title"] = "Sales";
        args["author"] = "Alice";
        args["keywords"] = "report,2025";
        args["customProperties"] = new JsonObject
        {
            ["Department"] = "Finance",
            ["Reviewed"] = true
        };

        await _tool.ExecuteAsync(args);

        using var package = new ExcelPackage(new FileInfo(outputPath));
        var props = package.Workbook.Properties;
        Assert.Equal("Sales", props.Title);
        Assert.Equal("Alice", props.Author);
        Assert.Equal("Finance", props.GetCustomPropertyValue("Department"));
    }

    [TestMethod]
    public async Task EditSheetProperties_ShouldApplyChanges()
    {
        var path = CreateWorkbook("props_sheet_edit.xlsx", package =>
        {
            package.Workbook.Worksheets.Add("Sheet2");
        });
        var outputPath = CreateTestFilePath("props_sheet_edit_out.xlsx");
        var args = CreateArguments("edit_sheet_properties", path, outputPath);
        args["sheetIndex"] = 0;
        args["name"] = "Summary";
        args["isVisible"] = false;
        args["tabColor"] = "#00FF00";

        await _tool.ExecuteAsync(args);

        using var package = new ExcelPackage(new FileInfo(outputPath));
        var worksheet = package.Workbook.Worksheets[0];
        Assert.Equal("Summary", worksheet.Name);
        Assert.Equal(eWorkSheetHidden.Hidden, worksheet.Hidden);
        Assert.Equal(Color.FromArgb(0, 255, 0), worksheet.TabColor);
    }

    [TestMethod]
    public async Task GetSheetProperties_ShouldReportUsage()
    {
        var path = CreateWorkbook("props_sheet_get.xlsx", package =>
        {
            var sheet = package.Workbook.Worksheets[0];
            sheet.Cells["A1"].Value = "Hello";
            sheet.Cells["B1"].Value = "World";
        });

        var args = CreateArguments("get_sheet_properties", path);
        args["sheetIndex"] = 0;
        var result = await _tool.ExecuteAsync(args);

        Assert.Contains("\"name\": \"Sheet1\"", result);
        Assert.Contains("\"dataRowCount\": 1", result);
    }

    [TestMethod]
    public async Task GetSheetInfo_ShouldListAllSheets()
    {
        var path = CreateTestFilePath("props_sheet_info.xlsx");
        using (var package = new ExcelPackage())
        {
            package.Workbook.Worksheets.Add("Sheet1");
            package.Workbook.Worksheets.Add("Sheet2");
            package.SaveAs(new FileInfo(path));
        }

        var args = CreateArguments("get_sheet_info", path);
        var result = await _tool.ExecuteAsync(args);

        using var json = JsonDocument.Parse(result);
        var root = json.RootElement;
        Assert.Equal(2, root.GetProperty("count").GetInt32());
        Assert.Equal(2, root.GetProperty("totalWorksheets").GetInt32());
    }
}



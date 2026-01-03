using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using ET.Test;
using ET.Tools.Excel;
using OfficeOpenXml;

namespace ET.Test;

[TestClass]
public class ExcelPivotTableToolTests : ExcelTestBase
{
    private readonly ExcelPivotTableTool _tool = new();

    [TestMethod]
    public async Task CreatePivotTable_ShouldAddPivotWithFields()
    {
        var workbookPath = CreateWorkbookWithSourceData("pivot_create.xlsx");
        var outputPath = CreateTestFilePath("pivot_create_output.xlsx");
        var arguments = CreateArguments("create", workbookPath, outputPath);
        arguments["dataRange"] = "A1:D6";
        arguments["rowFields"] = new JsonArray("地区");
        arguments["columnFields"] = new JsonArray("产品");
        arguments["dataFields"] = new JsonArray(
            new JsonObject
            {
                ["field"] = "销售额",
                ["function"] = "sum",
                ["name"] = "销售额合计"
            });

        await _tool.ExecuteAsync(arguments);

        using var package = new ExcelPackage(new FileInfo(outputPath));
        var worksheet = package.Workbook.Worksheets[0];
        var pivot = worksheet.PivotTables.Single();

        Assert.Single(pivot.RowFields);
        Assert.Single(pivot.ColumnFields);
        Assert.Single(pivot.DataFields);
        Assert.Equal("销售额合计", pivot.DataFields[0].Name);
    }

    [TestMethod]
    public async Task ConfigurePivotTable_ShouldReplaceFields()
    {
        var workbookPath = CreateWorkbookWithSourceData("pivot_configure.xlsx");
        var pivotPath = CreateTestFilePath("pivot_configure_with_table.xlsx");
        var createArgs = CreateArguments("create", workbookPath, pivotPath);
        createArgs["dataRange"] = "A1:D6";
        createArgs["rowFields"] = new JsonArray("地区");
        createArgs["dataFields"] = new JsonArray(
            new JsonObject { ["field"] = "销售额", ["function"] = "sum" });
        await _tool.ExecuteAsync(createArgs);

        var outputPath = CreateTestFilePath("pivot_configure_output.xlsx");
        var configureArgs = CreateArguments("configure", pivotPath, outputPath);
        configureArgs["pivotIndex"] = 0;
        configureArgs["rowFields"] = new JsonArray("产品");
        configureArgs["columnFields"] = new JsonArray("地区");
        configureArgs["dataFields"] = new JsonArray(
            new JsonObject { ["field"] = "数量", ["function"] = "count", ["name"] = "订单数" });

        await _tool.ExecuteAsync(configureArgs);

        using var package = new ExcelPackage(new FileInfo(outputPath));
        var worksheet = package.Workbook.Worksheets[0];
        var pivot = worksheet.PivotTables.Single();

        Assert.Equal("订单数", pivot.DataFields[0].Name);
        Assert.Single(pivot.RowFields);
        Assert.Single(pivot.ColumnFields);
        Assert.Equal("产品", pivot.RowFields[0].Name);
    }

    [TestMethod]
    public async Task RefreshPivotTable_ShouldReturnMessage()
    {
        var workbookPath = CreateWorkbookWithSourceData("pivot_refresh.xlsx");
        var pivotPath = CreateTestFilePath("pivot_refresh_with_table.xlsx");
        var createArgs = CreateArguments("create", workbookPath, pivotPath);
        createArgs["dataRange"] = "A1:D6";
        await _tool.ExecuteAsync(createArgs);

        var refreshArgs = CreateArguments("refresh", pivotPath, CreateTestFilePath("pivot_refresh_output.xlsx"));
        refreshArgs["pivotIndex"] = 0;
        var result = await _tool.ExecuteAsync(refreshArgs);

        Assert.Contains("已刷新数据透视表", result);
    }

    [TestMethod]
    public async Task GetPivotTables_ShouldReturnJsonPayload()
    {
        var workbookPath = CreateWorkbookWithSourceData("pivot_get.xlsx");
        var pivotPath = CreateTestFilePath("pivot_get_with_table.xlsx");
        var createArgs = CreateArguments("create", workbookPath, pivotPath);
        createArgs["dataRange"] = "A1:D6";
        createArgs["rowFields"] = new JsonArray("地区");
        createArgs["dataFields"] = new JsonArray(
            new JsonObject { ["field"] = "数量", ["function"] = "count" });
        await _tool.ExecuteAsync(createArgs);

        var getArgs = CreateArguments("get", pivotPath);
        var payload = await _tool.ExecuteAsync(getArgs);

        using var doc = JsonDocument.Parse(payload);
        Assert.Equal(1, doc.RootElement.GetProperty("count").GetInt32());
        Assert.Equal("成功获取数据透视表信息", doc.RootElement.GetProperty("message").GetString());
    }

    private string CreateWorkbookWithSourceData(string fileName)
    {
        var filePath = CreateTestFilePath(fileName);
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Sheet1");

        worksheet.Cells["A1"].Value = "地区";
        worksheet.Cells["B1"].Value = "产品";
        worksheet.Cells["C1"].Value = "销售额";
        worksheet.Cells["D1"].Value = "数量";

        var regions = new[] { "华东", "华北", "华南" };
        var products = new[] { "手机", "电脑" };
        var row = 2;

        foreach (var region in regions)
        foreach (var product in products)
        {
            worksheet.Cells[row, 1].Value = region;
            worksheet.Cells[row, 2].Value = product;
            worksheet.Cells[row, 3].Value = 1000 * row;
            worksheet.Cells[row, 4].Value = row;
            row++;
        }

        package.SaveAs(new FileInfo(filePath));
        return filePath;
    }
}



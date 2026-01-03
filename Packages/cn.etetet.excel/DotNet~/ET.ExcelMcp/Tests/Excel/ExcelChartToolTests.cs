using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ET.Test;
using ET.Tools.Excel;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;

namespace ET.Test;

public class ExcelChartToolTests : ExcelTestBase
{
    private readonly ExcelChartTool _tool = new();

    private string CreateWorkbookWithData(string fileName, int rowCount = 10, int extraSeries = 0)
    {
        var filePath = CreateTestFilePath(fileName);
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Sheet1");

        for (var i = 0; i < rowCount; i++)
        {
            worksheet.Cells[i + 1, 1].Value = $"Category{i + 1}";
            worksheet.Cells[i + 1, 2].Value = (i + 1) * 10;
            for (var seriesIndex = 0; seriesIndex < extraSeries; seriesIndex++)
                worksheet.Cells[i + 1, 3 + seriesIndex].Value = (i + 1) * (seriesIndex + 2) * 5;
        }

        package.SaveAs(new FileInfo(filePath));
        return filePath;
    }

    [Fact]
    public async Task AddChart_ShouldAddChart()
    {
        var workbookPath = CreateWorkbookWithData("chart_add.xlsx");
        var outputPath = CreateTestFilePath("chart_add_output.xlsx");
        var arguments = CreateArguments("add", workbookPath, outputPath);
        arguments["chartType"] = "Column";
        arguments["dataRange"] = "B1:B10";
        arguments["categoryAxisDataRange"] = "A1:A10";

        await _tool.ExecuteAsync(arguments);

        using var package = new ExcelPackage(new FileInfo(outputPath));
        var charts = GetCharts(package);
        Assert.Single(charts);
        Assert.Equal(eChartType.ColumnClustered, charts[0].ChartType);
    }

    [Fact]
    public async Task GetCharts_ShouldReturnInfo()
    {
        var workbookPath = CreateWorkbookWithData("chart_get.xlsx");
        var addArgs = CreateArguments("add", workbookPath);
        addArgs["chartType"] = "Line";
        addArgs["dataRange"] = "B1:B5";
        addArgs["categoryAxisDataRange"] = "A1:A5";
        await _tool.ExecuteAsync(addArgs);

        var getArgs = CreateArguments("get", workbookPath);
        var result = await _tool.ExecuteAsync(getArgs);

        Assert.Contains("\"count\": 1", result);
        Assert.Contains("\"type\": \"Line\"", result);
    }

    [Fact]
    public async Task DeleteChart_ShouldRemoveChart()
    {
        var workbookPath = CreateWorkbookWithData("chart_delete.xlsx");
        var addArgs = CreateArguments("add", workbookPath);
        addArgs["chartType"] = "Column";
        addArgs["dataRange"] = "B1:B3";
        await _tool.ExecuteAsync(addArgs);

        var outputPath = CreateTestFilePath("chart_delete_output.xlsx");
        var deleteArgs = CreateArguments("delete", workbookPath, outputPath);
        deleteArgs["chartIndex"] = 0;

        await _tool.ExecuteAsync(deleteArgs);

        using var package = new ExcelPackage(new FileInfo(outputPath));
        var charts = GetCharts(package);
        Assert.Empty(charts);
    }

    [Fact]
    public async Task EditChart_ShouldChangeType()
    {
        var workbookPath = CreateWorkbookWithData("chart_edit.xlsx");
        var addArgs = CreateArguments("add", workbookPath);
        addArgs["chartType"] = "Column";
        addArgs["dataRange"] = "B1:B5";
        await _tool.ExecuteAsync(addArgs);

        var outputPath = CreateTestFilePath("chart_edit_output.xlsx");
        var editArgs = CreateArguments("edit", workbookPath, outputPath);
        editArgs["chartIndex"] = 0;
        editArgs["chartType"] = "Pie";

        await _tool.ExecuteAsync(editArgs);

        using var package = new ExcelPackage(new FileInfo(outputPath));
        var chart = GetCharts(package).Single();
        Assert.Equal(eChartType.Pie, chart.ChartType);
    }

    [Fact]
    public async Task UpdateChartData_ShouldChangeRange()
    {
        var workbookPath = CreateWorkbookWithData("chart_update.xlsx");
        var addArgs = CreateArguments("add", workbookPath);
        addArgs["chartType"] = "Column";
        addArgs["dataRange"] = "B1:B5";
        addArgs["categoryAxisDataRange"] = "A1:A5";
        await _tool.ExecuteAsync(addArgs);

        var outputPath = CreateTestFilePath("chart_update_output.xlsx");
        var updateArgs = CreateArguments("update_data", workbookPath, outputPath);
        updateArgs["chartIndex"] = 0;
        updateArgs["dataRange"] = "B1:B3";
        updateArgs["categoryAxisDataRange"] = "A1:A3";

        await _tool.ExecuteAsync(updateArgs);

        using var package = new ExcelPackage(new FileInfo(outputPath));
        var chart = GetCharts(package).Single();
        Assert.Contains("$B$1:$B$3", chart.Series[0].Series);
        Assert.Contains("$A$1:$A$3", chart.Series[0].XSeries);
    }

    [Fact]
    public async Task SetChartProperties_ShouldUpdateTitle()
    {
        var workbookPath = CreateWorkbookWithData("chart_props.xlsx");
        var addArgs = CreateArguments("add", workbookPath);
        addArgs["chartType"] = "Column";
        addArgs["dataRange"] = "B1:B4";
        await _tool.ExecuteAsync(addArgs);

        var outputPath = CreateTestFilePath("chart_props_output.xlsx");
        var propArgs = CreateArguments("set_properties", workbookPath, outputPath);
        propArgs["chartIndex"] = 0;
        propArgs["title"] = "Sales 2024";
        propArgs["legendVisible"] = false;

        await _tool.ExecuteAsync(propArgs);

        using var package = new ExcelPackage(new FileInfo(outputPath));
        var chart = GetCharts(package).Single();
        Assert.Equal("Sales 2024", chart.Title.Text);
    }

    [Fact]
    public async Task Add_WithMultipleSeries_ShouldCreateSeries()
    {
        var workbookPath = CreateWorkbookWithData("chart_multi.xlsx", 5, 1);
        var outputPath = CreateTestFilePath("chart_multi_output.xlsx");
        var arguments = CreateArguments("add", workbookPath, outputPath);
        arguments["chartType"] = "Column";
        arguments["dataRange"] = "B1:C5";
        arguments["categoryAxisDataRange"] = "A1:A5";

        await _tool.ExecuteAsync(arguments);

        using var package = new ExcelPackage(new FileInfo(outputPath));
        var chart = GetCharts(package).Single();
        Assert.True(chart.Series.Count >= 2, $"Expect at least 2 series, got {chart.Series.Count}");
    }

    [Fact]
    public async Task Add_WithInvalidChartType_ShouldFallbackToColumn()
    {
        var workbookPath = CreateWorkbookWithData("chart_invalid_type.xlsx");
        var outputPath = CreateTestFilePath("chart_invalid_type_output.xlsx");
        var arguments = CreateArguments("add", workbookPath, outputPath);
        arguments["chartType"] = "Invalid";
        arguments["dataRange"] = "B1:B4";

        await _tool.ExecuteAsync(arguments);

        using var package = new ExcelPackage(new FileInfo(outputPath));
        var chart = GetCharts(package).Single();
        Assert.Equal(eChartType.ColumnClustered, chart.ChartType);
    }

    [Fact]
    public async Task Edit_WithInvalidIndex_ShouldThrow()
    {
        var workbookPath = CreateWorkbookWithData("chart_invalid_index.xlsx");
        var outputPath = CreateTestFilePath("chart_invalid_index_output.xlsx");
        var arguments = CreateArguments("edit", workbookPath, outputPath);
        arguments["chartIndex"] = 99;

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _tool.ExecuteAsync(arguments));
        Assert.Contains("超出范围", ex.Message);
    }

    [Fact]
    public async Task Get_WithNoCharts_ShouldReturnEmptyPayload()
    {
        var workbookPath = CreateWorkbookWithData("chart_none.xlsx");
        var arguments = CreateArguments("get", workbookPath);

        var result = await _tool.ExecuteAsync(arguments);
        using var json = JsonDocument.Parse(result);
        Assert.Equal(0, json.RootElement.GetProperty("count").GetInt32());
        Assert.Equal("未找到图表", json.RootElement.GetProperty("message").GetString());
    }

    private static List<ExcelChart> GetCharts(ExcelPackage package)
    {
        return package.Workbook.Worksheets[0].Drawings
            .Where(d => d is ExcelChart)
            .Cast<ExcelChart>()
            .ToList();
    }
}

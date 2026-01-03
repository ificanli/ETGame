using System.Drawing;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using ET.Test;
using ET.Tools.Excel;
using OfficeOpenXml;
using OfficeOpenXml.ConditionalFormatting;

namespace ET.Test;

[TestClass]
public class ExcelConditionalFormattingToolTests : ExcelTestBase
{
    private readonly ExcelConditionalFormattingTool _tool = new();

    [TestMethod]
    public async Task AddColorScale_ShouldCreateRule()
    {
        var workbookPath = CreateWorkbookWithValues("cf_color_scale.xlsx");
        var outputPath = CreateTestFilePath("cf_color_scale_output.xlsx");
        var arguments = CreateArguments("add", workbookPath, outputPath);
        arguments["range"] = "A2:A11";
        arguments["ruleType"] = "color_scale";
        arguments["scaleType"] = "three_color";

        await _tool.ExecuteAsync(arguments);

        using var package = new ExcelPackage(new FileInfo(outputPath));
        var rules = package.Workbook.Worksheets[0].ConditionalFormatting;
        Assert.Single(rules);
        Assert.Equal(eExcelConditionalFormattingRuleType.ThreeColorScale, rules[0].Type);
    }

    [TestMethod]
    public async Task AddGreaterThan_WithStyle_ShouldApplyFormatting()
    {
        var workbookPath = CreateWorkbookWithValues("cf_style.xlsx");
        var outputPath = CreateTestFilePath("cf_style_output.xlsx");
        var arguments = CreateArguments("add", workbookPath, outputPath);
        arguments["range"] = "B2:B11";
        arguments["ruleType"] = "greater_than";
        arguments["value"] = "5";
        arguments["style"] = new JsonObject
        {
            ["fontColor"] = "#FF0000",
            ["backgroundColor"] = "#00FF00",
            ["bold"] = true
        };

        await _tool.ExecuteAsync(arguments);

        using var package = new ExcelPackage(new FileInfo(outputPath));
        var rule = package.Workbook.Worksheets[0].ConditionalFormatting.Single();
        Assert.Equal(Color.FromArgb(255, 0, 0), rule.Style.Font.Color.Color);
        Assert.Equal(Color.FromArgb(0, 255, 0), rule.Style.Fill.BackgroundColor.Color);
        Assert.True(rule.Style.Font.Bold);
    }

    [TestMethod]
    public async Task ClearRules_ShouldRemoveAllForRange()
    {
        var workbookPath = CreateWorkbookWithValues("cf_clear.xlsx");
        var withRulePath = CreateTestFilePath("cf_clear_with_rule.xlsx");
        var addArgs = CreateArguments("add", workbookPath, withRulePath);
        addArgs["range"] = "C2:C11";
        addArgs["ruleType"] = "data_bar";
        await _tool.ExecuteAsync(addArgs);

        var clearOutputPath = CreateTestFilePath("cf_clear_output.xlsx");
        var clearArgs = CreateArguments("clear", withRulePath, clearOutputPath);
        clearArgs["range"] = "C2:C11";
        await _tool.ExecuteAsync(clearArgs);

        using var package = new ExcelPackage(new FileInfo(clearOutputPath));
        Assert.Empty(package.Workbook.Worksheets[0].ConditionalFormatting);
    }

    [TestMethod]
    public async Task DeleteRuleByIndex_ShouldReduceCount()
    {
        var workbookPath = CreateWorkbookWithValues("cf_delete.xlsx");
        var withRulesPath = CreateTestFilePath("cf_delete_with_rules.xlsx");
        var addArgs = CreateArguments("add", workbookPath, withRulesPath);
        addArgs["range"] = "A2:A11";
        addArgs["ruleType"] = "color_scale";
        await _tool.ExecuteAsync(addArgs);

        var secondArgs = CreateArguments("add", withRulesPath, withRulesPath);
        secondArgs["range"] = "B2:B11";
        secondArgs["ruleType"] = "greater_than";
        secondArgs["value"] = "3";
        await _tool.ExecuteAsync(secondArgs);

        var deleteOutputPath = CreateTestFilePath("cf_delete_output.xlsx");
        var deleteArgs = CreateArguments("delete", withRulesPath, deleteOutputPath);
        deleteArgs["ruleIndex"] = 0;
        await _tool.ExecuteAsync(deleteArgs);

        using var package = new ExcelPackage(new FileInfo(deleteOutputPath));
        Assert.Single(package.Workbook.Worksheets[0].ConditionalFormatting);
    }

    [TestMethod]
    public async Task GetRules_ShouldReturnMetadata()
    {
        var workbookPath = CreateWorkbookWithValues("cf_get.xlsx");
        var withRulePath = CreateTestFilePath("cf_get_with_rule.xlsx");
        var addArgs = CreateArguments("add", workbookPath, withRulePath);
        addArgs["range"] = "A2:A11";
        addArgs["ruleType"] = "greater_than";
        addArgs["value"] = "5";
        await _tool.ExecuteAsync(addArgs);

        var getArgs = CreateArguments("get", withRulePath);
        var payload = await _tool.ExecuteAsync(getArgs);

        using var doc = JsonDocument.Parse(payload);
        Assert.Equal(1, doc.RootElement.GetProperty("count").GetInt32());
        Assert.Equal("成功获取条件格式信息", doc.RootElement.GetProperty("message").GetString());
    }

    private string CreateWorkbookWithValues(string fileName)
    {
        var filePath = CreateTestFilePath(fileName);
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Sheet1");

        worksheet.Cells["A1"].Value = "值1";
        worksheet.Cells["B1"].Value = "值2";
        worksheet.Cells["C1"].Value = "值3";

        for (var row = 2; row <= 11; row++)
        {
            worksheet.Cells[row, 1].Value = row;
            worksheet.Cells[row, 2].Value = row * 2;
            worksheet.Cells[row, 3].Value = row * 3;
        }

        package.SaveAs(new FileInfo(filePath));
        return filePath;
    }
}



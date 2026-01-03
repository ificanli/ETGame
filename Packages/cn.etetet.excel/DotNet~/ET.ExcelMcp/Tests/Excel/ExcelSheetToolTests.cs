using System.Text.Json;
using ET.Test;
using ET.Tools.Excel;
using OfficeOpenXml;

namespace ET.Test;

public class ExcelSheetToolTests : ExcelTestBase
{
    private readonly ExcelSheetTool _tool = new();

    [Fact]
    public async Task CreateSheet_ShouldAddWorksheet()
    {
        var workbookPath = CreateExcelWorkbook("sheet_create.xlsx");
        var outputPath = CreateTestFilePath("sheet_create_output.xlsx");
        var arguments = CreateArguments("create", workbookPath, outputPath);
        arguments["sheetName"] = "DataSheet";

        await _tool.ExecuteAsync(arguments);

        using var package = new ExcelPackage(new FileInfo(outputPath));
        Assert.Contains(package.Workbook.Worksheets, ws => ws.Name == "DataSheet");
    }

    [Fact]
    public async Task DeleteSheet_ShouldRemoveWorksheet()
    {
        var workbookPath = CreateExcelWorkbook("sheet_delete.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            package.Workbook.Worksheets.Add("Second");
            package.Save();
        }

        var outputPath = CreateTestFilePath("sheet_delete_output.xlsx");
        var arguments = CreateArguments("delete", workbookPath, outputPath);
        arguments["sheetIndex"] = 1;

        await _tool.ExecuteAsync(arguments);

        using var resultPackage = new ExcelPackage(new FileInfo(outputPath));
        Assert.Equal(1, resultPackage.Workbook.Worksheets.Count);
    }

    [Fact]
    public async Task RenameSheet_ShouldUpdateName()
    {
        var workbookPath = CreateExcelWorkbook("sheet_rename.xlsx");
        var outputPath = CreateTestFilePath("sheet_rename_output.xlsx");
        var arguments = CreateArguments("rename", workbookPath, outputPath);
        arguments["sheetIndex"] = 0;
        arguments["newName"] = "Summary";

        await _tool.ExecuteAsync(arguments);

        using var package = new ExcelPackage(new FileInfo(outputPath));
        Assert.Equal("Summary", package.Workbook.Worksheets[0].Name);
    }

    [Fact]
    public async Task ListSheets_ShouldReturnSheetInfo()
    {
        var workbookPath = CreateExcelWorkbookWithData("sheet_list.xlsx", 5, 2);
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            package.Workbook.Worksheets.Add("BlankSheet");
            package.Save();
        }

        var arguments = CreateArguments("list", workbookPath);
        var result = await _tool.ExecuteAsync(arguments);

        using var json = JsonDocument.Parse(result);
        Assert.True(json.RootElement.ValueKind == JsonValueKind.Array);
        Assert.True(json.RootElement.GetArrayLength() >= 2);

        var firstSheet = json.RootElement[0];
        Assert.Equal("Sheet1", firstSheet.GetProperty("name").GetString());
        Assert.True(firstSheet.GetProperty("rowCount").GetInt32() > 0);
        Assert.True(firstSheet.GetProperty("columnCount").GetInt32() > 0);
    }

    [Fact]
    public async Task DeleteSheet_WithInvalidIndex_ShouldThrow()
    {
        var workbookPath = CreateExcelWorkbook("sheet_delete_invalid.xlsx");
        var outputPath = CreateTestFilePath("sheet_delete_invalid_output.xlsx");
        var arguments = CreateArguments("delete", workbookPath, outputPath);
        arguments["sheetIndex"] = 5;

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _tool.ExecuteAsync(arguments));
        Assert.Contains("无效的工作表索引", ex.Message);
    }
}

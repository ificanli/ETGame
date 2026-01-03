using System.Text.Json.Nodes;
using ET.Test;
using ET.Tools.Excel;
using OfficeOpenXml;

namespace ET.Test;

[TestClass]
public class ExcelFileOperationsToolTests : ExcelTestBase
{
    private readonly ExcelFileOperationsTool _tool = new();

    [TestMethod]
    public async Task CreateWorkbook_ShouldCreateFileWithSheetName()
    {
        var outputPath = CreateTestFilePath("file_ops_create.xlsx");
        var args = new JsonObject
        {
            ["operation"] = "create",
            ["path"] = outputPath,
            ["sheetName"] = "DataSheet"
        };

        var result = await _tool.ExecuteAsync(args);

        Assert.Contains(outputPath, result);
        using var package = new ExcelPackage(new FileInfo(outputPath));
        Assert.Equal("DataSheet", package.Workbook.Worksheets[0].Name);
    }

    [TestMethod]
    public async Task CreateWorkbook_WithOutputPathOnly_ShouldUseOutput()
    {
        var outputPath = CreateTestFilePath("file_ops_create_no_path.xlsx");
        var args = new JsonObject
        {
            ["operation"] = "create",
            ["outputPath"] = outputPath
        };

        await _tool.ExecuteAsync(args);

        Assert.True(File.Exists(outputPath));
        using var package = new ExcelPackage(new FileInfo(outputPath));
        Assert.Equal("Sheet1", package.Workbook.Worksheets[0].Name);
    }

    [TestMethod]
    public async Task ConvertWorkbook_ToCsv_ShouldGenerateCsv()
    {
        var inputPath = CreateExcelWorkbookWithData("file_ops_convert.xlsx", 2, 2);
        var outputPath = CreateTestFilePath("file_ops_convert.csv");
        var args = new JsonObject
        {
            ["operation"] = "convert",
            ["inputPath"] = inputPath,
            ["outputPath"] = outputPath,
            ["format"] = "csv"
        };

        var result = await _tool.ExecuteAsync(args);

        Assert.Contains(outputPath, result);
        Assert.True(File.Exists(outputPath));
        var csvContent = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("\"R1C1\"", csvContent);
        Assert.Contains("\"R2C2\"", csvContent);
    }

    [TestMethod]
    public async Task ConvertWorkbook_WithUnsupportedFormat_ShouldThrow()
    {
        var inputPath = CreateExcelWorkbookWithData("file_ops_convert_invalid.xlsx");
        var outputPath = CreateTestFilePath("file_ops_convert_invalid.html");
        var args = new JsonObject
        {
            ["operation"] = "convert",
            ["inputPath"] = inputPath,
            ["outputPath"] = outputPath,
            ["format"] = "html"
        };

        var ex = await Assert.ThrowsAsync<NotImplementedException>(() => _tool.ExecuteAsync(args));
        Assert.Contains("暂不支持", ex.Message);
    }

    [TestMethod]
    public async Task Execute_WithUnknownOperation_ShouldThrow()
    {
        var args = new JsonObject
        {
            ["operation"] = "unknown"
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _tool.ExecuteAsync(args));
        Assert.Contains("未知操作", ex.Message);
    }
}



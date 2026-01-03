using ET.Test;
using ET.Tools.Excel;
using OfficeOpenXml;

namespace ET.Test;

public class ExcelCellToolTests : ExcelTestBase
{
    private readonly ExcelCellTool _tool = new();

    [Fact]
    public async Task GetCellValue_ShouldReturnValue()
    {
        var workbookPath = CreateExcelWorkbookWithData("cell_get.xlsx", 3);
        var arguments = CreateArguments("get", workbookPath);
        arguments["cell"] = "A1";

        var result = await _tool.ExecuteAsync(arguments);

        Assert.Contains("R1C1", result);
        Assert.Contains("\"cell\": \"A1\"", result);
    }

    [Fact]
    public async Task WriteCellValue_ShouldPersistValue()
    {
        var workbookPath = CreateExcelWorkbook("cell_write.xlsx");
        var outputPath = CreateTestFilePath("cell_write_output.xlsx");
        var arguments = CreateArguments("write", workbookPath, outputPath);
        arguments["cell"] = "B2";
        arguments["value"] = "Hello";

        await _tool.ExecuteAsync(arguments);

        using var package = new ExcelPackage(new FileInfo(outputPath));
        Assert.Equal("Hello", package.Workbook.Worksheets[0].Cells["B2"].Value);
    }

    [Fact]
    public async Task EditCellFormula_ShouldApplyFormula()
    {
        var workbookPath = CreateExcelWorkbook("cell_formula.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            package.Workbook.Worksheets[0].Cells["A1"].Value = 10;
            package.Workbook.Worksheets[0].Cells["B1"].Value = 20;
            package.Save();
        }

        var outputPath = CreateTestFilePath("cell_formula_output.xlsx");
        var arguments = CreateArguments("edit", workbookPath, outputPath);
        arguments["cell"] = "C1";
        arguments["formula"] = "A1+B1";

        await _tool.ExecuteAsync(arguments);

        using var resultPackage = new ExcelPackage(new FileInfo(outputPath));
        var worksheet = resultPackage.Workbook.Worksheets[0];
        Assert.Equal("A1+B1", worksheet.Cells["C1"].Formula);
    }

    [Fact]
    public async Task ClearCell_ShouldRemoveValue()
    {
        var workbookPath = CreateExcelWorkbook("cell_clear.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            package.Workbook.Worksheets[0].Cells["A1"].Value = "ToClear";
            package.Save();
        }

        var outputPath = CreateTestFilePath("cell_clear_output.xlsx");
        var arguments = CreateArguments("clear", workbookPath, outputPath);
        arguments["cell"] = "A1";

        await _tool.ExecuteAsync(arguments);

        using var resultPackage = new ExcelPackage(new FileInfo(outputPath));
        var clearedCell = resultPackage.Workbook.Worksheets[0].Cells["A1"];
        Assert.True(string.IsNullOrEmpty(clearedCell.Text));
    }

    [Fact]
    public async Task Write_ShouldHandleMultipleDataTypes()
    {
        var workbookPath = CreateExcelWorkbook("cell_types.xlsx");
        var outputPath = CreateTestFilePath("cell_types_output.xlsx");

        var numberArgs = CreateArguments("write", workbookPath, outputPath);
        numberArgs["cell"] = "A1";
        numberArgs["value"] = "123.45";
        await _tool.ExecuteAsync(numberArgs);

        var boolArgs = CreateArguments("write", outputPath, outputPath);
        boolArgs["cell"] = "A2";
        boolArgs["value"] = "true";
        await _tool.ExecuteAsync(boolArgs);

        var dateArgs = CreateArguments("write", outputPath, outputPath);
        dateArgs["cell"] = "A3";
        dateArgs["value"] = "2024-01-15";
        await _tool.ExecuteAsync(dateArgs);

        using var package = new ExcelPackage(new FileInfo(outputPath));
        var worksheet = package.Workbook.Worksheets[0];

        Assert.Equal(123.45, worksheet.Cells["A1"].GetValue<double>(), 2);
        Assert.True(worksheet.Cells["A2"].GetValue<bool>());
        Assert.Equal(new DateTime(2024, 1, 15), worksheet.Cells["A3"].GetValue<DateTime>().Date);
    }

    [Fact]
    public async Task GetCell_FromDifferentSheet_ShouldReturnValue()
    {
        var workbookPath = CreateExcelWorkbook("cell_get_sheet.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            var sheet = package.Workbook.Worksheets.Add("SecondSheet");
            sheet.Cells["A1"].Value = "Sheet2Data";
            package.Save();
        }

        var arguments = CreateArguments("get", workbookPath);
        arguments["cell"] = "A1";
        arguments["sheetIndex"] = 1;

        var result = await _tool.ExecuteAsync(arguments);

        Assert.Contains("Sheet2Data", result);
    }

    [Fact]
    public async Task WriteCell_WithInvalidAddress_ShouldThrow()
    {
        var workbookPath = CreateExcelWorkbook("cell_invalid.xlsx");
        var outputPath = CreateTestFilePath("cell_invalid_output.xlsx");
        var arguments = CreateArguments("write", workbookPath, outputPath);
        arguments["cell"] = "Invalid";
        arguments["value"] = "Test";

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _tool.ExecuteAsync(arguments));
        Assert.Contains("单元格地址格式无效", ex.Message);
    }

    [Fact]
    public async Task EditCell_WithoutValueOrFormula_ShouldThrow()
    {
        var workbookPath = CreateExcelWorkbook("cell_edit_missing.xlsx");
        var outputPath = CreateTestFilePath("cell_edit_missing_output.xlsx");
        var arguments = CreateArguments("edit", workbookPath, outputPath);
        arguments["cell"] = "A1";

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _tool.ExecuteAsync(arguments));
        Assert.Contains("必须提供 value 或 formula", ex.Message);
    }
}

using System.Text.Json;
using System.Text.Json.Nodes;
using OfficeOpenXml;
using ET.Test;
using ET.Tools.Excel;

namespace ET.Test;

[TestClass]
public class ExcelFormulaToolTests : ExcelTestBase
{
    private readonly ExcelFormulaTool _tool = new();

    #region Calculate Tests

    [TestMethod]
    public async Task CalculateFormulas_ShouldCalculateAllFormulas()
    {
        var workbookPath = CreateExcelWorkbook("test_calculate_formulas.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            package.Workbook.Worksheets[0].Cells["A1"].Value = 10;
            package.Workbook.Worksheets[0].Cells["A2"].Value = 20;
            package.Workbook.Worksheets[0].Cells["A3"].Formula = "A1+A2";
            package.Save();
        }

        var outputPath = CreateTestFilePath("test_calculate_formulas_output.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "calculate",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath
        };

        var result = await _tool.ExecuteAsync(arguments);

        Assert.Contains("公式已计算", result);
        Assert.True(File.Exists(outputPath));
    }

    #endregion

    #region Add Tests

    [TestMethod]
    public async Task AddFormula_WithSum_ShouldAddSumFormula()
    {
        var workbookPath = CreateExcelWorkbook("test_formula_sum.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            package.Workbook.Worksheets[0].Cells["A1"].Value = 10;
            package.Workbook.Worksheets[0].Cells["A2"].Value = 20;
            package.Workbook.Worksheets[0].Cells["A3"].Value = 30;
            package.Save();
        }

        var outputPath = CreateTestFilePath("test_formula_sum_output.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "add",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["cell"] = "A4",
            ["formula"] = "=SUM(A1:A3)"
        };

        await _tool.ExecuteAsync(arguments);

        using var resultPackage = new ExcelPackage(new FileInfo(outputPath));
        var worksheet = resultPackage.Workbook.Worksheets[0];
        Assert.Equal("SUM(A1:A3)", worksheet.Cells["A4"].Formula);
    }

    [TestMethod]
    public async Task AddFormula_WithAutoCalculateFalse_ShouldNotCalculate()
    {
        var workbookPath = CreateExcelWorkbook("test_formula_no_calc.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            package.Workbook.Worksheets[0].Cells["A1"].Value = 10;
            package.Workbook.Worksheets[0].Cells["A2"].Value = 20;
            package.Save();
        }

        var outputPath = CreateTestFilePath("test_formula_no_calc_output.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "add",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["cell"] = "A3",
            ["formula"] = "=A1+A2",
            ["autoCalculate"] = false
        };

        var result = await _tool.ExecuteAsync(arguments);

        Assert.Contains("公式已添加", result);
        Assert.True(File.Exists(outputPath));
    }

    [TestMethod]
    public async Task AddFormula_WithAverage_ShouldAddAverageFormula()
    {
        var workbookPath = CreateExcelWorkbook("test_formula_average.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            package.Workbook.Worksheets[0].Cells["A1"].Value = 10;
            package.Workbook.Worksheets[0].Cells["A2"].Value = 20;
            package.Workbook.Worksheets[0].Cells["A3"].Value = 30;
            package.Save();
        }

        var outputPath = CreateTestFilePath("test_formula_average_output.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "add",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["cell"] = "A4",
            ["formula"] = "=AVERAGE(A1:A3)"
        };

        await _tool.ExecuteAsync(arguments);

        using var resultPackage = new ExcelPackage(new FileInfo(outputPath));
        Assert.Equal("AVERAGE(A1:A3)", resultPackage.Workbook.Worksheets[0].Cells["A4"].Formula);
    }

    [TestMethod]
    public async Task AddFormula_WithSheetIndex_ShouldApplyToCorrectSheet()
    {
        var workbookPath = CreateExcelWorkbook("test_formula_sheet.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            package.Workbook.Worksheets.Add("Sheet2");
            package.Workbook.Worksheets[1].Cells["A1"].Value = 100;
            package.Save();
        }

        var outputPath = CreateTestFilePath("test_formula_sheet_output.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "add",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["sheetIndex"] = 1,
            ["cell"] = "B1",
            ["formula"] = "=A1*2"
        };

        await _tool.ExecuteAsync(arguments);

        using var resultPackage = new ExcelPackage(new FileInfo(outputPath));
        Assert.Equal("A1*2", resultPackage.Workbook.Worksheets[1].Cells["B1"].Formula);
    }

    #endregion

    #region Get Tests

    [TestMethod]
    public async Task GetFormula_ShouldReturnFormula()
    {
        var workbookPath = CreateExcelWorkbook("test_get_formula.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            package.Workbook.Worksheets[0].Cells["A1"].Formula = "SUM(B1:B10)";
            package.Save();
        }

        var arguments = new JsonObject
        {
            ["operation"] = "get",
            ["path"] = workbookPath
        };

        var result = await _tool.ExecuteAsync(arguments);

        Assert.NotNull(result);
        Assert.Contains("A1", result);
        Assert.Contains("SUM", result, StringComparison.OrdinalIgnoreCase);
    }

    [TestMethod]
    public async Task GetFormula_WithRange_ShouldReturnFormulasInRange()
    {
        var workbookPath = CreateExcelWorkbook("test_get_formula_range.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            package.Workbook.Worksheets[0].Cells["A1"].Formula = "B1+1";
            package.Workbook.Worksheets[0].Cells["A2"].Formula = "B2+2";
            package.Workbook.Worksheets[0].Cells["C1"].Formula = "D1+3";
            package.Save();
        }

        var arguments = new JsonObject
        {
            ["operation"] = "get",
            ["path"] = workbookPath,
            ["range"] = "A1:B2"
        };

        var result = await _tool.ExecuteAsync(arguments);

        var json = JsonDocument.Parse(result);
        Assert.Equal(2, json.RootElement.GetProperty("count").GetInt32());
    }

    [TestMethod]
    public async Task GetFormula_NoFormulas_ShouldReturnEmptyResult()
    {
        var workbookPath = CreateExcelWorkbook("test_get_formula_empty.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            package.Workbook.Worksheets[0].Cells["A1"].Value = "Just text";
            package.Save();
        }

        var arguments = new JsonObject
        {
            ["operation"] = "get",
            ["path"] = workbookPath
        };

        var result = await _tool.ExecuteAsync(arguments);

        var json = JsonDocument.Parse(result);
        Assert.Equal(0, json.RootElement.GetProperty("count").GetInt32());
        Assert.Contains("未找到公式", json.RootElement.GetProperty("message").GetString());
    }

    #endregion

    #region GetResult Tests

    [TestMethod]
    public async Task GetFormulaResult_ShouldReturnResult()
    {
        var workbookPath = CreateExcelWorkbook("test_get_formula_result.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            package.Workbook.Worksheets[0].Cells["A1"].Value = 10;
            package.Workbook.Worksheets[0].Cells["A2"].Value = 20;
            package.Workbook.Worksheets[0].Cells["A3"].Formula = "A1+A2";
            package.Workbook.Calculate();
            package.Save();
        }

        var arguments = new JsonObject
        {
            ["operation"] = "get_result",
            ["path"] = workbookPath,
            ["cell"] = "A3"
        };

        var result = await _tool.ExecuteAsync(arguments);

        Assert.NotNull(result);
        var json = JsonDocument.Parse(result);
        Assert.Equal("A3", json.RootElement.GetProperty("cell").GetString());
        Assert.Contains("30", json.RootElement.GetProperty("calculatedValue").GetString());
    }

    #endregion

    #region SetArray Tests

    [TestMethod]
    public async Task SetArrayFormula_ShouldSetArrayFormula()
    {
        var workbookPath = CreateExcelWorkbook("test_array_formula.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            package.Workbook.Worksheets[0].Cells["A1"].Value = 1;
            package.Workbook.Worksheets[0].Cells["A2"].Value = 2;
            package.Workbook.Worksheets[0].Cells["A3"].Value = 3;
            package.Save();
        }

        var outputPath = CreateTestFilePath("test_array_formula_output.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "set_array",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["range"] = "B1:B3",
            ["formula"] = "=A1:A3*2"
        };

        var result = await _tool.ExecuteAsync(arguments);

        Assert.Contains("数组公式已设置", result);
        Assert.True(File.Exists(outputPath));
    }

    #endregion

    #region GetArray Tests

    [TestMethod]
    public async Task GetArrayFormula_ShouldReturnArrayFormula()
    {
        var workbookPath = CreateExcelWorkbook("test_get_array_formula.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            package.Workbook.Worksheets[0].Cells["A1"].Value = 1;
            package.Workbook.Worksheets[0].Cells["A2"].Value = 2;
            package.Workbook.Worksheets[0].Cells["B1:B2"].CreateArrayFormula("A1:A2*2");
            package.Save();
        }

        var arguments = new JsonObject
        {
            ["operation"] = "get_array",
            ["path"] = workbookPath,
            ["cell"] = "B1"
        };

        var result = await _tool.ExecuteAsync(arguments);

        var json = JsonDocument.Parse(result);
        Assert.True(json.RootElement.GetProperty("isArrayFormula").GetBoolean());
    }

    [TestMethod]
    public async Task GetArrayFormula_NotArrayFormula_ShouldReturnFalse()
    {
        var workbookPath = CreateExcelWorkbook("test_get_array_not.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            package.Workbook.Worksheets[0].Cells["A1"].Formula = "SUM(B1:B5)";
            package.Save();
        }

        var arguments = new JsonObject
        {
            ["operation"] = "get_array",
            ["path"] = workbookPath,
            ["cell"] = "A1"
        };

        var result = await _tool.ExecuteAsync(arguments);

        var json = JsonDocument.Parse(result);
        Assert.False(json.RootElement.GetProperty("isArrayFormula").GetBoolean());
    }

    #endregion

    #region Error Handling Tests

    [TestMethod]
    public async Task UnknownOperation_ShouldThrowException()
    {
        var workbookPath = CreateExcelWorkbook("test_unknown_op.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "unknown",
            ["path"] = workbookPath
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _tool.ExecuteAsync(arguments));
        Assert.Contains("未知操作", ex.Message);
    }

    [TestMethod]
    public async Task InvalidSheetIndex_ShouldThrowException()
    {
        var workbookPath = CreateExcelWorkbook("test_invalid_sheet.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "add",
            ["path"] = workbookPath,
            ["sheetIndex"] = 99,
            ["cell"] = "A1",
            ["formula"] = "=1+1"
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _tool.ExecuteAsync(arguments));
        Assert.Contains("Invalid sheet index", ex.Message);
    }

    #endregion
}



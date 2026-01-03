using System.Text.Json;
using System.Text.Json.Nodes;
using OfficeOpenXml;
using OfficeOpenXml.DataValidation;
using ET.Test;
using ET.Tools.Excel;

namespace ET.Test;

public class ExcelDataValidationToolTests : ExcelTestBase
{
    private readonly ExcelDataValidationTool _tool = new();

    #region Add Tests

    [Fact]
    public async Task AddDataValidation_WithList_ShouldAddListValidation()
    {
        var workbookPath = CreateExcelWorkbookWithData("test_add_validation.xlsx");
        var outputPath = CreateTestFilePath("test_add_validation_output.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "add",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["range"] = "A1:A10",
            ["validationType"] = "List",
            ["formula1"] = "Option1,Option2,Option3"
        };

        var result = await _tool.ExecuteAsync(arguments);

        Assert.Contains("已添加数据验证", result);
        Assert.Contains("索引: 0", result);

        using var package = new ExcelPackage(new FileInfo(outputPath));
        var worksheet = package.Workbook.Worksheets[0];
        Assert.True(worksheet.DataValidations.Count > 0);
    }

    [Fact]
    public async Task AddDataValidation_WithWholeNumber_ShouldAddNumberValidation()
    {
        var workbookPath = CreateExcelWorkbook("test_add_number_validation.xlsx");
        var outputPath = CreateTestFilePath("test_add_number_validation_output.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "add",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["range"] = "A1:A10",
            ["validationType"] = "WholeNumber",
            ["formula1"] = "0",
            ["formula2"] = "100"
        };

        await _tool.ExecuteAsync(arguments);

        using var package = new ExcelPackage(new FileInfo(outputPath));
        var worksheet = package.Workbook.Worksheets[0];
        var validation = worksheet.DataValidations[0];
        Assert.Equal(eDataValidationType.Whole, validation.ValidationType.Type);
        Assert.Equal(ExcelDataValidationOperator.between, validation.Operator);
    }

    [Fact]
    public async Task AddDataValidation_WithOperatorGreaterThan_ShouldUseGreaterThanOperator()
    {
        var workbookPath = CreateExcelWorkbook("test_add_greater_than.xlsx");
        var outputPath = CreateTestFilePath("test_add_greater_than_output.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "add",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["range"] = "A1:A10",
            ["validationType"] = "WholeNumber",
            ["operatorType"] = "GreaterThan",
            ["formula1"] = "0"
        };

        await _tool.ExecuteAsync(arguments);

        using var package = new ExcelPackage(new FileInfo(outputPath));
        var worksheet = package.Workbook.Worksheets[0];
        var validation = worksheet.DataValidations[0];
        Assert.Equal(eDataValidationType.Whole, validation.ValidationType.Type);
        Assert.Equal(ExcelDataValidationOperator.greaterThan, validation.Operator);
    }

    [Fact]
    public async Task AddDataValidation_WithMessages_ShouldSetMessages()
    {
        var workbookPath = CreateExcelWorkbook("test_add_with_messages.xlsx");
        var outputPath = CreateTestFilePath("test_add_with_messages_output.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "add",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["range"] = "A1:A10",
            ["validationType"] = "List",
            ["formula1"] = "Yes,No",
            ["inputMessage"] = "Please select Yes or No",
            ["errorMessage"] = "Invalid selection"
        };

        await _tool.ExecuteAsync(arguments);

        using var package = new ExcelPackage(new FileInfo(outputPath));
        var worksheet = package.Workbook.Worksheets[0];
        var validation = worksheet.DataValidations[0];
        Assert.Equal("Please select Yes or No", validation.Prompt);
        Assert.Equal("Invalid selection", validation.Error);
        Assert.True(validation.ShowInputMessage);
        Assert.True(validation.ShowErrorMessage);
    }

    [Fact]
    public async Task AddDataValidation_AllValidationTypes_ShouldWork()
    {
        var validationTypes = new[] { "WholeNumber", "Decimal", "TextLength", "Custom" };

        foreach (var validationType in validationTypes)
        {
            var workbookPath = CreateExcelWorkbook($"test_type_{validationType}.xlsx");
            var outputPath = CreateTestFilePath($"test_type_{validationType}_output.xlsx");
            var arguments = new JsonObject
            {
                ["operation"] = "add",
                ["path"] = workbookPath,
                ["outputPath"] = outputPath,
                ["range"] = "A1:A10",
                ["validationType"] = validationType,
                ["formula1"] = "1"
            };

            var result = await _tool.ExecuteAsync(arguments);
            Assert.Contains($"类型: {validationType}", result);
        }
    }

    #endregion

    #region Get Tests

    [Fact]
    public async Task GetDataValidation_ShouldReturnValidationInfo()
    {
        var workbookPath = CreateExcelWorkbook("test_get_validation.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            var worksheet = package.Workbook.Worksheets[0];
            var validation = worksheet.DataValidations.AddListValidation("A1:A10");
            validation.Formula.Values.Add("1");
            validation.Formula.Values.Add("2");
            validation.Formula.Values.Add("3");
            package.Save();
        }

        var arguments = new JsonObject
        {
            ["operation"] = "get",
            ["path"] = workbookPath
        };

        var result = await _tool.ExecuteAsync(arguments);

        Assert.NotNull(result);
        var json = JsonDocument.Parse(result);
        Assert.Equal(1, json.RootElement.GetProperty("count").GetInt32());
        Assert.True(json.RootElement.GetProperty("items").GetArrayLength() > 0);
    }

    [Fact]
    public async Task GetDataValidation_EmptyWorksheet_ShouldReturnEmptyList()
    {
        var workbookPath = CreateExcelWorkbook("test_get_empty.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "get",
            ["path"] = workbookPath
        };

        var result = await _tool.ExecuteAsync(arguments);

        var json = JsonDocument.Parse(result);
        Assert.Equal(0, json.RootElement.GetProperty("count").GetInt32());
        Assert.Equal(0, json.RootElement.GetProperty("items").GetArrayLength());
    }

    #endregion

    #region SetMessages Tests

    [Fact]
    public async Task SetMessages_ShouldSetInputAndErrorMessage()
    {
        var workbookPath = CreateExcelWorkbook("test_set_messages.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            var worksheet = package.Workbook.Worksheets[0];
            var validation = worksheet.DataValidations.AddListValidation("A1:A10");
            validation.Formula.Values.Add("Test");
            package.Save();
        }

        var outputPath = CreateTestFilePath("test_set_messages_output.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "set_messages",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["validationIndex"] = 0,
            ["inputMessage"] = "Please select a value",
            ["errorMessage"] = "Invalid value selected"
        };

        var result = await _tool.ExecuteAsync(arguments);

        Assert.Contains("已更新数据验证", result);
        Assert.Contains("InputMessage=Please select a value", result);
        Assert.Contains("ErrorMessage=Invalid value selected", result);

        using var resultPackage = new ExcelPackage(new FileInfo(outputPath));
        var resultWorksheet = resultPackage.Workbook.Worksheets[0];
        var resultValidation = resultWorksheet.DataValidations[0];
        Assert.Equal("Please select a value", resultValidation.Prompt);
        Assert.Equal("Invalid value selected", resultValidation.Error);
    }

    [Fact]
    public async Task SetMessages_ClearMessage_ShouldClearAndDisableShow()
    {
        var workbookPath = CreateExcelWorkbook("test_clear_messages.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            var worksheet = package.Workbook.Worksheets[0];
            var validation = worksheet.DataValidations.AddListValidation("A1:A10");
            validation.Formula.Values.Add("Test");
            validation.Prompt = "Old message";
            validation.ShowInputMessage = true;
            package.Save();
        }

        var outputPath = CreateTestFilePath("test_clear_messages_output.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "set_messages",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["validationIndex"] = 0,
            ["inputMessage"] = ""
        };

        await _tool.ExecuteAsync(arguments);

        using var resultPackage = new ExcelPackage(new FileInfo(outputPath));
        var resultValidation = resultPackage.Workbook.Worksheets[0].DataValidations[0];
        Assert.True(string.IsNullOrEmpty(resultValidation.Prompt));
        Assert.False(resultValidation.ShowInputMessage);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task DeleteDataValidation_ShouldDeleteValidation()
    {
        var workbookPath = CreateExcelWorkbook("test_delete_validation.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            var worksheet = package.Workbook.Worksheets[0];
            var validation = worksheet.DataValidations.AddListValidation("A1:A10");
            validation.Formula.Values.Add("Test");
            package.Save();
        }

        var outputPath = CreateTestFilePath("test_delete_validation_output.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "delete",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["validationIndex"] = 0
        };

        var result = await _tool.ExecuteAsync(arguments);

        Assert.Contains("已删除数据验证 #0", result);
        Assert.Contains("剩余: 0", result);

        using var resultPackage = new ExcelPackage(new FileInfo(outputPath));
        Assert.Equal(0, resultPackage.Workbook.Worksheets[0].DataValidations.Count);
    }

    [Fact]
    public async Task DeleteDataValidation_InvalidIndex_ShouldThrowException()
    {
        var workbookPath = CreateExcelWorkbook("test_delete_invalid.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "delete",
            ["path"] = workbookPath,
            ["validationIndex"] = 99
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _tool.ExecuteAsync(arguments));
        Assert.Contains("超出范围", ex.Message);
    }

    #endregion

    #region Edit Tests

    [Fact]
    public async Task EditDataValidation_ShouldEditValidation()
    {
        var workbookPath = CreateExcelWorkbook("test_edit_validation.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            var worksheet = package.Workbook.Worksheets[0];
            var validation = worksheet.DataValidations.AddListValidation("A1:A10");
            validation.Formula.Values.Add("1");
            validation.Formula.Values.Add("2");
            validation.Formula.Values.Add("3");
            package.Save();
        }

        var outputPath = CreateTestFilePath("test_edit_validation_output.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "edit",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["validationIndex"] = 0,
            ["inputMessage"] = "New message"
        };

        var result = await _tool.ExecuteAsync(arguments);

        Assert.Contains("已编辑数据验证 #0", result);
        Assert.Contains("InputMessage=New message", result);
    }

    [Fact]
    public async Task EditDataValidation_InvalidIndex_ShouldThrowException()
    {
        var workbookPath = CreateExcelWorkbook("test_edit_invalid.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "edit",
            ["path"] = workbookPath,
            ["validationIndex"] = 99,
            ["inputMessage"] = "test"
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _tool.ExecuteAsync(arguments));
        Assert.Contains("超出范围", ex.Message);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task AddDataValidation_InvalidValidationType_ShouldThrowException()
    {
        var workbookPath = CreateExcelWorkbook("test_add_invalid_type.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "add",
            ["path"] = workbookPath,
            ["range"] = "A1:A10",
            ["validationType"] = "InvalidType",
            ["formula1"] = "test"
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _tool.ExecuteAsync(arguments));
        Assert.Contains("不支持的验证类型", ex.Message);
    }

    [Fact]
    public async Task AddDataValidation_InvalidOperatorType_ShouldThrowException()
    {
        var workbookPath = CreateExcelWorkbook("test_add_invalid_operator.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "add",
            ["path"] = workbookPath,
            ["range"] = "A1:A10",
            ["validationType"] = "WholeNumber",
            ["operatorType"] = "InvalidOperator",
            ["formula1"] = "0"
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _tool.ExecuteAsync(arguments));
        Assert.Contains("不支持的运算符类型", ex.Message);
    }

    [Fact]
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

    #endregion
}

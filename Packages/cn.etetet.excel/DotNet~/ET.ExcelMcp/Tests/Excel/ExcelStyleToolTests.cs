using System.Text.Json.Nodes;
using ET.Test;
using ET.Tools.Excel;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace ET.Test;

[TestClass]
public class ExcelStyleToolTests : ExcelTestBase
{
    private readonly ExcelStyleTool _tool = new();

    [TestMethod]
    public async Task FormatCells_WithFontOptions_ShouldApplyFontFormatting()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbook("test_format_font.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            package.Workbook.Worksheets[0].Cells["A1"].Value = "Test";
            package.Save();
        }

        var outputPath = CreateTestFilePath("test_format_font_output.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "format",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["range"] = "A1",
            ["fontName"] = "Arial",
            ["fontSize"] = 14,
            ["bold"] = true,
            ["italic"] = true
        };

        // Act
        await _tool.ExecuteAsync(arguments);

        // Assert
        using var resultPackage = new ExcelPackage(new FileInfo(outputPath));
        var worksheet = resultPackage.Workbook.Worksheets[0];
        var style = worksheet.Cells["A1"].Style;
        Assert.Equal("Arial", style.Font.Name);
        Assert.Equal(14, style.Font.Size);
        Assert.True(style.Font.Bold);
        Assert.True(style.Font.Italic);
    }

    [TestMethod]
    public async Task FormatCells_WithColors_ShouldApplyColors()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbook("test_format_colors.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            package.Workbook.Worksheets[0].Cells["A1"].Value = "Test";
            package.Save();
        }

        var outputPath = CreateTestFilePath("test_format_colors_output.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "format",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["range"] = "A1",
            ["fontColor"] = "#FF0000",
            ["backgroundColor"] = "#FFFF00"
        };

        // Act
        await _tool.ExecuteAsync(arguments);

        // Assert
        using var resultPackage = new ExcelPackage(new FileInfo(outputPath));
        var worksheet = resultPackage.Workbook.Worksheets[0];
        var style = worksheet.Cells["A1"].Style;
        // Verify colors were applied
        var fontColorRgb = style.Font.Color.Rgb;
        var bgColorRgb = style.Fill.BackgroundColor.Rgb;
        Assert.True(fontColorRgb == "FFFF0000" || bgColorRgb == "FFFFFF00",
            $"Colors should be applied. Font: {fontColorRgb}, Background: {bgColorRgb}");
    }

    [TestMethod]
    public async Task FormatCells_WithAlignment_ShouldApplyAlignment()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbook("test_format_alignment.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            package.Workbook.Worksheets[0].Cells["A1"].Value = "Test";
            package.Save();
        }

        var outputPath = CreateTestFilePath("test_format_alignment_output.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "format",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["range"] = "A1",
            ["horizontalAlignment"] = "Center",
            ["verticalAlignment"] = "Center"
        };

        // Act
        await _tool.ExecuteAsync(arguments);

        // Assert
        using var resultPackage = new ExcelPackage(new FileInfo(outputPath));
        var worksheet = resultPackage.Workbook.Worksheets[0];
        var style = worksheet.Cells["A1"].Style;
        Assert.Equal(ExcelHorizontalAlignment.Center, style.HorizontalAlignment);
        Assert.Equal(ExcelVerticalAlignment.Center, style.VerticalAlignment);
    }

    [TestMethod]
    public async Task FormatCells_WithBorder_ShouldApplyBorder()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbook("test_format_border.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            package.Workbook.Worksheets[0].Cells["A1"].Value = "Test";
            package.Save();
        }

        var outputPath = CreateTestFilePath("test_format_border_output.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "format",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["range"] = "A1",
            ["borderStyle"] = "Thin",
            ["borderColor"] = "#000000"
        };

        // Act
        await _tool.ExecuteAsync(arguments);

        // Assert
        using var resultPackage = new ExcelPackage(new FileInfo(outputPath));
        var worksheet = resultPackage.Workbook.Worksheets[0];
        var style = worksheet.Cells["A1"].Style;
        // Verify border was applied
        var hasBorder = style.Border.Top.Style != ExcelBorderStyle.None ||
                        style.Border.Bottom.Style != ExcelBorderStyle.None ||
                        style.Border.Left.Style != ExcelBorderStyle.None ||
                        style.Border.Right.Style != ExcelBorderStyle.None;
        Assert.True(hasBorder, "Border should be applied");
    }

    [TestMethod]
    public async Task FormatCells_WithNumberFormat_ShouldApplyNumberFormat()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbook("test_format_number.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            package.Workbook.Worksheets[0].Cells["A1"].Value = 1234.56;
            package.Save();
        }

        var outputPath = CreateTestFilePath("test_format_number_output.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "format",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["range"] = "A1",
            ["numberFormat"] = "#,##0.00"
        };

        // Act
        await _tool.ExecuteAsync(arguments);

        // Assert
        using var resultPackage = new ExcelPackage(new FileInfo(outputPath));
        var worksheet = resultPackage.Workbook.Worksheets[0];
        var style = worksheet.Cells["A1"].Style;
        // Number format applied
        Assert.Contains("#,##0.00", style.Numberformat.Format);
    }

    [TestMethod]
    public async Task FormatCells_WithAllFormattingOptions_ShouldApplyAllFormats()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbook("test_format_all.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            package.Workbook.Worksheets[0].Cells["A1"].Value = "Test";
            package.Save();
        }

        var outputPath = CreateTestFilePath("test_format_all_output.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "format",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["range"] = "A1",
            ["fontName"] = "Arial",
            ["fontSize"] = 14,
            ["bold"] = true,
            ["italic"] = true,
            ["fontColor"] = "#FF0000",
            ["backgroundColor"] = "#FFFF00",
            ["horizontalAlignment"] = "Center",
            ["verticalAlignment"] = "Center",
            ["borderStyle"] = "Thin"
        };

        // Act
        await _tool.ExecuteAsync(arguments);

        // Assert
        using var resultPackage = new ExcelPackage(new FileInfo(outputPath));
        var worksheet = resultPackage.Workbook.Worksheets[0];
        var style = worksheet.Cells["A1"].Style;
        Assert.Equal("Arial", style.Font.Name);
        Assert.Equal(14, style.Font.Size);
        Assert.True(style.Font.Bold);
        Assert.True(style.Font.Italic);
        Assert.Equal(ExcelHorizontalAlignment.Center, style.HorizontalAlignment);
        Assert.Equal(ExcelVerticalAlignment.Center, style.VerticalAlignment);
    }

    [TestMethod]
    public async Task GetFormat_ShouldReturnFormatInfo()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbook("test_get_format.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            var cell = package.Workbook.Worksheets[0].Cells["A1"];
            cell.Value = "Test";
            cell.Style.Font.Name = "Arial";
            cell.Style.Font.Size = 14;
            cell.Style.Font.Bold = true;
            package.Save();
        }

        var arguments = new JsonObject
        {
            ["operation"] = "get_format",
            ["path"] = workbookPath,
            ["range"] = "A1"
        };

        // Act
        var result = await _tool.ExecuteAsync(arguments);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("A1", result);
    }

    [TestMethod]
    public async Task CopySheetFormat_ShouldCopyFormat()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbook("test_copy_sheet_format.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            var sourceSheet = package.Workbook.Worksheets[0];
            sourceSheet.Cells["A1"].Value = "Test";
            sourceSheet.Cells["A1"].Style.Font.Name = "Arial";
            sourceSheet.Cells["A1"].Style.Font.Size = 14;
            package.Workbook.Worksheets.Add("TargetSheet");
            package.Save();
        }

        var outputPath = CreateTestFilePath("test_copy_sheet_format_output.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "copy_sheet_format",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["sourceSheetIndex"] = 0,
            ["targetSheetIndex"] = 1,
            ["copyColumnWidths"] = true,
            ["copyRowHeights"] = true
        };

        // Act
        await _tool.ExecuteAsync(arguments);

        // Assert
        Assert.True(File.Exists(outputPath), "Output workbook should be created");
    }

    [TestMethod]
    public async Task FormatCells_WithBatchRanges_ShouldApplyToAllRanges()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbook("test_format_batch.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            package.Workbook.Worksheets[0].Cells["A1"].Value = "Test1";
            package.Workbook.Worksheets[0].Cells["B2"].Value = "Test2";
            package.Save();
        }

        var outputPath = CreateTestFilePath("test_format_batch_output.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "format",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["ranges"] = new JsonArray { "A1", "B2" },
            ["bold"] = true
        };

        // Act
        await _tool.ExecuteAsync(arguments);

        // Assert
        using var resultPackage = new ExcelPackage(new FileInfo(outputPath));
        Assert.True(resultPackage.Workbook.Worksheets[0].Cells["A1"].Style.Font.Bold);
        Assert.True(resultPackage.Workbook.Worksheets[0].Cells["B2"].Style.Font.Bold);
    }

    [TestMethod]
    public async Task FormatCells_WithoutRangeOrRanges_ShouldThrowArgumentException()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbook("test_format_no_range.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "format",
            ["path"] = workbookPath,
            ["bold"] = true
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _tool.ExecuteAsync(arguments));
        Assert.Contains("range", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [TestMethod]
    public async Task FormatCells_WithInvalidColor_ShouldThrowArgumentException()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbook("test_format_invalid_color.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "format",
            ["path"] = workbookPath,
            ["range"] = "A1",
            ["backgroundColor"] = "invalid_color"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _tool.ExecuteAsync(arguments));
    }

    [TestMethod]
    public async Task GetFormat_WithCellParameter_ShouldReturnFormatInfo()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbook("test_get_format_cell.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            package.Workbook.Worksheets[0].Cells["A1"].Value = "Test";
            package.Save();
        }

        var arguments = new JsonObject
        {
            ["operation"] = "get_format",
            ["path"] = workbookPath,
            ["cell"] = "A1"
        };

        // Act
        var result = await _tool.ExecuteAsync(arguments);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("A1", result);
        Assert.Contains("fontName", result);
    }

    [TestMethod]
    public async Task GetFormat_WithoutCellOrRange_ShouldThrowArgumentException()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbook("test_get_format_no_cell.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "get_format",
            ["path"] = workbookPath
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _tool.ExecuteAsync(arguments));
        Assert.Contains("cell", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [TestMethod]
    public async Task GetFormat_WithInvalidRange_ShouldThrowArgumentException()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbook("test_get_format_invalid_range.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "get_format",
            ["path"] = workbookPath,
            ["range"] = "INVALID"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _tool.ExecuteAsync(arguments));
        Assert.Contains("Invalid", ex.Message);
    }

    [TestMethod]
    public async Task GetFormat_WithMultipleCells_ShouldReturnAllCellFormats()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbook("test_get_format_range.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            package.Workbook.Worksheets[0].Cells["A1"].Value = "Test1";
            package.Workbook.Worksheets[0].Cells["A2"].Value = "Test2";
            package.Save();
        }

        var arguments = new JsonObject
        {
            ["operation"] = "get_format",
            ["path"] = workbookPath,
            ["range"] = "A1:A2"
        };

        // Act
        var result = await _tool.ExecuteAsync(arguments);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("A1", result);
        Assert.Contains("A2", result);
        Assert.Contains("\"count\": 2", result);
    }

    [TestMethod]
    public async Task CopySheetFormat_WithColumnWidthsOnly_ShouldCopyColumnWidths()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbook("test_copy_column_widths.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            var sourceSheet = package.Workbook.Worksheets[0];
            sourceSheet.Column(1).Width = 20;
            package.Workbook.Worksheets.Add("TargetSheet");
            package.Save();
        }

        var outputPath = CreateTestFilePath("test_copy_column_widths_output.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "copy_sheet_format",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["sourceSheetIndex"] = 0,
            ["targetSheetIndex"] = 1,
            ["copyColumnWidths"] = true,
            ["copyRowHeights"] = false
        };

        // Act
        var result = await _tool.ExecuteAsync(arguments);

        // Assert
        Assert.Contains("复制", result);
        Assert.True(File.Exists(outputPath), "Output workbook should be created");

        using var resultPackage = new ExcelPackage(new FileInfo(outputPath));
        var targetSheet = resultPackage.Workbook.Worksheets[1];
        Assert.Equal(20, targetSheet.Column(1).Width, 1);
    }

    [TestMethod]
    public async Task InvalidOperation_ShouldThrowArgumentException()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbook("test_invalid_operation.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "invalid_operation",
            ["path"] = workbookPath
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _tool.ExecuteAsync(arguments));
        Assert.Contains("未知操作", ex.Message);
    }

    [TestMethod]
    public async Task FormatCells_WithBuiltInNumberFormat_ShouldApplyFormat()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbook("test_format_builtin_number.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            package.Workbook.Worksheets[0].Cells["A1"].Value = 1234.56;
            package.Save();
        }

        var outputPath = CreateTestFilePath("test_format_builtin_number_output.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "format",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["range"] = "A1",
            ["numberFormat"] = "4" // Built-in format number
        };

        // Act
        await _tool.ExecuteAsync(arguments);

        // Assert
        using var resultPackage = new ExcelPackage(new FileInfo(outputPath));
        var style = resultPackage.Workbook.Worksheets[0].Cells["A1"].Style;
        Assert.NotNull(style.Numberformat.Format);
    }

    [TestMethod]
    public async Task FormatCells_WithDifferentBorderStyles_ShouldApplyCorrectStyle()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbook("test_format_border_styles.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            package.Workbook.Worksheets[0].Cells["A1"].Value = "Test";
            package.Save();
        }

        var outputPath = CreateTestFilePath("test_format_border_styles_output.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "format",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["range"] = "A1",
            ["borderStyle"] = "double"
        };

        // Act
        await _tool.ExecuteAsync(arguments);

        // Assert
        using var resultPackage = new ExcelPackage(new FileInfo(outputPath));
        var style = resultPackage.Workbook.Worksheets[0].Cells["A1"].Style;
        Assert.Equal(ExcelBorderStyle.Double, style.Border.Top.Style);
    }

    [TestMethod]
    public async Task FormatCells_WithPatternFill_ShouldApplyPattern()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbook("test_format_pattern.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            package.Workbook.Worksheets[0].Cells["A1"].Value = "Pattern Test";
            package.Save();
        }

        var outputPath = CreateTestFilePath("test_format_pattern_output.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "format",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["range"] = "A1",
            ["patternType"] = "DiagonalStripe",
            ["backgroundColor"] = "#FF0000",
            ["patternColor"] = "#FFFFFF"
        };

        // Act
        await _tool.ExecuteAsync(arguments);

        // Assert
        using var resultPackage = new ExcelPackage(new FileInfo(outputPath));
        var style = resultPackage.Workbook.Worksheets[0].Cells["A1"].Style;
        Assert.NotEqual(ExcelFillStyle.None, style.Fill.PatternType);
    }

    [TestMethod]
    public async Task FormatCells_WithGray50Pattern_ShouldApplyPattern()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbook("test_format_gray50.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            package.Workbook.Worksheets[0].Cells["A1"].Value = "Gray50 Test";
            package.Save();
        }

        var outputPath = CreateTestFilePath("test_format_gray50_output.xlsx");
        var arguments = new JsonObject
        {
            ["operation"] = "format",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["range"] = "A1",
            ["patternType"] = "Gray50",
            ["backgroundColor"] = "#0000FF"
        };

        // Act
        await _tool.ExecuteAsync(arguments);

        // Assert
        using var resultPackage = new ExcelPackage(new FileInfo(outputPath));
        var style = resultPackage.Workbook.Worksheets[0].Cells["A1"].Style;
        Assert.NotEqual(ExcelFillStyle.None, style.Fill.PatternType);
    }

    [TestMethod]
    public async Task GetFormat_WithFieldsParameter_ShouldReturnOnlyRequestedFields()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbook("test_get_format_fields.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            package.Workbook.Worksheets[0].Cells["A1"].Value = "Test";
            package.Save();
        }

        var arguments = new JsonObject
        {
            ["operation"] = "get_format",
            ["path"] = workbookPath,
            ["range"] = "A1",
            ["fields"] = "font"
        };

        // Act
        var result = await _tool.ExecuteAsync(arguments);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("fontName", result);
        Assert.Contains("fontSize", result);
        Assert.DoesNotContain("borders", result);
        Assert.DoesNotContain("horizontalAlignment", result);
    }

    [TestMethod]
    public async Task GetFormat_WithMultipleFields_ShouldReturnRequestedFields()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbook("test_get_format_multi_fields.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            package.Workbook.Worksheets[0].Cells["A1"].Value = "Test";
            package.Save();
        }

        var arguments = new JsonObject
        {
            ["operation"] = "get_format",
            ["path"] = workbookPath,
            ["range"] = "A1",
            ["fields"] = "font,color"
        };

        // Act
        var result = await _tool.ExecuteAsync(arguments);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("fontName", result);
        Assert.Contains("fontColor", result);
        Assert.Contains("patternType", result);
        Assert.DoesNotContain("borders", result);
    }

    [TestMethod]
    public async Task GetFormat_WithColorField_ShouldIncludePatternType()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbook("test_get_format_color_field.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            var cell = package.Workbook.Worksheets[0].Cells["A1"];
            cell.Value = "Test";
            cell.Style.Fill.PatternType = ExcelFillStyle.DarkGrid;
            cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Red);
            package.Save();
        }

        var arguments = new JsonObject
        {
            ["operation"] = "get_format",
            ["path"] = workbookPath,
            ["range"] = "A1",
            ["fields"] = "color"
        };

        // Act
        var result = await _tool.ExecuteAsync(arguments);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("patternType", result);
        Assert.Contains("foregroundColor", result);
        Assert.Contains("backgroundColor", result);
    }

    [TestMethod]
    public async Task GetFormat_WithAlignmentField_ShouldReturnAlignmentOnly()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbook("test_get_format_alignment_field.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            package.Workbook.Worksheets[0].Cells["A1"].Value = "Test";
            package.Save();
        }

        var arguments = new JsonObject
        {
            ["operation"] = "get_format",
            ["path"] = workbookPath,
            ["range"] = "A1",
            ["fields"] = "alignment"
        };

        // Act
        var result = await _tool.ExecuteAsync(arguments);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("horizontalAlignment", result);
        Assert.Contains("verticalAlignment", result);
        Assert.DoesNotContain("fontName", result);
        Assert.DoesNotContain("borders", result);
    }

    [TestMethod]
    public async Task GetFormat_WithValueField_ShouldReturnValueInfo()
    {
        // Arrange
        var workbookPath = CreateExcelWorkbook("test_get_format_value_field.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            package.Workbook.Worksheets[0].Cells["A1"].Value = "TestValue";
            package.Save();
        }

        var arguments = new JsonObject
        {
            ["operation"] = "get_format",
            ["path"] = workbookPath,
            ["range"] = "A1",
            ["fields"] = "value"
        };

        // Act
        var result = await _tool.ExecuteAsync(arguments);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("TestValue", result);
        Assert.Contains("dataType", result);
        Assert.DoesNotContain("fontName", result);
    }
}



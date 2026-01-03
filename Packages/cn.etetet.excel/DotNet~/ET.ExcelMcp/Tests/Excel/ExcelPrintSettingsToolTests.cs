using System.Text.Json;
using System.Text.Json.Nodes;
using ET.Test;
using ET.Tools.Excel;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace ET.Test;

[TestClass]
public class ExcelPrintSettingsToolTests : ExcelTestBase
{
    private readonly ExcelPrintSettingsTool _tool = new();

    [TestMethod]
    public async Task PageSetup_ShouldUpdatePrinterSettings()
    {
        var workbookPath = CreateExcelWorkbook("print_page_setup.xlsx");
        var outputPath = CreateTestFilePath("print_page_setup_out.xlsx");

        var args = new JsonObject
        {
            ["operation"] = "page_setup",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["orientation"] = "landscape",
            ["paperSize"] = "A4",
            ["scale"] = 120,
            ["fitToPagesWide"] = 1,
            ["fitToPagesTall"] = 2,
            ["margins"] = new JsonObject
            {
                ["top"] = 1.1,
                ["bottom"] = 1.2
            }
        };

        await _tool.ExecuteAsync(args);

        using var package = new ExcelPackage(new FileInfo(outputPath));
        var settings = package.Workbook.Worksheets[0].PrinterSettings;
        Assert.Equal(eOrientation.Landscape, settings.Orientation);
        Assert.Equal(ePaperSize.A4, settings.PaperSize);
        Assert.Equal(120, settings.Scale);
        Assert.Equal(1, settings.FitToWidth);
        Assert.Equal(2, settings.FitToHeight);
        Assert.True(settings.FitToPage);
        Assert.Equal(1.1m, settings.TopMargin, 3);
        Assert.Equal(1.2m, settings.BottomMargin, 3);
    }

    [TestMethod]
    public async Task HeaderFooter_ShouldApplyTexts()
    {
        var workbookPath = CreateExcelWorkbookWithData("print_header.xlsx");
        var outputPath = CreateTestFilePath("print_header_out.xlsx");

        var args = new JsonObject
        {
            ["operation"] = "header_footer",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["header"] = new JsonObject
            {
                ["left"] = "左侧",
                ["center"] = "销售报告",
                ["right"] = "&[Date]"
            },
            ["footer"] = new JsonObject
            {
                ["left"] = "&[Page]",
                ["center"] = "机密",
                ["right"] = "右侧"
            }
        };

        await _tool.ExecuteAsync(args);

        using var package = new ExcelPackage(new FileInfo(outputPath));
        var headerFooter = package.Workbook.Worksheets[0].HeaderFooter;
        Assert.Equal("销售报告", headerFooter.OddHeader.CenteredText);
        Assert.Equal("&[Page]", headerFooter.OddFooter.LeftAlignedText);
        Assert.Equal("右侧", headerFooter.OddFooter.RightAlignedText);
    }

    [TestMethod]
    public async Task PrintArea_ShouldSetAndClear()
    {
        var workbookPath = CreateExcelWorkbookWithData("print_area.xlsx");
        var outputPath = CreateTestFilePath("print_area_out.xlsx");

        var setArgs = new JsonObject
        {
            ["operation"] = "print_area",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["range"] = "A1:C5"
        };

        await _tool.ExecuteAsync(setArgs);
        using (var package = new ExcelPackage(new FileInfo(outputPath)))
        {
            Assert.Equal("Sheet1!$A$1:$C$5", package.Workbook.Worksheets[0].PrinterSettings.PrintArea?.Address);
        }

        var clearOutput = CreateTestFilePath("print_area_cleared.xlsx");
        var clearArgs = new JsonObject
        {
            ["operation"] = "print_area",
            ["path"] = outputPath,
            ["outputPath"] = clearOutput,
            ["clear"] = true
        };

        await _tool.ExecuteAsync(clearArgs);
        using var clearedPackage = new ExcelPackage(new FileInfo(clearOutput));
        Assert.Null(clearedPackage.Workbook.Worksheets[0].PrinterSettings.PrintArea);
    }

    [TestMethod]
    public async Task GetPrintSettings_ShouldReturnJson()
    {
        var workbookPath = CreateExcelWorkbook("print_get.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            var worksheet = package.Workbook.Worksheets[0];
            worksheet.PrinterSettings.Orientation = eOrientation.Landscape;
            worksheet.PrinterSettings.PrintArea = worksheet.Cells["A1:B2"];
            worksheet.HeaderFooter.OddHeader.LeftAlignedText = "Left";
            package.Save();
        }

        var args = new JsonObject
        {
            ["operation"] = "get",
            ["path"] = workbookPath
        };

        var result = await _tool.ExecuteAsync(args);
        using var json = JsonDocument.Parse(result);
        var root = json.RootElement;
        Assert.Equal("Landscape", root.GetProperty("orientation").GetString());
        Assert.Equal("Sheet1!$A$1:$B$2", root.GetProperty("printArea").GetString());
        Assert.Equal(0, root.GetProperty("fitToWidth").GetInt32());
        Assert.Equal("Left", root.GetProperty("header").GetProperty("left").GetString());
    }
}



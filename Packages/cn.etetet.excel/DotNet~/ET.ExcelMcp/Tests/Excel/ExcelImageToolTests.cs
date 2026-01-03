using System.Text.Json;
using System.Text.Json.Nodes;
using ET.Test;
using ET.Tools.Excel;
using OfficeOpenXml;

namespace ET.Test;

[TestClass]
public class ExcelImageToolTests : ExcelTestBase
{
    private readonly ExcelImageTool _tool = new();

    [TestMethod]
    public async Task AddImage_ShouldInsertDrawing()
    {
        var workbookPath = CreateExcelWorkbook("image_add.xlsx");
        var imagePath = CreateTestImage("img_add.png");
        var outputPath = CreateTestFilePath("image_add_out.xlsx");

        var args = new JsonObject
        {
            ["operation"] = "add",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["imagePath"] = imagePath,
            ["cell"] = "B2",
            ["width"] = 150,
            ["keepAspectRatio"] = true
        };

        await _tool.ExecuteAsync(args);
        using var package = new ExcelPackage(new FileInfo(outputPath));
        Assert.Single(package.Workbook.Worksheets[0].Drawings);
    }

    [TestMethod]
    public async Task GetImages_ShouldReturnMetadata()
    {
        var workbookPath = CreateExcelWorkbook("image_get.xlsx");
        var imagePath = CreateTestImage("img_get.png");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            var ws = package.Workbook.Worksheets[0];
            ws.Drawings.AddPicture("Pic", new FileInfo(imagePath));
            package.Save();
        }

        var args = new JsonObject
        {
            ["operation"] = "get",
            ["path"] = workbookPath
        };

        var result = await _tool.ExecuteAsync(args);
        using var json = JsonDocument.Parse(result);
        Assert.Equal(1, json.RootElement.GetProperty("count").GetInt32());
    }

    [TestMethod]
    public async Task DeleteImage_ShouldRemoveDrawing()
    {
        var workbookPath = CreateExcelWorkbook("image_delete.xlsx");
        var imagePath = CreateTestImage("img_delete.png");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            package.Workbook.Worksheets[0].Drawings.AddPicture("Pic", new FileInfo(imagePath));
            package.Save();
        }

        var outputPath = CreateTestFilePath("image_delete_out.xlsx");
        var args = new JsonObject
        {
            ["operation"] = "delete",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["imageIndex"] = 0
        };

        await _tool.ExecuteAsync(args);
        using var packageOut = new ExcelPackage(new FileInfo(outputPath));
        Assert.Empty(packageOut.Workbook.Worksheets[0].Drawings);
    }

    [TestMethod]
    public async Task ExtractImage_ShouldCreateFile()
    {
        var workbookPath = CreateExcelWorkbook("image_extract.xlsx");
        var imagePath = CreateTestImage("img_extract.png");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            package.Workbook.Worksheets[0].Drawings.AddPicture("Pic", new FileInfo(imagePath));
            package.Save();
        }

        var exportPath = CreateTestFilePath("extracted.png");
        var args = new JsonObject
        {
            ["operation"] = "extract",
            ["path"] = workbookPath,
            ["imageIndex"] = 0,
            ["exportPath"] = exportPath
        };

        await _tool.ExecuteAsync(args);
        Assert.True(File.Exists(exportPath));
        Assert.True(new FileInfo(exportPath).Length > 0);
    }

    [TestMethod]
    public async Task AddImage_UnsupportedFormat_ShouldThrow()
    {
        var workbookPath = CreateExcelWorkbook("image_invalid.xlsx");
        var invalidPath = CreateTestFilePath("image.txt");
        await File.WriteAllTextAsync(invalidPath, "not image");

        var args = new JsonObject
        {
            ["operation"] = "add",
            ["path"] = workbookPath,
            ["imagePath"] = invalidPath,
            ["cell"] = "A1"
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _tool.ExecuteAsync(args));
    }

    private string CreateTestImage(string fileName)
    {
        // 1x1 PNG (red pixel)
        const string Base64 =
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR4nGNgYAAAAAMAASsJTYQAAAAASUVORK5CYII=";
        var path = CreateTestFilePath(fileName);
        File.WriteAllBytes(path, Convert.FromBase64String(Base64));
        return path;
    }
}



using System.Text.Json;
using System.Text.Json.Nodes;
using ET.Test;
using ET.Tools.Excel;
using OfficeOpenXml;

namespace ET.Test;

public class ExcelCommentToolTests : ExcelTestBase
{
    private readonly ExcelCommentTool _tool = new();

    [Fact]
    public async Task AddComment_ShouldCreateComment()
    {
        var workbookPath = CreateExcelWorkbook("comment_add.xlsx");
        var outputPath = CreateTestFilePath("comment_add_out.xlsx");
        var args = new JsonObject
        {
            ["operation"] = "add",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["cell"] = "A1",
            ["comment"] = "Test comment"
        };

        await _tool.ExecuteAsync(args);

        using var package = new ExcelPackage(new FileInfo(outputPath));
        var comment = package.Workbook.Worksheets[0].Cells["A1"].Comment;
        Assert.NotNull(comment);
        Assert.Equal("Test comment", comment.Text);
    }

    [Fact]
    public async Task GetComments_ShouldReturnInfo()
    {
        var workbookPath = CreateExcelWorkbook("comment_get.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            var ws = package.Workbook.Worksheets[0];
            ws.Cells["A1"].AddComment("Hello", "Tester");
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

    [Fact]
    public async Task EditComment_ShouldUpdateText()
    {
        var workbookPath = CreateExcelWorkbook("comment_edit.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            var ws = package.Workbook.Worksheets[0];
            ws.Cells["A1"].AddComment("Old", "Tester");
            package.Save();
        }

        var outputPath = CreateTestFilePath("comment_edit_out.xlsx");
        var args = new JsonObject
        {
            ["operation"] = "edit",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["cell"] = "A1",
            ["comment"] = "New comment"
        };

        await _tool.ExecuteAsync(args);
        using var packageOut = new ExcelPackage(new FileInfo(outputPath));
        var comment = packageOut.Workbook.Worksheets[0].Cells["A1"].Comment;
        Assert.Equal("New comment", comment?.Text);
    }

    [Fact]
    public async Task DeleteComment_ShouldRemoveComment()
    {
        var workbookPath = CreateExcelWorkbook("comment_delete.xlsx");
        using (var package = new ExcelPackage(new FileInfo(workbookPath)))
        {
            var ws = package.Workbook.Worksheets[0];
            ws.Cells["A1"].AddComment("Bye", "Tester");
            package.Save();
        }

        var outputPath = CreateTestFilePath("comment_delete_out.xlsx");
        var args = new JsonObject
        {
            ["operation"] = "delete",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["cell"] = "A1"
        };

        await _tool.ExecuteAsync(args);
        using var packageOut = new ExcelPackage(new FileInfo(outputPath));
        Assert.Null(packageOut.Workbook.Worksheets[0].Cells["A1"].Comment);
    }

    [Fact]
    public async Task Add_InvalidCell_ShouldThrow()
    {
        var workbookPath = CreateExcelWorkbook("comment_invalid_cell.xlsx");
        var args = new JsonObject
        {
            ["operation"] = "add",
            ["path"] = workbookPath,
            ["cell"] = "Invalid",
            ["comment"] = "Test"
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _tool.ExecuteAsync(args));
    }

    [Fact]
    public async Task Get_NoComments_ShouldReturnZero()
    {
        var workbookPath = CreateExcelWorkbook("comment_none.xlsx");
        var args = new JsonObject
        {
            ["operation"] = "get",
            ["path"] = workbookPath
        };

        var result = await _tool.ExecuteAsync(args);
        Assert.Contains("\"count\": 0", result);
    }
}

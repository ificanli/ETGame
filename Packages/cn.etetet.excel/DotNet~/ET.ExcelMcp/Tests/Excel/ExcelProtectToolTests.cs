using System.Text.Json.Nodes;
using ET.Test;
using ET.Tools.Excel;
using OfficeOpenXml;

namespace ET.Test;

[TestClass]
public class ExcelProtectToolTests : ExcelTestBase
{
    private readonly ExcelProtectTool _tool = new();

    [TestMethod]
    public async Task ProtectSheet_ShouldEnableProtectionWithOptions()
    {
        var workbookPath = CreateExcelWorkbook("protect_sheet.xlsx");
        var outputPath = CreateTestFilePath("protect_sheet_out.xlsx");
        var args = new JsonObject
        {
            ["operation"] = "protect_sheet",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["sheetIndex"] = 0,
            ["password"] = "secret",
            ["allowSelectLockedCells"] = false,
            ["allowSelectUnlockedCells"] = true,
            ["allowSorting"] = true,
            ["allowFiltering"] = true
        };

        await _tool.ExecuteAsync(args);

        using var package = new ExcelPackage(new FileInfo(outputPath));
        var worksheet = package.Workbook.Worksheets[0];
        Assert.True(worksheet.Protection.IsProtected);
        Assert.False(worksheet.Protection.AllowSelectLockedCells);
        Assert.True(worksheet.Protection.AllowSelectUnlockedCells);
        Assert.True(worksheet.Protection.AllowSort);
        Assert.True(worksheet.Protection.AllowAutoFilter);
    }

    [TestMethod]
    public async Task UnprotectSheet_ShouldDisableProtection()
    {
        var workbookPath = CreateExcelWorkbook("unprotect_sheet.xlsx");
        var protectedPath = CreateTestFilePath("sheet_protected.xlsx");
        var unprotectedPath = CreateTestFilePath("sheet_unprotected.xlsx");

        await _tool.ExecuteAsync(new JsonObject
        {
            ["operation"] = "protect_sheet",
            ["path"] = workbookPath,
            ["outputPath"] = protectedPath,
            ["sheetIndex"] = 0
        });

        await _tool.ExecuteAsync(new JsonObject
        {
            ["operation"] = "unprotect_sheet",
            ["path"] = protectedPath,
            ["outputPath"] = unprotectedPath,
            ["sheetIndex"] = 0
        });

        using var package = new ExcelPackage(new FileInfo(unprotectedPath));
        Assert.False(package.Workbook.Worksheets[0].Protection.IsProtected);
    }

    [TestMethod]
    public async Task ProtectWorkbook_ShouldLockStructureAndWindows()
    {
        var workbookPath = CreateExcelWorkbook("protect_workbook.xlsx");
        var outputPath = CreateTestFilePath("protect_workbook_out.xlsx");

        await _tool.ExecuteAsync(new JsonObject
        {
            ["operation"] = "protect_workbook",
            ["path"] = workbookPath,
            ["outputPath"] = outputPath,
            ["lockStructure"] = true,
            ["lockWindows"] = true
        });

        using var package = new ExcelPackage(new FileInfo(outputPath));
        var protection = package.Workbook.Protection;
        Assert.True(protection.LockStructure);
        Assert.True(protection.LockWindows);
        Assert.False(protection.LockRevision);
    }

    [TestMethod]
    public async Task LockAndUnlockCells_ShouldToggleLockedFlag()
    {
        var workbookPath = CreateExcelWorkbookWithData("lock_cells.xlsx");
        var unlockedPath = CreateTestFilePath("lock_cells_unlocked.xlsx");
        var lockedAgainPath = CreateTestFilePath("lock_cells_locked.xlsx");

        await _tool.ExecuteAsync(new JsonObject
        {
            ["operation"] = "unlock_cells",
            ["path"] = workbookPath,
            ["outputPath"] = unlockedPath,
            ["sheetIndex"] = 0,
            ["ranges"] = new JsonArray("A1:B2", "C3")
        });

        using (var package = new ExcelPackage(new FileInfo(unlockedPath)))
        {
            Assert.False(package.Workbook.Worksheets[0].Cells["A1"].Style.Locked);
            Assert.False(package.Workbook.Worksheets[0].Cells["C3"].Style.Locked);
        }

        await _tool.ExecuteAsync(new JsonObject
        {
            ["operation"] = "lock_cells",
            ["path"] = unlockedPath,
            ["outputPath"] = lockedAgainPath,
            ["sheetIndex"] = 0,
            ["ranges"] = new JsonArray("A1:B2", "C3")
        });

        using var packageLocked = new ExcelPackage(new FileInfo(lockedAgainPath));
        Assert.True(packageLocked.Workbook.Worksheets[0].Cells["A1"].Style.Locked);
        Assert.True(packageLocked.Workbook.Worksheets[0].Cells["C3"].Style.Locked);
    }
}



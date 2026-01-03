using OfficeOpenXml;

namespace ET.Test;

/// <summary>
///     Base class for Excel tool tests providing Excel-specific functionality
/// </summary>
public abstract class ExcelTestBase : TestBase
{
    static ExcelTestBase()
    {
        // 设置 EPPlus 的许可证上下文
        ExcelPackage.License.SetNonCommercialOrganization("ETET");
    }

    /// <summary>
    ///     Creates a new Excel workbook for testing
    /// </summary>
    protected string CreateExcelWorkbook(string fileName)
    {
        var filePath = CreateTestFilePath(fileName);
        using var package = new ExcelPackage();
        package.Workbook.Worksheets.Add("Sheet1");
        package.SaveAs(new FileInfo(filePath));
        return filePath;
    }

    /// <summary>
    ///     Creates an Excel workbook with sample data
    /// </summary>
    protected string CreateExcelWorkbookWithData(string fileName, int rowCount = 5, int columnCount = 3)
    {
        var filePath = CreateTestFilePath(fileName);
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Sheet1");

        for (var row = 1; row <= rowCount; row++)
        for (var col = 1; col <= columnCount; col++)
            worksheet.Cells[row, col].Value = $"R{row}C{col}";

        package.SaveAs(new FileInfo(filePath));
        return filePath;
    }

    /// <summary>
    ///     Verifies that a cell has the expected value
    /// </summary>
    protected void AssertCellValue(ExcelPackage package, int sheetIndex, int row, int column, object expectedValue)
    {
        var worksheet = package.Workbook.Worksheets[sheetIndex];
        var actualValue = worksheet.Cells[row, column].Value;
        Assert.Equal(expectedValue, actualValue);
    }
}

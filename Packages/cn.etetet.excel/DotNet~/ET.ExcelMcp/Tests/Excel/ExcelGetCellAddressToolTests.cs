using System.Text.Json.Nodes;
using ET.Test;
using ET.Tools.Excel;

namespace ET.Test;

[TestClass]
public class ExcelGetCellAddressToolTests : TestBase
{
    private readonly ExcelGetCellAddressTool _tool = new();

    [TestMethod]
    public async Task ConvertAddressToIndex_ShouldReturnZeroBasedCoords()
    {
        var args = new JsonObject { ["cellAddress"] = "C3" };

        var result = await _tool.ExecuteAsync(args);

        Assert.Equal("C3 = Row 2, Column 2", result);
    }

    [TestMethod]
    public async Task ConvertIndexToAddress_ShouldSupportMaxBounds()
    {
        var args = new JsonObject
        {
            ["row"] = 1_048_575,
            ["column"] = 16_383
        };

        var result = await _tool.ExecuteAsync(args);

        Assert.Equal("XFD1048576 = Row 1048575, Column 16383", result);
    }

    [TestMethod]
    public async Task MissingParameters_ShouldThrow()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _tool.ExecuteAsync(new JsonObject()));
    }

    [TestMethod]
    public async Task PartialRowColumn_ShouldThrow()
    {
        var args = new JsonObject { ["row"] = 5 };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _tool.ExecuteAsync(args));

        Assert.Contains("row 和 column", ex.Message);
    }

    [TestMethod]
    public async Task OutOfRangeIndexes_ShouldThrow()
    {
        var args = new JsonObject
        {
            ["row"] = -1,
            ["column"] = 0
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _tool.ExecuteAsync(args));

        Assert.Contains("row 超出范围", ex.Message);
    }
}



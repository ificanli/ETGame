using OfficeOpenXml;

ExcelPackage.License.SetNonCommercialOrganization("ETET");

var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var dataFile = Path.Combine(root, "Packages", "cn.etetet.statesync", "Luban", "Config", "Datas", "HeroDisplay.xlsx");
var tablesFile = Path.Combine(root, "Packages", "cn.etetet.statesync", "Luban", "Config", "Base", "__tables__.xlsx");
Directory.CreateDirectory(Path.GetDirectoryName(dataFile)!);

using (var package = new ExcelPackage())
{
    var ws = package.Workbook.Worksheets.Add("HeroDisplay");
    ws.Cells[1,1].Value = "##var";
    ws.Cells[1,2].Value = "Id";
    ws.Cells[1,3].Value = "ModelResName";
    ws.Cells[1,4].Value = "CameraName";

    ws.Cells[2,1].Value = "##type";
    ws.Cells[2,2].Value = "int";
    ws.Cells[2,3].Value = "string";
    ws.Cells[2,4].Value = "string";

    ws.Cells[3,1].Value = "##group";
    ws.Cells[3,2].Value = "c";
    ws.Cells[3,3].Value = "c";
    ws.Cells[3,4].Value = "c";

    ws.Cells[4,1].Value = "##";
    ws.Cells[4,2].Value = "英雄配置Id";
    ws.Cells[4,3].Value = "3D模型资源名";
    ws.Cells[4,4].Value = "模型内观察相机节点名(可空)";

    (int, string, string)[] rows =
    [
        (1001, "TaoZi", ""),
        (1002, "PuTao", ""),
        (1003, "CaoMei", "")
    ];

    for (var i = 0; i < rows.Length; i++)
    {
        var row = 5 + i;
        ws.Cells[row, 2].Value = rows[i].Item1;
        ws.Cells[row, 3].Value = rows[i].Item2;
        ws.Cells[row, 4].Value = rows[i].Item3;
    }

    ws.Cells[ws.Dimension.Address].AutoFitColumns();
    package.SaveAs(new FileInfo(dataFile));
}

using (var package = new ExcelPackage(new FileInfo(tablesFile)))
{
    var ws = package.Workbook.Worksheets.FirstOrDefault() ?? package.Workbook.Worksheets.Add("Tables");
    var row = 4;
    while (!string.IsNullOrWhiteSpace(ws.Cells[row, 2].Text) && ws.Cells[row, 2].Text != "HeroDisplayConfig")
    {
        row++;
    }

    ws.Cells[row, 2].Value = "HeroDisplayConfig";
    ws.Cells[row, 3].Value = "ET.HeroDisplayConfigCategory";
    ws.Cells[row, 4].Value = "HeroDisplayConfig";
    ws.Cells[row, 5].Value = true;
    ws.Cells[row, 6].Value = "../../../../cn.etetet.statesync/Luban/Config/Datas/HeroDisplay.xlsx";
    ws.Cells[row, 10].Value = "英雄大厅3D展示配置表";
    ws.Cells[row, 12].Value = "HeroDisplayConfigCategory";
    package.Save();
}

Console.WriteLine("HeroDisplay table created.");

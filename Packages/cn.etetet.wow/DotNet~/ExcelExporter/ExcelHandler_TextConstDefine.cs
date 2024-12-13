using System.IO;
using System.Text;
using OfficeOpenXml;

namespace ET
{
    public class ExcelHandler_TextConstDefine: IExcelHandler
    {
        public void Run()
        {
            ExcelPackage excelPackage = ExcelExporter.GetPackage(Path.GetFullPath("Packages/cn.etetet.wow/Luban/Datas/Text.xlsx"));

            StringBuilder sb = new();
            sb.Append("namespace ET\n");
            sb.Append("{\n");
            sb.Append("\tpublic static partial class TextConstDefine\n");
            sb.Append("\t{\n");
            ExcelWorksheet workbookWorksheet = excelPackage.Workbook.Worksheets[0];
            for (int i = 4; i <= workbookWorksheet.Dimension.End.Row; ++i)
            {
                string Name = workbookWorksheet.Cells[i, 3].Text.Trim();
                string Id = workbookWorksheet.Cells[i, 2].Text.Trim();

                sb.Append($"\t\tpublic const int {Name} = {Id};\n");

                sb.Append($"\n");
            }
            
            sb.Append("\t}\n");
            sb.Append("}");
            
            File.WriteAllText("Packages/cn.etetet.wow/Scripts/Model/Share/TextConstDefine.cs", sb.ToString());
        }
    }
}
using System;
using System.IO;
using System.Text;
using OfficeOpenXml;

namespace ET
{
    public class ExcelHandler_NumericType : IExcelHandler
    {
        public void Run()
        {
            ExcelPackage excelPackage = ExcelExporter.GetPackage(Path.GetFullPath("Packages/cn.etetet.wow/Excel/NumericTypeConfig.xlsx"));

            StringBuilder sb = new();
            sb.Append("namespace ET\n");
            sb.Append("{\n");
            sb.Append("\tpublic enum NumericType\n");
            sb.Append("\t{\n");
            ExcelWorksheet workbookWorksheet = excelPackage.Workbook.Worksheets[0];
            for (int i = 6; i <= workbookWorksheet.Dimension.End.Row; ++i)
            {
                string Name = workbookWorksheet.Cells[i, 4].Text.Trim();
                string Id = workbookWorksheet.Cells[i, 3].Text.Trim();
                string GenSecondAttr = workbookWorksheet.Cells[i, 5].Text.Trim();

                sb.Append($"\t\t{Name} = {Id},\n");

                if (GenSecondAttr == "1")
                {
                    sb.Append($"\t\t{Name}Base = {Id}1,\n");
                    sb.Append($"\t\t{Name}Add = {Id}2,\n");
                    sb.Append($"\t\t{Name}Pct = {Id}3,\n");
                    sb.Append($"\t\t{Name}FinalAdd = {Id}4,\n");
                    sb.Append($"\t\t{Name}FinalPct = {Id}5,\n");
                }
                sb.Append($"\n");
            }
            
            sb.Append("\t}\n");
            sb.Append("}");
            
            File.WriteAllText("Packages/cn.etetet.wow/Scripts/Loader/Share/NumericType.cs", sb.ToString());
        }
    }
}
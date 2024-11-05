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
            sb.Append("\tpublic static partial class NumericType\n");
            sb.Append("\t{\n");
            ExcelWorksheet workbookWorksheet = excelPackage.Workbook.Worksheets[0];
            for (int i = 6; i <= workbookWorksheet.Dimension.End.Row; ++i)
            {
                string Name = workbookWorksheet.Cells[i, 4].Text.Trim();
                string Id = workbookWorksheet.Cells[i, 3].Text.Trim();
                string GenSecondAttr = workbookWorksheet.Cells[i, 5].Text.Trim();

                sb.Append($"\t\tpublic const int {Name} = {Id};\n");

                if (GenSecondAttr == "1")
                {
                    sb.Append($"\t\tpublic const int {Name}Base = {Id}1;\n");
                    sb.Append($"\t\tpublic const int {Name}Add = {Id}2;\n");
                    sb.Append($"\t\tpublic const int {Name}Pct = {Id}3;\n");
                    sb.Append($"\t\tpublic const int {Name}FinalAdd = {Id}4;\n");
                    sb.Append($"\t\tpublic const int {Name}FinalPct = {Id}5;\n");
                }
                sb.Append($"\n");
            }
            
            sb.Append($"\t\tpublic const int Max = 10000;\n");
            
            sb.Append("\t}\n");
            sb.Append("}");
            
            File.WriteAllText("Packages/cn.etetet.wow/Scripts/Model/Share/NumericType.cs", sb.ToString());
        }
    }
}
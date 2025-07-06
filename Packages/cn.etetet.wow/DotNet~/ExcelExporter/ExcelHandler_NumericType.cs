using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using OfficeOpenXml;

namespace ET
{
    public class ExcelHandler_NumericType : IExcelHandler
    {
        public void Run()
        {
            // 遍历所有Packages目录，查找NumericType.xlsx文件
            string packagesDir = Path.GetFullPath("Packages");
            string[] packageDirs = Directory.GetDirectories(packagesDir, "cn.etetet.*");
            
            HashSet<string> globalProcessedIds = new HashSet<string>(); // 全局ID检查，防止跨包重复
            int totalEntries = 0;
            int processedPackages = 0;

            foreach (string packageDir in packageDirs)
            {
                string numericTypeExcelPath = Path.Combine(packageDir, "Luban/Config/Datas/NumericType.xlsx");
                if (File.Exists(numericTypeExcelPath))
                {
                    string packageName = Path.GetFileName(packageDir);
                    List<(string name, string id, string genSecondAttr)> packageEntries = new List<(string, string, string)>();
                    
                    if (ProcessNumericTypeExcel(numericTypeExcelPath, packageName, packageEntries, globalProcessedIds))
                    {
                        GenerateNumericTypeForPackage(packageDir, packageName, packageEntries);
                        totalEntries += packageEntries.Count;
                        processedPackages++;
                    }
                }
            }

            Log.Console($"NumericType generation completed! Processed {totalEntries} entries from {processedPackages} packages.");
        }

        private bool ProcessNumericTypeExcel(string excelPath, string packageName, List<(string name, string id, string genSecondAttr)> packageEntries, HashSet<string> globalProcessedIds)
        {
            try
            {
                ExcelPackage excelPackage = ExcelExporter.GetPackage(excelPath);
                ExcelWorksheet worksheet = excelPackage.Workbook.Worksheets[0];
                
                if (worksheet.Dimension == null)
                {
                    Log.Warning($"Empty worksheet in {excelPath}");
                    return false;
                }

                for (int i = 6; i <= worksheet.Dimension.End.Row; ++i)
                {
                    string name = worksheet.Cells[i, 3].Text.Trim();
                    string id = worksheet.Cells[i, 2].Text.Trim();
                    string genSecondAttr = worksheet.Cells[i, 5].Text.Trim();

                    if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(id))
                    {
                        continue;
                    }

                    if (globalProcessedIds.Contains(id))
                    {
                        Log.Warning($"Duplicate ID {id} found in {packageName}, skipping...");
                        continue;
                    }

                    globalProcessedIds.Add(id);
                    packageEntries.Add((name, id, genSecondAttr));
                }
                
                Log.Debug($"Processed {excelPath}: found {packageEntries.Count} entries");
                return packageEntries.Count > 0;
            }
            catch (System.Exception e)
            {
                Log.Error($"Error processing {excelPath}: {e.Message}");
                return false;
            }
        }

        private void GenerateNumericTypeForPackage(string packageDir, string packageName, List<(string name, string id, string genSecondAttr)> entries)
        {
            // 生成const int版本
            GenerateNumericTypeConstants(packageDir, packageName, entries);
        }
        
        private void GenerateNumericTypeConstants(string packageDir, string packageName, List<(string name, string id, string genSecondAttr)> entries)
        {
            StringBuilder sb = new();
            sb.Append("namespace ET\n");
            sb.Append("{\n");
            sb.Append("\t/// <summary>\n");
            sb.Append($"\t/// {packageName} Numeric Type Constants (Runtime)\n");
            sb.Append("\t/// </summary>\n");
            sb.Append("\tpublic static partial class NumericType\n");
            sb.Append("\t{\n");

            // 按ID排序
            entries.Sort((a, b) => string.Compare(a.id, b.id, System.StringComparison.Ordinal));

            foreach (var entry in entries)
            {
                sb.Append($"\t\tpublic const int {entry.name} = {entry.id};\n");

                if (entry.genSecondAttr == "1")
                {
                    sb.Append($"\t\tpublic const int {entry.name}Base = {entry.id}1;\n");
                    sb.Append($"\t\tpublic const int {entry.name}Add = {entry.id}2;\n");
                    sb.Append($"\t\tpublic const int {entry.name}Pct = {entry.id}3;\n");
                    sb.Append($"\t\tpublic const int {entry.name}FinalAdd = {entry.id}4;\n");
                    sb.Append($"\t\tpublic const int {entry.name}FinalPct = {entry.id}5;\n");
                }
            }
            
            sb.Append("\t}\n");
            sb.Append("}");

            // 确保Scripts/Model/Share目录存在
            string targetDir = Path.Combine(packageDir, "Scripts/Model/Share");
            Directory.CreateDirectory(targetDir);
            
            string outputPath = Path.Combine(targetDir, "NumericType.cs");
            File.WriteAllText(outputPath, sb.ToString());
            
            Log.Console($"Generated {outputPath} with {entries.Count} entries");
        }
        
    }
}
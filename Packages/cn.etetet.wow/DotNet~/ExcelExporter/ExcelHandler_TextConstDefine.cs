using System.IO;
using System.Text;
using System.Collections.Generic;
using OfficeOpenXml;

namespace ET
{
    public class ExcelHandler_TextConstDefine: IExcelHandler
    {
        public void Run()
        {
            // 遍历所有Packages目录，查找Text.xlsx文件
            string packagesDir = Path.GetFullPath("Packages");
            string[] packageDirs = Directory.GetDirectories(packagesDir, "cn.etetet.*");
            
            HashSet<string> globalProcessedIds = new HashSet<string>(); // 全局ID检查，防止跨包重复
            int totalEntries = 0;
            int processedPackages = 0;

            foreach (string packageDir in packageDirs)
            {
                string textExcelPath = Path.Combine(packageDir, "Luban/Config/Datas/Text.xlsx");
                if (File.Exists(textExcelPath))
                {
                    string packageName = Path.GetFileName(packageDir);
                    List<(string name, string id)> packageEntries = new List<(string, string)>();
                    
                    if (ProcessTextExcel(textExcelPath, packageName, packageEntries, globalProcessedIds))
                    {
                        GenerateTextConstDefineForPackage(packageDir, packageName, packageEntries);
                        totalEntries += packageEntries.Count;
                        processedPackages++;
                    }
                }
            }

            Log.Console($"TextConstDefine generation completed! Processed {totalEntries} entries from {processedPackages} packages.");
        }

        private bool ProcessTextExcel(string excelPath, string packageName, List<(string name, string id)> packageEntries, HashSet<string> globalProcessedIds)
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

                for (int i = 4; i <= worksheet.Dimension.End.Row; ++i)
                {
                    string name = worksheet.Cells[i, 3].Text.Trim();
                    string id = worksheet.Cells[i, 2].Text.Trim();

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
                    packageEntries.Add((name, id));
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

        private void GenerateTextConstDefineForPackage(string packageDir, string packageName, List<(string name, string id)> entries)
        {
            StringBuilder sb = new();
            sb.Append("namespace ET\n");
            sb.Append("{\n");
            sb.Append("\t/// <summary>\n");
            sb.Append($"\t/// {packageName} Text Constants\n");
            sb.Append("\t/// </summary>\n");
            sb.Append("\tpublic static partial class TextConstDefine\n");
            sb.Append("\t{\n");

            // 按ID排序
            entries.Sort((a, b) => string.Compare(a.id, b.id, System.StringComparison.Ordinal));

            foreach (var entry in entries)
            {
                sb.Append($"\t\tpublic const int {entry.name} = {entry.id};\n");
            }
            
            sb.Append("\t}\n");
            sb.Append("}");

            // 确保Scripts/Model/Share目录存在
            string targetDir = Path.Combine(packageDir, "Scripts/Model/Share");
            Directory.CreateDirectory(targetDir);
            
            string outputPath = Path.Combine(targetDir, "TextConstDefine.cs");
            File.WriteAllText(outputPath, sb.ToString());
            
            Log.Console($"Generated {outputPath} with {entries.Count} entries");
        }
    }
}
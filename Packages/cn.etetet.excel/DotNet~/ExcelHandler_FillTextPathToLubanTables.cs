using System.IO;
using System.Collections.Generic;
using OfficeOpenXml;

namespace ET
{
    /// <summary>
    /// 扫描Packages/cn.etetet.XXX/Luban/Config/Datas/Text.xlsx，把路径填到
    /// Packages/cn.etetet.wow/Luban/Config/Base/__tables__.xlsx表格第二列Text的行，第一行为input的列里面，用英文逗号分割
    /// 操作Excel可以参考ExcelHandler_TextConstDefine.cs
    /// </summary>
    public class ExcelHandler_FillTextPathToLubanTables : IExcelHandler
    {
        public void Run()
        {
            // 扫描所有Packages/cn.etetet.*/Luban/Config/Datas/Text.xlsx文件
            List<string> textExcelPaths = ScanTextExcelFiles();
            
            if (textExcelPaths.Count == 0)
            {
                Log.Warning("No Text.xlsx files found in any packages");
                return;
            }
            
            // 读取目标__tables__.xlsx文件并更新Text行的input列
            UpdateTablesExcel(textExcelPaths);
            
            Log.Console($"Successfully updated __tables__.xlsx with {textExcelPaths.Count} Text.xlsx paths");
        }
        
        private List<string> ScanTextExcelFiles()
        {
            List<string> textExcelPaths = new List<string>();
            
            // 获取Packages目录的绝对路径
            string packagesDir = Path.GetFullPath("Packages");
            
            if (!Directory.Exists(packagesDir))
            {
                Log.Error($"Packages directory not found: {packagesDir}");
                return textExcelPaths;
            }
            
            // 扫描所有cn.etetet.*包目录
            string[] packageDirs = Directory.GetDirectories(packagesDir, "cn.etetet.*");
            
            foreach (string packageDir in packageDirs)
            {
                string textExcelPath = Path.Combine(packageDir, "Luban/Config/Datas/Text.xlsx");
                
                if (File.Exists(textExcelPath))
                {
                    // 生成带4个../前缀的路径格式
                    string packageName = Path.GetFileName(packageDir);
                    string pathWithPrefix = $"../../../../{packageName}/Luban/Config/Datas/Text.xlsx";
                    textExcelPaths.Add(pathWithPrefix);
                    Log.Console($"Found Text.xlsx: {pathWithPrefix}");
                }
            }
            
            return textExcelPaths;
        }
        
        private void UpdateTablesExcel(List<string> textExcelPaths)
        {
            string tablesExcelPath = Path.GetFullPath("Packages/cn.etetet.wow/Luban/Config/Base/__tables__.xlsx");
            
            if (!File.Exists(tablesExcelPath))
            {
                Log.Error($"__tables__.xlsx not found: {tablesExcelPath}");
                return;
            }
            
            try
            {
                // 直接创建新的ExcelPackage，绕过缓存
                using FileStream fileStream = new FileStream(tablesExcelPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                using ExcelPackage excelPackage = new ExcelPackage(fileStream);
                ExcelWorksheet worksheet = excelPackage.Workbook.Worksheets[0];
                
                if (worksheet.Dimension == null)
                {
                    Log.Warning($"Empty worksheet in {tablesExcelPath}");
                    return;
                }
                
                // 查找Text行和input列
                int textRow = -1;
                int inputCol = -1;
                
                // 查找input列（第一行）
                for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                {
                    string headerValue = worksheet.Cells[1, col].Text.Trim();
                    if (headerValue.Equals("input", System.StringComparison.OrdinalIgnoreCase))
                    {
                        inputCol = col;
                        break;
                    }
                }
                
                if (inputCol == -1)
                {
                    Log.Error("Input column not found in __tables__.xlsx");
                    return;
                }
                
                // 查找Text行（第二列）
                for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                {
                    string cellValue = worksheet.Cells[row, 2].Text.Trim();
                    if (cellValue.Equals("Text", System.StringComparison.OrdinalIgnoreCase))
                    {
                        textRow = row;
                        break;
                    }
                }
                
                if (textRow == -1)
                {
                    Log.Error("Text row not found in __tables__.xlsx");
                    return;
                }
                
                // 将Text.xlsx路径用英文逗号分割后填入input列
                string pathsString = string.Join(",", textExcelPaths);
                worksheet.Cells[textRow, inputCol].Value = pathsString;
                
                // 强制保存Excel文件 - EPPlus需要显式保存
                excelPackage.SaveAs(new FileInfo(tablesExcelPath));
                
                Log.Console($"Updated Text row at [{textRow}, {inputCol}] with: {pathsString}");
            }
            catch (System.Exception e)
            {
                Log.Error($"Error updating {tablesExcelPath}: {e.Message}");
                Log.Error($"Stack trace: {e.StackTrace}");
            }
        }
    }
}
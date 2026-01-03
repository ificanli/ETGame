using System.IO;
using System.Collections.Generic;
using OfficeOpenXml;

namespace ET
{
    /// <summary>
    /// Excel表路径填充配置
    /// </summary>
    public class ExcelPathFillConfig
    {
        /// <summary>
        /// 要扫描的Excel文件名（如 Text.xlsx, Item.xlsx）
        /// </summary>
        public string ScanFileName { get; set; }
        
        /// <summary>
        /// 要扫描的子路径（相对于包目录，如 Luban/Config/Datas）
        /// </summary>
        public string ScanSubPath { get; set; }
        
        /// <summary>
        /// 目标__tables__.xlsx文件路径（相对于项目根目录）
        /// </summary>
        public string TargetTablesPath { get; set; }
        
        /// <summary>
        /// 在目标表中要更新的行标识（在第二列中查找）
        /// </summary>
        public string TargetRowName { get; set; }
    }
    
    /// <summary>
    /// 扫描Packages/cn.etetet.XXX目录下的Excel文件，把路径填到指定的__tables__.xlsx表格中
    /// 支持配置多个扫描任务
    /// </summary>
    public class ExcelHandler_FillTextPathToLubanTables : IExcelHandler
    {
        private List<ExcelPathFillConfig> configs;
        
        public void Run()
        {
            // 初始化配置
            InitConfigs();
            
            if (configs.Count == 0)
            {
                Log.Warning("No Excel path fill configs defined");
                return;
            }
            
            // 处理每个配置
            foreach (var config in configs)
            {
                ProcessConfig(config);
            }
            
            Log.Console($"Successfully processed {configs.Count} Excel path fill configurations");
        }
        
        /// <summary>
        /// 初始化配置列表
        /// 可以根据需要添加更多配置
        /// </summary>
        private void InitConfigs()
        {
            configs = new List<ExcelPathFillConfig>
            {
                // 配置1：扫描Text.xlsx，填到excel包的__tables__.xlsx的Text行
                new ExcelPathFillConfig
                {
                    ScanFileName = "Text.xlsx",
                    ScanSubPath = "Luban/Config/Datas",
                    TargetTablesPath = "Packages/cn.etetet.excel/Luban/Config/Base/__tables__.xlsx",
                    TargetRowName = "Text"
                },
                
                // 配置示例2：扫描其他表（需要时取消注释并修改）
                // new ExcelPathFillConfig
                // {
                //     ScanFileName = "Item.xlsx",
                //     ScanSubPath = "Luban/Config/Datas",
                //     TargetTablesPath = "Packages/cn.etetet.excel/Luban/Config/Base/__tables__.xlsx",
                //     TargetRowName = "Item"
                // },
            };
        }
        
        /// <summary>
        /// 处理单个配置
        /// </summary>
        private void ProcessConfig(ExcelPathFillConfig config)
        {
            Log.Console($"Processing config: {config.ScanFileName} -> {config.TargetRowName}");
            
            // 扫描指定的Excel文件
            List<string> excelPaths = ScanExcelFiles(config.ScanFileName, config.ScanSubPath);
            
            if (excelPaths.Count == 0)
            {
                Log.Warning($"No {config.ScanFileName} files found in any packages");
                return;
            }
            
            // 更新目标__tables__.xlsx
            UpdateTablesExcel(excelPaths, config.TargetTablesPath, config.TargetRowName);
            
            Log.Console($"Updated {config.TargetRowName} with {excelPaths.Count} paths");
        }
        
        /// <summary>
        /// 扫描指定的Excel文件
        /// </summary>
        private List<string> ScanExcelFiles(string fileName, string subPath)
        {
            List<string> excelPaths = new List<string>();
            
            // 获取Packages目录的绝对路径
            string packagesDir = Path.GetFullPath("Packages");
            
            if (!Directory.Exists(packagesDir))
            {
                Log.Error($"Packages directory not found: {packagesDir}");
                return excelPaths;
            }
            
            // 扫描所有cn.etetet.*包目录
            string[] packageDirs = Directory.GetDirectories(packagesDir, "cn.etetet.*");
            
            foreach (string packageDir in packageDirs)
            {
                string excelPath = Path.Combine(packageDir, subPath, fileName);
                
                if (File.Exists(excelPath))
                {
                    // 生成带4个../前缀的相对路径格式
                    string packageName = Path.GetFileName(packageDir);
                    string pathWithPrefix = $"../../../../{packageName}/{subPath}/{fileName}";
                    excelPaths.Add(pathWithPrefix);
                    Log.Console($"Found {fileName}: {pathWithPrefix}");
                }
            }
            
            return excelPaths;
        }
        
        /// <summary>
        /// 更新目标__tables__.xlsx文件
        /// </summary>
        private void UpdateTablesExcel(List<string> excelPaths, string tablesPath, string rowName)
        {
            string tablesExcelPath = Path.GetFullPath(tablesPath);
            
            if (!File.Exists(tablesExcelPath))
            {
                Log.Error($"Target tables file not found: {tablesExcelPath}");
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
                
                // 查找目标行和input列
                int targetRow = -1;
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
                    Log.Error($"Input column not found in {tablesExcelPath}");
                    return;
                }
                
                // 查找目标行（第二列）
                for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                {
                    string cellValue = worksheet.Cells[row, 2].Text.Trim();
                    if (cellValue.Equals(rowName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        targetRow = row;
                        break;
                    }
                }
                
                if (targetRow == -1)
                {
                    Log.Error($"{rowName} row not found in {tablesExcelPath}");
                    return;
                }
                
                // 将Excel路径用英文逗号分割后填入input列
                string pathsString = string.Join(",", excelPaths);
                worksheet.Cells[targetRow, inputCol].Value = pathsString;
                
                // 强制保存Excel文件 - EPPlus需要显式保存
                excelPackage.SaveAs(new FileInfo(tablesExcelPath));
                
                Log.Console($"Updated {rowName} row at [{targetRow}, {inputCol}] with: {pathsString}");
            }
            catch (System.Exception e)
            {
                Log.Error($"Error updating {tablesExcelPath}: {e.Message}");
                Log.Error($"Stack trace: {e.StackTrace}");
            }
        }
    }
}

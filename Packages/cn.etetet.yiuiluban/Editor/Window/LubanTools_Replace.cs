using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace YIUI.Luban.Editor
{
    public partial class LubanTools
    {
        static bool ReplaceAll(bool tips = false)
        {
            if (ReplaceExcel() && ReplaceExcelInit() && ReplaceServerCommand())
            {
                if (tips)
                {
                    UnityTipsHelper.Show("Luban替换成功");
                }

                return true;
            }
            else
            {
                UnityTipsHelper.Show("Luban替换失败，请检查日志");
                return false;
            }
        }

        static bool ReplaceExcel()
        {
            if (!DeleteExcelEditor()) return false;

            string   directoryPath = $"{Application.dataPath}/../Packages/cn.etetet.excel";
            string[] filesToKeep   = { "package.json", "packagegit.json" };

            return DeleteFilesRecursively(directoryPath, filesToKeep);
        }

        static bool DeleteFilesRecursively(string directoryPath, string[] filesToKeep)
        {
            try
            {
                string[] files          = Directory.GetFiles(directoryPath);
                string[] subDirectories = Directory.GetDirectories(directoryPath);

                foreach (string file in files)
                {
                    string fileName = Path.GetFileName(file);

                    if (!filesToKeep.Contains(fileName, StringComparer.OrdinalIgnoreCase))
                    {
                        File.Delete(file);
                    }
                }

                foreach (string subDirectory in subDirectories)
                {
                    DeleteFilesRecursively(subDirectory, filesToKeep);

                    if (Directory.GetFiles(subDirectory).Length == 0 && Directory.GetDirectories(subDirectory).Length == 0)
                    {
                        Directory.Delete(subDirectory, false);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"删除 Excel 包时报错: {e.Message}");
                return false;
            }

            return true;
        }

        static bool DeleteExcelEditor()
        {
            string filePath = $"{Application.dataPath}/../ET.Excel.Editor.csproj";

            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (Exception e)
                {
                    Debug.LogError($"删除 ET.Excel.Editor 失败 {e.Message}");
                    return false;
                }
            }

            return true;
        }

        static bool ReplaceExcelInit()
        {
            string filePath = $"{Application.dataPath}/../Packages/cn.etetet.statesync/Editor/InitHelper.cs";
            return ReplaceFile(filePath, @"ExcelEditor.Init\(\);", @"EditorApplication.ExecuteMenuItem(""ET/Excel/ExcelExporter"");", "ExcelEditor.Init");
        }

        static bool ReplaceServerCommand()
        {
            string filePath = $"{Application.dataPath}/../Packages/cn.etetet.loader/Editor/ServerCommandLineEditor/ServerCommandLineEditor.cs";
            return ReplaceFile(filePath, "Packages/cn.etetet.excel/Config/Bytes/cs/StartConfig", "Packages/cn.etetet.yiuilubangen/Assets/Config/Bin/Server/StartConfig");
        }

        static bool ReplaceFile(string filePath, string oldText, string newText, string checkString = "")
        {
            if (!File.Exists(filePath))
            {
                Debug.Log($"文件不存在：{filePath}");
                return true;
            }

            try
            {
                string content = File.ReadAllText(filePath);
                if (!content.Contains(string.IsNullOrEmpty(checkString) ? oldText : checkString))
                {
                    return true;
                }

                content = Regex.Replace(content, oldText, newText);

                File.WriteAllText(filePath, content, Encoding.UTF8);

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"替换失败：{filePath} {ex.Message}");
            }

            return false;
        }

        static bool SyncInvoke()
        {
            var packagePath = $"{Application.dataPath}/../Packages";
            var sourcePath  = $"{packagePath}/cn.etetet.yiuiluban/.Template/cn.etetet.yiuilubangen/Scripts";
            var targetPath  = $"{packagePath}/cn.etetet.yiuilubangen/Scripts";
            var clientFile  = $"/HotfixView/Client/LubanClientLoaderInvoker.cs";
            var serverFile  = $"/Hotfix/Server/LubanServerLoaderInvoker.cs";

            if (CoverFile($"{sourcePath}{clientFile}", $"{targetPath}{clientFile}") &&
                CoverFile($"{sourcePath}{serverFile}", $"{targetPath}{serverFile}"))
            {
                CloseWindowRefresh();
                return true;
            }

            return false;
        }

        static bool CoverFile(string sourcePath, string targetPath)
        {
            if (!File.Exists(sourcePath))
            {
                Debug.LogError($"源文件未找到: {sourcePath}");
                return false;
            }

            try
            {
                string content = File.ReadAllText(sourcePath);

                File.WriteAllText(targetPath, content);

                Debug.Log($"替换完毕 {targetPath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"覆盖文件错误: {ex.Message}");
            }

            return false;
        }
    }
}

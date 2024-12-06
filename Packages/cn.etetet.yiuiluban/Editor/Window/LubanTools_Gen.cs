using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace YIUI.Luban.Editor
{
    public partial class LubanTools
    {
        [MenuItem("ET/Excel/ExcelExporter")]
        public static void MenuLubanGen()
        {
            if (!CreateLubanConf()) return;

            RunLubanGen();
        }

        public static void LubanGen()
        {
            if (!CreateLubanConf()) return;

            RunLubanGen(true);
        }

        private class SchemaFile
        {
            public string fileName { get; set; }
            public string type     { get; set; }
        }

        private static bool CreateLubanConf()
        {
            List<SchemaFile> AllSchemaFile = new();
            foreach (string directory in Directory.GetDirectories("Packages", "cn.etetet.*"))
            {
                var tablesPath = Path.Combine(directory, "Assets/Editor/Luban/Base/__tables__.xlsx");
                if (File.Exists(tablesPath))
                {
                    AllSchemaFile.Add(new() { fileName = $"../../../../../../{tablesPath}", type = "table" });
                }

                var beansPath = Path.Combine(directory, "Assets/Editor/Luban/Base/__beans__.xlsx");
                if (File.Exists(beansPath))
                {
                    AllSchemaFile.Add(new() { fileName = $"../../../../../../{beansPath}", type = "bean" });
                }

                var enumsPath = Path.Combine(directory, "Assets/Editor/Luban/Base/__enums__.xlsx");
                if (File.Exists(enumsPath))
                {
                    AllSchemaFile.Add(new() { fileName = $"../../../../../../{enumsPath}", type = "enum" });
                }

                var definesPath = Path.Combine(directory, "Assets/Editor/Luban/Base/Defines");
                if (Directory.Exists(definesPath))
                {
                    AllSchemaFile.Add(new() { fileName = $"../../../../../../{definesPath}", type = "" });
                }
            }

            var packagePath = $"{Application.dataPath}/../Packages/cn.etetet.yiuilubangen/Assets/Editor/Luban/Base/luban.conf";
            if (!File.Exists(packagePath))
            {
                Debug.LogError($"源文件 luban.conf 不存在 请重新初始化LubanGen");
                return false;
            }

            try
            {
                string fileContent = File.ReadAllText(packagePath);

                JObject json = JObject.Parse(fileContent);

                JArray schemaFilesArray = JArray.FromObject(AllSchemaFile);

                json["schemaFiles"] = schemaFilesArray;

                string updatedJsonContent = json.ToString(Newtonsoft.Json.Formatting.Indented);

                File.WriteAllText(packagePath, updatedJsonContent, Encoding.UTF8);

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"创建 LubanConf失败 {e.Message}");
                return false;
            }
        }

        static void ConvertLineEndings(string filePath, string newLine)
        {
            string fileContent    = File.ReadAllText(filePath);
            string updatedContent = fileContent.Replace("\r\n", "\n").Replace("\n", newLine);
            File.WriteAllText(filePath, updatedContent);
        }

        private static bool m_UsePs = false;

        private static void RunLubanGen(bool tips = false)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (m_UsePs)
                {
                    RunProcess("powershell.exe", $"-NoExit -ExecutionPolicy Bypass -File ./Packages/cn.etetet.yiuiluban/.ToolsGen/LubanGen.ps1", tips);
                }
                else
                {
                    var path = $"{Application.dataPath}/../Packages/cn.etetet.yiuiluban/.ToolsGen/LubanGen.bat";
                    ConvertLineEndings(path, "\r\n");
                    RunProcess(path, "", tips);
                }
            }
            else
            {
                if (m_UsePs)
                {
                    RunProcess("/usr/local/bin/pwsh", $"-NoExit -ExecutionPolicy Bypass -File ./Packages/cn.etetet.yiuiluban/.ToolsGen/LubanGen.ps1", tips);
                }
                else
                {
                    var path = $"./Packages/cn.etetet.yiuiluban/.ToolsGen/LubanGen.sh";
                    ChangePermissions($"{Application.dataPath}/../{path}", "755");
                    RunProcess("/bin/bash", $"-c \"{path}\"", tips);
                }
            }
        }

        public static void ChangePermissions(string filePath, string permissions)
        {
            try
            {
                ProcessStartInfo processInfo = new ProcessStartInfo
                {
                    FileName               = "/bin/chmod",
                    Arguments              = $"{permissions} \"{filePath}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true
                };

                using (Process process = Process.Start(processInfo))
                {
                    process.WaitForExit();
                    string output = process.StandardOutput.ReadToEnd();

                    // You can handle the output if needed  
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to change file permissions: {e.Message}");
            }
        }

        private static void RunProcess(string exe, string arguments, bool tips = false, string workingDirectory = ".", bool waitExit = true)
        {
            var redirectStandardOutput = false;
            var redirectStandardError  = false;
            var useShellExecute        = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            if (waitExit)
            {
                redirectStandardOutput = true;
                redirectStandardError  = true;
                useShellExecute        = false;
            }

            var ImportAllOutput = new StringBuilder();
            var ImportAllError  = new StringBuilder();
            var importProcess   = new Process();
            importProcess.StartInfo.FileName               = exe;
            importProcess.StartInfo.Arguments              = arguments;
            importProcess.StartInfo.WorkingDirectory       = workingDirectory;
            importProcess.StartInfo.UseShellExecute        = useShellExecute;
            importProcess.StartInfo.CreateNoWindow         = true;
            importProcess.StartInfo.RedirectStandardOutput = redirectStandardOutput;
            importProcess.StartInfo.RedirectStandardError  = redirectStandardError;
            importProcess.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            importProcess.StartInfo.StandardErrorEncoding  = Encoding.UTF8;
            importProcess.OutputDataReceived += (_, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    if (args.Data.Contains("ERROR"))
                    {
                        ImportAllError.AppendLine(args.Data);
                    }

                    ImportAllOutput.AppendLine(args.Data);
                }
            };
            importProcess.ErrorDataReceived += (_, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    ImportAllError.AppendLine(args.Data);
                }
            };
            EditorUtility.DisplayProgressBar("Luban", $"导出Luban配置中...", 0);

            try
            {
                importProcess.Start();
                importProcess.BeginOutputReadLine();
                importProcess.BeginErrorReadLine();
                importProcess.WaitForExit(20000);

                var output = ImportAllOutput.ToString();
                if (!string.IsNullOrEmpty(output))
                {
                    Debug.Log($"Luban导出日志:\n{output}");
                }

                var error = ImportAllError.ToString();
                if (!string.IsNullOrEmpty(error))
                {
                    if (tips)
                    {
                        UnityTipsHelper.ShowError($"Luban导出错误:\n{error}");
                    }
                    else
                    {
                        Debug.LogError($"Luban导出错误:\n{error}");
                    }

                    CloseWindow();
                }
                else
                {
                    if (tips)
                    {
                        UnityTipsHelper.Show("Luban导出完成");
                    }
                    else
                    {
                        Debug.Log("Luban导出完成");
                    }

                    CloseWindowRefresh();
                }
            }
            catch (Exception e)
            {
                Debug.Log($"Luban导出执行报错: {e.Message}");
            }
            finally
            {
                importProcess.Close();
                EditorUtility.ClearProgressBar();
            }
        }
    }
}
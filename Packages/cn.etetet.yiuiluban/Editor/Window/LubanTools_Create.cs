using System.IO;
using UnityEditor;
using UnityEngine;

namespace YIUI.Luban.Editor
{
    public partial class LubanTools
    {
        public void CreateToPackage(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath)) return;

            if (!Directory.Exists(folderPath))
            {
                UnityTipsHelper.Show($"此文件夹不存在 无法创建:  {folderPath}");
                return;
            }

            var createPackage = GetPackagesName(folderPath);

            if (string.IsNullOrEmpty(createPackage)) return;

            var sourceFolder      = $"{LubanTemplate}/cn.etetet.yiui/Editor/Luban";
            var targetPackagePath = $"{PackagesPath}/{createPackage}/Assets/Editor/Luban";

            if (!Directory.Exists($"{PackagesPath}/{createPackage}"))
            {
                UnityTipsHelper.ShowError($"目标包不存在 无法创建 \n{createPackage}");
                return;
            }

            if (!CopyFolder.Copy(sourceFolder, targetPackagePath)) return;
            CreateNullDirectory($"{targetPackagePath}/Datas");
            CreateNullDirectory($"{targetPackagePath}/Base/Defines");

            CloseWindowRefresh?.Invoke();
            UnityTipsHelper.Show($"{createPackage} Luban创建完毕");
            UnityTipsHelper.SelectLubanFolder(createPackage);
        }

        private void CreateNullDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private static string GetPackagesName(string folderPath)
        {
            var basePath = $"{Application.dataPath.Replace("Assets", "Packages")}/";
            if (!folderPath.Contains(basePath))
            {
                UnityTipsHelper.Show($"不是Packages目录 无法创建:  {folderPath}");
                return "";
            }

            var replacePath = folderPath.Replace(basePath, "");

            var splitValue = replacePath.Split("/");
            if (splitValue is { Length: >= 1 })
            {
                var packageName = splitValue[0];
                if (packageName.StartsWith("cn.etetet."))
                {
                    return packageName;
                }
            }

            UnityTipsHelper.Show($"没有找到ET包 无法创建 {replacePath}");
            return "";
        }
    }
}
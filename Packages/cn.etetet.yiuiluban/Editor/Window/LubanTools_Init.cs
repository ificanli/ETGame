using System.IO;
using UnityEngine;

namespace YIUI.Luban.Editor
{
    public partial class LubanTools
    {
        private static readonly string PackagesPath            = $"{Application.dataPath}/../Packages";
        private static readonly string LubanTemplate           = $"{PackagesPath}/cn.etetet.yiuiluban/.Template";
        private static readonly string yiuilubangenName        = "cn.etetet.yiuilubangen";
        private static readonly string yiuilubangenPackagePath = $"{PackagesPath}/{yiuilubangenName}";

        private bool GenPackageExists()
        {
            return Directory.Exists(yiuilubangenPackagePath);
        }

        private void InitGen()
        {
            if (!ReplaceAll()) return;

            var sourceFolder = $"{LubanTemplate}/{yiuilubangenName}";
            var targetFolder = yiuilubangenPackagePath;
            if (!CopyFolder.Copy(sourceFolder, targetFolder)) return;
            CreateNullDirectory($"{targetFolder}/Assets/Editor/Luban/Datas");
            CreateNullDirectory($"{targetFolder}/Assets/Editor/Luban/Base/Defines");

            CloseWindowRefresh();
            UnityTipsHelper.Show("LubanGen 创建完毕");
            UnityTipsHelper.SelectLubanFolder(yiuilubangenName);
            RunLubanGen();
        }
    }
}
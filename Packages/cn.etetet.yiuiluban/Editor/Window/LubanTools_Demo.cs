using System.IO;
using UnityEditor;
using UnityEngine;

namespace YIUI.Luban.Editor
{
    public partial class LubanTools
    {
        private static readonly string yiuilubandemoName        = "cn.etetet.yiuilubandemo";
        private static readonly string yiuilubandemoPackagePath = $"{Application.dataPath}/../Packages/{yiuilubandemoName}";

        public bool DemoPackageExists()
        {
            return Directory.Exists(yiuilubandemoPackagePath);
        }

        public void CreateLubanDemoPackage()
        {
            var sourceFolder = $"{LubanTemplate}/{yiuilubandemoName}";
            var targetFolder = yiuilubandemoPackagePath;
            if (!CopyFolder.Copy(sourceFolder, targetFolder)) return;
            CreateNullDirectory($"{targetFolder}/Assets/Editor/Luban/Datas");
            CreateNullDirectory($"{targetFolder}/Assets/Editor/Luban/Base/Defines");

            CloseWindowRefresh?.Invoke();
            UnityTipsHelper.Show("LubanDemo 创建完毕");
            UnityTipsHelper.SelectLubanFolder(yiuilubandemoName);
            MenuLubanGen();
        }

        public void DeleteLubanDemoPackage()
        {
            if (DemoPackageExists())
            {
                Directory.Delete(yiuilubandemoPackagePath, true);
                UnityTipsHelper.Show("LubanDemo 删除完毕");
                CloseWindowRefresh?.Invoke();
            }
        }
    }
}
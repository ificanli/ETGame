using UnityEditor;
using System.Diagnostics;

namespace ET
{
    public static class ProtoEditor
    {
        [MenuItem("ET/Proto/Proto2CS")]
        public static void Run()
        {
            ProcessHelper.DotNet("./Packages/cn.etetet.proto/DotNet~/Exe/ET.Proto2CS.dll", "./", true);
        }
        
        public static void Init()
        {
            Run();
        }
    }
}
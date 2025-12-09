using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace ET
{
    public class CodeLoader: Singleton<CodeLoader>, ISingletonAwake
    {
        private Assembly modelAssembly;
        private Assembly modelViewAssembly;

        private Dictionary<string, TextAsset> dlls;
        private Dictionary<string, TextAsset> aotDlls;

        public void Awake()
        {
        }

        private async ETTask DownloadAsync()
        {
#if UNITY_EDITOR
            await ETTask.CompletedTask;
#else
            this.dlls = await ResourcesComponent.Instance.LoadAllAssetsAsync<TextAsset>($"ET.Model.dll");
            this.aotDlls = await ResourcesComponent.Instance.LoadAllAssetsAsync<TextAsset>($"mscorlib.dll");
#endif
        }

        public async ETTask Start()
        {
            await DownloadAsync();
            
#if UNITY_EDITOR
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                string name = assembly.GetName().Name;
                if (name == "ET.Model")
                {
                    this.modelAssembly = assembly;
                    continue;
                }
                if (name == "ET.ModelView")
                {
                    this.modelViewAssembly = assembly;
                    continue;
                }
                if (this.modelAssembly != null && this.modelViewAssembly != null)
                {
                    break;
                }
            }
#else
            byte[] modelAssBytes = this.dlls["ET.Model.dll"].bytes;
            byte[] modelPdbBytes = this.dlls["ET.Model.pdb"].bytes;
            byte[] modelViewAssBytes = this.dlls["ET.ModelView.dll"].bytes;
            byte[] modelViewPdbBytes = this.dlls["ET.ModelView.pdb"].bytes;
            // 如果需要测试，可替换成下面注释的代码直接加载Packages/cn.etetet.loader/Code/ET.Model.dll.bytes，但真正打包时必须使用上面的代码
            //byte[] modelAssBytes = File.ReadAllBytes(Path.Combine(Define.CodeDir, "ET.Model.dll.bytes"));
            //byte[] modelPdbBytes = File.ReadAllBytes(Path.Combine(Define.CodeDir, "ET.Model.pdb.bytes"));
            //byte[] modelViewAssBytes = File.ReadAllBytes(Path.Combine(Define.CodeDir, "ET.ModelView.dll.bytes"));
            //byte[] modelViewPdbBytes = File.ReadAllBytes(Path.Combine(Define.CodeDir, "ET.ModelView.pdb.bytes"));

            foreach (var kv in this.aotDlls)
            {
                TextAsset textAsset = kv.Value;
                HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly(textAsset.bytes, HybridCLR.HomologousImageMode.SuperSet);
            }
            
            this.modelAssembly = Assembly.Load(modelAssBytes, modelPdbBytes);
            this.modelViewAssembly = Assembly.Load(modelViewAssBytes, modelViewPdbBytes);
#endif
            
            (Assembly hotfixAssembly, Assembly hotfixViewAssembly) = this.LoadHotfix();

            World.Instance.AddSingleton<CodeTypes, Assembly[]>(new[]
            {
                typeof (World).Assembly, typeof (Init).Assembly, this.modelAssembly, this.modelViewAssembly, hotfixAssembly,
                hotfixViewAssembly
            });

            IStaticMethod start = new StaticMethod(this.modelAssembly, "ET.Entry", "Start");
            start.Run();
        }

        private (Assembly, Assembly) LoadHotfix()
        {
#if UNITY_EDITOR
            byte[] hotfixAssBytes = File.ReadAllBytes(Path.Combine(Define.CodeDir, "ET.Hotfix.dll.bytes"));
            byte[] hotfixPdbBytes = File.ReadAllBytes(Path.Combine(Define.CodeDir, "ET.Hotfix.pdb.bytes"));
            byte[] hotfixViewAssBytes = File.ReadAllBytes(Path.Combine(Define.CodeDir, "ET.HotfixView.dll.bytes"));
            byte[] hotfixViewPdbBytes = File.ReadAllBytes(Path.Combine(Define.CodeDir, "ET.HotfixView.pdb.bytes"));
            Assembly hotfixAssembly = Assembly.Load(hotfixAssBytes, hotfixPdbBytes);
            Assembly hotfixViewAssembly = Assembly.Load(hotfixViewAssBytes, hotfixViewPdbBytes);
#else
            byte[] hotfixAssBytes = this.dlls["ET.Hotfix.dll"].bytes;
            byte[] hotfixPdbBytes = this.dlls["ET.Hotfix.pdb"].bytes;
            byte[] hotfixViewAssBytes = this.dlls["ET.HotfixView.dll"].bytes;
            byte[] hotfixViewPdbBytes = this.dlls["ET.HotfixView.pdb"].bytes;
            // 如果需要测试，可替换成下面注释的代码直接加载Packages/cn.etetet.loader/Code/Hotfix.dll.bytes，但真正打包时必须使用上面的代码
            //hotfixAssBytes = File.ReadAllBytes(Path.Combine(Define.CodeDir, "ET.Hotfix.dll.bytes"));
            //hotfixPdbBytes = File.ReadAllBytes(Path.Combine(Define.CodeDir, "ET.Hotfix.pdb.bytes"));
            //hotfixViewAssBytes = File.ReadAllBytes(Path.Combine(Define.CodeDir, "ET.HotfixView.dll.bytes"));
            //hotfixViewPdbBytes = File.ReadAllBytes(Path.Combine(Define.CodeDir, "ET.HotfixView.pdb.bytes"));
            Assembly hotfixAssembly = Assembly.Load(hotfixAssBytes, hotfixPdbBytes);
            Assembly hotfixViewAssembly = Assembly.Load(hotfixViewAssBytes, hotfixViewPdbBytes);
#endif
            return (hotfixAssembly, hotfixViewAssembly);
        }

        public void Reload()
        {
            (Assembly hotfixAssembly, Assembly hotfixViewAssembly) = this.LoadHotfix();

            CodeTypes codeTypes = World.Instance.AddSingleton<CodeTypes, Assembly[]>(new[]
            {
                typeof (World).Assembly, typeof (Init).Assembly, this.modelAssembly, this.modelViewAssembly, hotfixAssembly,
                hotfixViewAssembly
            });
            codeTypes.CodeProcess();

            Log.Info($"reload dll finish!");
        }
    }
}
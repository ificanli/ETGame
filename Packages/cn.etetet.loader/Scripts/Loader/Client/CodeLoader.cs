using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ET
{
    public class CodeLoader: Singleton<CodeLoader>, ISingletonAwake
    {
        private Dictionary<string, TextAsset> dlls;
        private Dictionary<string, TextAsset> aotDlls;
        private readonly List<Assembly> assemblies = new();

        public void Awake()
        {
        }

        private async ETTask DownloadAsync()
        {
            try
            {
#if UNITY_EDITOR
                await ETTask.CompletedTask;
#else
                this.dlls = await ResourcesComponent.Instance.LoadAllAssetsAsync<TextAsset>($"ET.Model.dll");
                this.aotDlls = await ResourcesComponent.Instance.LoadAllAssetsAsync<TextAsset>($"mscorlib.dll");
#endif
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        public async ETTask Start()
        {
            await DownloadAsync();
            
            
            HashSet<string> assemblyNames = new()
            {
                "ET.Core",
                "ET.Loader",
#if UNITY_EDITOR     
                "ET.Model",
                "ET.ModelView",
                "ET.BehaviorTree.Editor",
#endif
            };

            Assembly[] domainAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in domainAssemblies)
            {
                string name = assembly.GetName().Name;
                if (assemblyNames.Contains(name))
                {
                    assemblies.Add(assembly);
                }
            }
            List<Assembly> list = new(this.assemblies);
            
#if !UNITY_EDITOR
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
            
            Assembly modelAssembly = Assembly.Load(modelAssBytes, modelPdbBytes);
            Assembly modelViewAssembly = Assembly.Load(modelViewAssBytes, modelViewPdbBytes);
            list.Add(modelAssembly);
            list.Add(modelViewAssembly);
#endif
            
            (Assembly hotfixAssembly, Assembly hotfixViewAssembly) = this.LoadHotfix();
            
            list.Add(hotfixViewAssembly);
            list.Add(hotfixAssembly);
            World.Instance.AddSingleton<CodeTypes, Assembly[]>(list.ToArray());
            IStaticMethod start = new StaticMethod(hotfixAssembly, "ET.Entry", "Start");
            start.Run();
        }

        private (Assembly, Assembly) LoadHotfix(bool isReload = false)
        {
#if UNITY_EDITOR
            Assembly hotfixAssembly = null;
            Assembly hotfixViewAssembly = null;
            
            if (isReload)
            {
                byte[] hotfixAssBytes = File.ReadAllBytes(Path.Combine(Define.CodeDir, "ET.Hotfix.dll.bytes"));
                byte[] hotfixPdbBytes = File.ReadAllBytes(Path.Combine(Define.CodeDir, "ET.Hotfix.pdb.bytes"));
                byte[] hotfixViewAssBytes = File.ReadAllBytes(Path.Combine(Define.CodeDir, "ET.HotfixView.dll.bytes"));
                byte[] hotfixViewPdbBytes = File.ReadAllBytes(Path.Combine(Define.CodeDir, "ET.HotfixView.pdb.bytes"));
                hotfixAssembly = Assembly.Load(hotfixAssBytes, hotfixPdbBytes);
                hotfixViewAssembly = Assembly.Load(hotfixViewAssBytes, hotfixViewPdbBytes);
            }
            else
            {
                Assembly[] domainAssemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (Assembly assembly in domainAssemblies)
                {
                    string name = assembly.GetName().Name;
                    if (name == "ET.Hotfix")
                    {
                        hotfixAssembly = assembly;
                        continue;
                    }
                    if (name == "ET.HotfixView")
                    {
                        hotfixViewAssembly = assembly;
                        continue;
                    }
                    if (hotfixAssembly != null && hotfixViewAssembly != null)
                    {
                        break;
                    }
                }
            }

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
            (Assembly hotfixAssembly, Assembly hotfixViewAssembly) = this.LoadHotfix(true);
            List<Assembly> list = new(this.assemblies);
            list.Add(hotfixViewAssembly);
            list.Add(hotfixAssembly);
            CodeTypes codeTypes = World.Instance.AddSingleton<CodeTypes, Assembly[]>(list.ToArray());
            codeTypes.CodeProcess();

            Log.Info($"reload dll finish!");
        }
    }
}
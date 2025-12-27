using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace ET
{
    public class CodeLoader: Singleton<CodeLoader>, ISingletonAwake
    {
        private AssemblyLoadContext assemblyLoadContext;

        private readonly List<Assembly> assemblies = new();

        public void Awake()
        {
        }

        public void Start()
        {
            HashSet<string> assemblyNames = new()
            {
                "ET.Core",
                "ET.Loader",
                "ET.Model",
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

            Assembly hotfixAssembly = this.LoadHotfix();

            List<Assembly> list = new(this.assemblies);
            list.Add(hotfixAssembly);
            World.Instance.AddSingleton<CodeTypes, Assembly[]>(list.ToArray());

            IStaticMethod start = new StaticMethod(hotfixAssembly, "ET.Entry", "Start");
            start.Run();
        }

        private Assembly LoadHotfix()
        {
            assemblyLoadContext?.Unload();
            GC.Collect();
            assemblyLoadContext = new AssemblyLoadContext("ET.Hotfix", true);
            byte[] dllBytes = File.ReadAllBytes("./Bin/ET.Hotfix.dll");
            byte[] pdbBytes = File.ReadAllBytes("./Bin/ET.Hotfix.pdb");
            Assembly hotfixAssembly = assemblyLoadContext.LoadFromStream(new MemoryStream(dllBytes), new MemoryStream(pdbBytes));
            return hotfixAssembly;
        }
        
        public void Reload()
        {
            Assembly hotfixAssembly = this.LoadHotfix();
			
            List<Assembly> list = new(this.assemblies);
            list.Add(hotfixAssembly);
            CodeTypes codeTypes = World.Instance.AddSingleton<CodeTypes, Assembly[]>(list.ToArray());

            codeTypes.CodeProcess();
            Log.Debug($"reload dll finish!");
        }
    }
}
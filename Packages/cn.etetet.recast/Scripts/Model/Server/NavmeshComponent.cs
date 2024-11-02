using System;
using System.Collections.Generic;
using System.IO;
using DotRecast.Detour;
using DotRecast.Detour.Io;

namespace ET
{
    public class NavmeshComponent: Singleton<NavmeshComponent>, ISingletonAwake
    {
        public struct RecastFileLoader
        {
            public string Name { get; set; }
        }

        private readonly Dictionary<string, DtNavMesh> navmeshs = new(); 
        
        public void Awake()
        {
        }
        
        public DtNavMesh Get(string name)
        {
            lock (this)
            {
                if (this.navmeshs.TryGetValue(name, out DtNavMesh dtNavMesh))
                {
                    return dtNavMesh;
                }

                byte[] buffer =
                        EventSystem.Instance.Invoke<NavmeshComponent.RecastFileLoader, byte[]>(
                            new NavmeshComponent.RecastFileLoader() { Name = name });
                if (buffer.Length == 0)
                {
                    throw new Exception($"no nav data: {name}");
                }
                
                DtMeshSetReader reader = new();
                using MemoryStream ms = new(buffer);
                using BinaryReader br = new(ms);
                DtNavMesh navMesh = reader.Read(br, 6); // cpp recast导出来的要用Read32Bit读取，DotRecast导出来的还没试过
                this.navmeshs.Add(name, navMesh);
                return navMesh;
            }
        }
    }
}
using System;
using System.Collections.Concurrent;
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

        private readonly ConcurrentDictionary<string, DtNavMesh> navmeshs = new(); 
        
        public void Awake()
        {
        }

        public async ETTask Load(string name)
        {
            if (this.navmeshs.ContainsKey(name))
            {
                return;
            }
            
            byte[] buffer =
                    await EventSystem.Instance.Invoke<RecastFileLoader, ETTask<byte[]>>(
                        new RecastFileLoader() { Name = name });
            
            if (buffer.Length == 0)
            {
                throw new Exception($"no nav data: {name}");
            }
            
            DtMeshSetReader reader = new();
            using MemoryStream ms = new(buffer);
            using BinaryReader br = new(ms);
            DtNavMesh navMesh = reader.Read(br, 6); // cpp recast导出来的要用Read32Bit读取，DotRecast导出来的还没试过
            this.navmeshs.TryAdd(name, navMesh);
        }
        
        public DtNavMesh Get(string name)
        {
            return this.navmeshs[name];
        }
    }
}
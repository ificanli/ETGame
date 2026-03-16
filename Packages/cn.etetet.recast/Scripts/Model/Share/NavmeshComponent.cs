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
        private readonly ConcurrentDictionary<string, byte[]> navmeshBuffers = new();
        
        public void Awake()
        {
        }

        public async ETTask Load(string name)
        {
            if (this.navmeshs.ContainsKey(name) && this.navmeshBuffers.ContainsKey(name))
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

            this.navmeshBuffers.TryAdd(name, buffer);
            this.navmeshs.TryAdd(name, this.ReadNavMesh(buffer, name));
        }
        
        public DtNavMesh Get(string name)
        {
            return this.navmeshs[name];
        }

        public DtNavMesh CreateInstance(string name)
        {
            if (!this.navmeshBuffers.TryGetValue(name, out byte[] buffer) || buffer == null || buffer.Length == 0)
            {
                throw new Exception($"nav buffer not loaded: {name}");
            }

            return this.ReadNavMesh(buffer, name);
        }

        private DtNavMesh ReadNavMesh(byte[] buffer, string name)
        {
            DtMeshSetReader reader = new();
            DtNavMesh navMesh = null;
            Exception readException = null;

            try
            {
                using MemoryStream ms = new(buffer);
                using BinaryReader br = new(ms);
                navMesh = reader.Read(br, 6);
            }
            catch (Exception e)
            {
                readException = e;
            }

            if (navMesh != null)
            {
                return navMesh;
            }

            try
            {
                using MemoryStream ms = new(buffer);
                using BinaryReader br = new(ms);
                navMesh = reader.Read32Bit(br, 6);
                Log.Warning($"[Navmesh] fallback to Read32Bit succeeded: {name}");
                return navMesh;
            }
            catch (Exception fallbackException)
            {
                throw new Exception($"navmesh read failed: {name}", new AggregateException(readException, fallbackException));
            }
        }
    }
}

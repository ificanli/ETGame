using System;
using System.Collections.Generic;
using System.Linq;

namespace ET.Server
{
    [EntitySystemOf(typeof(MapCopy))]
    public static partial class MapCopySystem
    {
        [EntitySystem]
        private static void Destroy(this MapCopy self)
        {
            self.Fiber().RemoveFiber(self.FiberId).NoContext();
        }
        [EntitySystem]
        private static void Awake(this MapCopy self, int fiberId)
        {
            self.FiberId = fiberId;
        }

        public static void AddPlayer(this MapCopy self, long playerId)
        {
            if (!self.WaitEnterPlayer.Remove(playerId))
            {
                throw new Exception($"player not in wait list: {playerId}");
            }
            self.Players.Add(playerId);
        }

        public static void AddWaitPlayer(this MapCopy self, long playerId)
        {
            self.WaitEnterPlayer.Add(playerId, TimeInfo.Instance.ServerNow());
        }
    }

    [EntitySystemOf(typeof(MapInfo))]
    public static partial class MapInfoSystem
    {
        [EntitySystem]
        private static void Awake(this MapInfo self, string mapName)
        {
            self.MapName = mapName;
        }

        private static long GetNotUsedLineNumber(this MapInfo self)
        {
            long lineNum = 1;
            foreach (long k  in self.Children.Keys)
            {
                if (k == lineNum)
                {
                    lineNum++;
                    continue;
                }
                break;
            }
            return lineNum;
        }
        
        public static async ETTask RemoveCopy(this MapInfo self, long id)
        {
            EntityRef<MapInfo> selfRef = self;
            await self.Fiber().RemoveFiber((int)id);
            self = selfRef;
            MapCopy mapCopy = self.GetChild<MapCopy>(id);
            
            Log.Debug($"remove map copy: {self.MapName}:{mapCopy.Id}");
            
            self.RemoveChild(id);
        }

        public static async ETTask<MapCopy> GetCopy(this MapInfo self, long id = 0)
        {
            MapConfig mapConfig = MapConfigCategory.Instance.GetByName(self.MapName);
            string mapName = self.MapName;
            MapCopy mapCopy = null;
            if (id != 0)
            {
                mapCopy = self.GetChild<MapCopy>(id);
                if (mapCopy != null)
                {
                    Log.Debug($"get map copy: {mapName}:{id}");
                    return mapCopy;
                }

                return null;
            }
            
            foreach (var kv in self.Children)
            {
                MapCopy copy = (MapCopy)kv.Value;
                if (copy.Status != MapCopyStatus.Running)
                {
                    continue;
                }
                if (copy.Players.Count < mapConfig.RecommendPlayerNum)
                {
                    Log.Debug($"get map copy: {mapName}:{copy.Id}:");
                    return copy;
                }
            }

            long mapId = 0;
            switch (mapConfig.CopyType)
            {
                case CopyType.Normal:
                {
                    mapId = IdGenerater.Instance.GenerateId();
                    break;
                }
                case CopyType.Line:
                {
                    mapId = self.GetNotUsedLineNumber();
                    break;
                }
                case CopyType.Copy:
                {
                    mapId = IdGenerater.Instance.GenerateId();
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            mapCopy = await self.AddChildWithIdAsync(mapId);
            
            Log.Debug($"get map copy: {mapName}:{mapId}");
            
            return mapCopy;
        }
        
        private static async ETTask<MapCopy> AddChildWithIdAsync(this MapInfo self, long id)
        {
            EntityRef<MapInfo> selfRef = self;
            
            // 创建Copy Fiber
            int fiberId = await self.Fiber().CreateFiber(SchedulerType.ThreadPool, id, self.Zone(), SceneType.Map, $"{self.MapName}@{id}");
            self = selfRef;
            return self.AddChildWithId<MapCopy, int>(id, fiberId);
        }
    }
    
    
    
    [EntitySystemOf(typeof(MapManagerComponent))]
    public static partial class MapManagerComponentSystem
    {
        [EntitySystem]
        private static void Awake(this MapManagerComponent self)
        {

        }

        private static MapInfo AddMap(this MapManagerComponent self, string mapName)
        {
            MapConfig mapConfig = MapConfigCategory.Instance.GetByName(mapName);
            MapInfo mapInfo = self.AddChild<MapInfo, string>(mapName);
            if (mapConfig.CopyType == CopyType.Line)
            {
                mapInfo.AddComponent<MergeLinesComponent>();
            }
            self.MapInfos.Add(mapName, mapInfo);
            return mapInfo;
        }

        public static async ETTask<MapCopy> GetMapAsync(this MapManagerComponent self, string mapName, long id = 0)
        {
            if (id != 0)
            {
                MapCopy mapCopy = self.GetMap(mapName, id);
                if (mapCopy != null)
                {
                    return mapCopy;
                }
            }

            MapInfo mapInfo = null;
            if (!self.MapInfos.TryGetValue(mapName, out var mapInfoRef))
            {
                mapInfo = self.AddMap(mapName);
            }
            else
            {
                mapInfo = mapInfoRef;
            }

            return await mapInfo.GetCopy(id);
        }
        
        public static MapCopy GetMap(this MapManagerComponent self, string mapName, long mapId)
        {
            if (!self.MapInfos.TryGetValue(mapName, out EntityRef<MapInfo> mapInfoRef))
            {
                return null;
            }

            MapInfo mapInfo = mapInfoRef;
            MapCopy mapCopy = mapInfo.GetChild<MapCopy>(mapId);
            return mapCopy;
        }
    }
}
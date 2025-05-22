using System;
using System.Collections.Generic;

namespace ET.Server
{
    [EntitySystemOf(typeof(MapCopy))]
    public static partial class MapCopySystem
    {
        [EntitySystem]
        private static void Awake(this MapCopy self, int lineNum)
        {
            self.LineNum = lineNum;
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
            self.WaitEnterPlayer.Add(playerId, TimeInfo.Instance.FrameTime);
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

        private static int GetNotUsedLineNumber(this MapInfo self)
        {
            int lineNum = 1;
            foreach (var kv  in self.Lines)
            {
                if (kv.Key == lineNum)
                {
                    lineNum++;
                    continue;
                }
                break;
            }
            return lineNum;
        }
        
        public static MapCopy GetByLineNum(this MapInfo self, int lineNum)
        {
            if (!self.Lines.TryGetValue(lineNum, out var mapCopyId))
            {
                throw new Exception($"line num not found: {self.MapName} {lineNum}");
            }
            return self.GetChild<MapCopy>(mapCopyId);
        }

        public static MapCopy GetCopy(this MapInfo self, long id)
        {
            return self.GetChild<MapCopy>(id);
        }

        public static async ETTask<MapCopy> GetCopy(this MapInfo self)
        {
            MapConfig mapConfig = MapConfigCategory.Instance.GetByName(self.MapName);
            
            foreach (var kv in self.Children)
            {
                MapCopy copy = (MapCopy)kv.Value;
                if (copy.Status != MapCopyStatus.Running)
                {
                    continue;
                }
                if (copy.Players.Count < mapConfig.RecommendPlayerNum)
                {
                    return copy;
                }
            }
            
            // 创建Copy Fiber
            long mapCopyId = await FiberManager.Instance.Create(SchedulerType.ThreadPool, self.Zone(), SceneType.Map, self.MapName);

            int lineNum = self.GetNotUsedLineNumber();
            self.Lines[lineNum] = mapCopyId;
            MapCopy mapCopy = self.AddChildWithId<MapCopy, int>(mapCopyId, lineNum);
            return mapCopy;
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
            MapInfo mapInfo = self.AddChild<MapInfo, string>(mapName);
            self.MapInfos.Add(mapName, mapInfo);
            return mapInfo;
        }

        public static async ETTask<MapCopy> GetMap(this MapManagerComponent self, string mapName)
        {
            MapInfo mapInfo = null;
            if (!self.MapInfos.TryGetValue(mapName, out var mapInfoRef))
            {
                mapInfo = self.AddMap(mapName);
            }
            else
            {
                mapInfo = mapInfoRef;
            }

            return await mapInfo.GetCopy();
        }
    }
}
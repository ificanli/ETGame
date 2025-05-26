using System;
using System.Collections.Generic;
using System.Linq;

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
        
        public static async ETTask RemoveCopy(this MapInfo self, long id)
        {
            await FiberManager.Instance.Remove((int)id);
            
            MapCopy mapCopy = self.GetChild<MapCopy>(id);
            
            Log.Debug($"remove map copy: {self.MapName}:{mapCopy.LineNum}:{mapCopy.Id}");
            
            self.Lines.Remove(mapCopy.LineNum);
            self.RemoveChild(id);
        }

        // line2合并到line1
        public static async ETTask MergeLines(this MapInfo self, int lineNum1, int lineNum2)
        {
            MapCopy mapCopy1 = self.GetByLineNum(lineNum1);
            MapCopy mapCopy2 = self.GetByLineNum(lineNum2);

            if (mapCopy1.Status != MapCopyStatus.WaitMerge || mapCopy2.Status != MapCopyStatus.WaitMerge)
            {
                throw new Exception($"map copy not running: {self.MapName} {lineNum1} {lineNum2}");
            }
            
            MessageLocationSenderComponent messageLocationSender = self.Root().GetComponent<MessageLocationSenderComponent>();
            MessageLocationSenderOneType messageLocationSenderOneType = messageLocationSender.Get(LocationType.Unit);
            
            // 通知传送
            foreach (long playerId in mapCopy2.Players.ToArray())
            {
                MapManager2Map_NotifyPlayerTransferRequest request = MapManager2Map_NotifyPlayerTransferRequest.Create();
                await messageLocationSenderOneType.Call(playerId, request);

                mapCopy1.Players.Add(playerId);
                mapCopy2.Players.Remove(playerId);
            }

            // 合并等待玩家
            if (mapCopy2.Players.Count > 0)
            {
                Log.Error($"合并副本时，mapCopy2有玩家未传送: {mapCopy2.Players.ToList().ListToString()}");
            }

            // 设置状态
            mapCopy1.Status = MapCopyStatus.Running;
            mapCopy1.MergeTime = TimeInfo.Instance.FrameTime;

            // 删除副本2
            await self.RemoveCopy(mapCopy2.Id);
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
            MapCopy mapCopy = self.AddChildWithId<MapCopy, int>(mapCopyId, lineNum);
            self.Lines[lineNum] = mapCopyId;
            
            Log.Debug($"create map copy: {self.MapName}:{lineNum}:{mapCopyId}");
            
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
            MapConfig mapConfig = MapConfigCategory.Instance.GetByName(mapName);
            MapInfo mapInfo = self.AddChild<MapInfo, string>(mapName);
            if (mapConfig.CopyType == CopyType.Line)
            {
                mapInfo.AddComponent<MergeLinesComponent>();
            }
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
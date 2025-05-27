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
            self.Line = lineNum;
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
            
            Log.Debug($"remove map copy: {self.MapName}:{mapCopy.Line}:{mapCopy.Id}");
            
            self.Lines.Remove(mapCopy.Line);
            self.RemoveChild(id);
        }

        // line2合并到line1
        public static async ETTask MergeLines(this MapInfo self, int lineNum1, int lineNum2)
        {
            Log.Debug($"start merge lines: {self.MapName} {lineNum1} {lineNum2}");
            
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
                request.MapName = self.MapName;
                request.Line = mapCopy1.Line;
                await messageLocationSenderOneType.Call(playerId, request);

                Log.Debug($"merge lines transfer: {self.MapName} transfer {playerId} to line {lineNum1}");
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

        public static async ETTask<MapCopy> GetCopy(this MapInfo self, int line = 0)
        {
            MapConfig mapConfig = MapConfigCategory.Instance.GetByName(self.MapName);

            long mapCopyId = 0;
            MapCopy mapCopy = null;
            if (line != 0)
            {
                self.Lines.TryGetValue(line, out mapCopyId);
                mapCopy = self.GetChild<MapCopy>(mapCopyId);
                Log.Debug($"get map copy: {self.MapName}:{line}:{mapCopyId}");
                return mapCopy;
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
                    Log.Debug($"get map copy: {self.MapName}:{copy.Line}:{mapCopyId}");
                    return copy;
                }
            }
            
            int lineNum = self.GetNotUsedLineNumber();
            // 创建Copy Fiber
            mapCopyId = await FiberManager.Instance.Create(SchedulerType.ThreadPool, self.Zone(), SceneType.Map, $"{self.MapName}@{lineNum}");
            mapCopy = self.AddChildWithId<MapCopy, int>(mapCopyId, lineNum);
            self.Lines[lineNum] = mapCopyId;
            
            Log.Debug($"get map copy: {self.MapName}:{lineNum}:{mapCopyId}");
            
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

        public static async ETTask<MapCopy> GetMap(this MapManagerComponent self, string mapName, int line)
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

            return await mapInfo.GetCopy(line);
        }
        
        public static MapCopy FindMap(this MapManagerComponent self, string mapName, long mapId)
        {
            if (!self.MapInfos.TryGetValue(mapName, out EntityRef<MapInfo> mapInfoRef))
            {
                return null;
            }

            MapInfo mapInfo = mapInfoRef;
            MapCopy mapCopy = mapInfo.GetCopy(mapId);
            return mapCopy;
        }
    }
}
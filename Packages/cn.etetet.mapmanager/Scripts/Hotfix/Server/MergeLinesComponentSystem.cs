using System;
using System.Collections.Generic;
using System.Linq;

namespace ET.Server
{
    [EntitySystemOf(typeof(MergeLinesComponent))]
    public static partial class MergeLinesComponentSystem
    {
        [EntitySystem]
        private static void Awake(this MergeLinesComponent self)
        {
            self.StartCheckAsync().NoContext();
            self.StartTransferAsync().NoContext();
        }
        
        [EntitySystem]
        private static void Destroy(this MergeLinesComponent self)
        {
        }
        
        private static async ETTask StartTransferAsync(this MergeLinesComponent self)
        {
            EntityRef<MergeLinesComponent> selfRef = self;
            TimerComponent timer = self.Root().GetComponent<TimerComponent>();
            while (true)
            {
                await timer.WaitAsync(100);
                self = selfRef;
                if (self == null)
                {
                    return;
                }
                await self.TransferAsync();
            }
        }

        private static async ETTask TransferAsync(this MergeLinesComponent self)
        {
            MapInfo mapInfo = self.GetParent<MapInfo>();
            EntityRef<MapInfo> mapInfoRef = mapInfo;
            EntityRef<MergeLinesComponent> selfRef = self;
            // 如果有合线
            while (self.WaitMergetQueue.Count > 0)
            {
                MergeLineInfo mergeLineInfo = self.WaitMergetQueue.Peek();
                if (TimeInfo.Instance.FrameTime - mergeLineInfo.Time > 1)
                {
                    self.WaitMergetQueue.Dequeue();
                    
                    // 合线，把mapCopy2合并到mapCopy1
                    await self.MergeLines(mergeLineInfo.LineNum1, mergeLineInfo.LineNum2);
                    self = selfRef;
                    mapInfo = mapInfoRef;
                    Log.Info($"merge line ok, {mapInfo.MapName} {mergeLineInfo.LineNum1} {mergeLineInfo.LineNum2}");
                }
                else
                {
                    break;
                }
            }
        }
        
        // line2合并到line1
        public static async ETTask MergeLines(this MergeLinesComponent self, long line1, long line2)
        {
            MapInfo mapInfo = self.GetParent<MapInfo>();
            Log.Debug($"start merge lines: {mapInfo.MapName} {line1} {line2}");
         
            EntityRef<MapInfo> mapInfoRef = mapInfo;
            MapCopy mapCopy1 = mapInfo.GetChild<MapCopy>(line1);
            MapCopy mapCopy2 = mapInfo.GetChild<MapCopy>(line2);
            EntityRef<MapCopy> mapCopy1Ref = mapCopy1;

            if (mapCopy1.Status != MapCopyStatus.WaitMerge || mapCopy2.Status != MapCopyStatus.WaitMerge)
            {
                throw new Exception($"map copy not running: {mapInfo.MapName} {line1} {line2}");
            }
            
            MessageLocationSenderComponent messageLocationSender = self.Root().GetComponent<MessageLocationSenderComponent>();
            MessageLocationSenderOneType messageLocationSenderOneType = messageLocationSender.Get(LocationType.Unit);
            EntityRef<MessageLocationSenderOneType> messageLocationSenderOneTypeRef = messageLocationSenderOneType;
            // 通知传送
            foreach (long playerId in mapCopy2.Players.ToArray())
            {
                MapManager2Map_NotifyPlayerTransferRequest request = MapManager2Map_NotifyPlayerTransferRequest.Create();
                mapInfo = mapInfoRef;
                mapCopy1 = mapCopy1Ref;
                request.MapName = mapInfo.MapName;
                request.MapId = mapCopy1.Id;
                messageLocationSenderOneType = messageLocationSenderOneTypeRef;
                await messageLocationSenderOneType.Call(playerId, request);

                mapInfo = mapInfoRef;
                Log.Debug($"merge lines transfer: {mapInfo.MapName} transfer {playerId} to line {line1}");
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
            await mapInfo.RemoveCopy(mapCopy2.Id);
        }
        

        private static async ETTask StartCheckAsync(this MergeLinesComponent self)
        {
            EntityRef<MergeLinesComponent> selfRef = self;
            TimerComponent timer = self.Root().GetComponent<TimerComponent>();
            while (true)
            {
                await timer.WaitAsync(1000);
                self = selfRef;
                if (self == null)
                {
                    return;
                }
                self.CheckAsync();
            }
        }

        private static void CheckAsync(this MergeLinesComponent self)
        {
            MapInfo mapInfo = self.GetParent<MapInfo>();

            MapConfig mapConfig = MapConfigCategory.Instance.GetByName(mapInfo.MapName);
            
            foreach ((long line1, Entity e1) in mapInfo.Children)
            {
                MapCopy mapCopy1 = e1 as MapCopy;
                if (mapCopy1.Status != MapCopyStatus.Running)
                {
                    continue;
                }
                if (mapCopy1.WaitEnterPlayer.Count > 0)
                {
                    // 有等待进入的玩家，不能合线
                    continue;
                }

                if (mapCopy1.Players.Count == 0)
                {
                    continue;
                }
                foreach ((long line2, Entity e2) in mapInfo.Children)
                {
                    if (line2 <= line1)
                    {
                        continue;
                    }
                    MapCopy mapCopy2 = e2 as MapCopy;
                    if (mapCopy2.Status != MapCopyStatus.Running)
                    {
                        continue;
                    }
                    if (mapCopy2.WaitEnterPlayer.Count > 0)
                    {
                        // 有等待进入的玩家，不能合线
                        continue;
                    }
                    
                    if (mapCopy2.Players.Count == 0)
                    {
                        continue;
                    }
                    
                    // 记录下来，可以合线
                    if (mapCopy1.Players.Count + mapCopy2.Players.Count <= mapConfig.RecommendPlayerNum)
                    {
                        mapCopy1.Status = MapCopyStatus.WaitMerge;
                        mapCopy2.Status = MapCopyStatus.WaitMerge;
                        self.WaitMergetQueue.Enqueue(new MergeLineInfo() {LineNum1 = line1, LineNum2 = line2, Time = TimeInfo.Instance.FrameTime});
                        // 通知两个场景的玩家，1分钟后合线
                        Log.Info($"map can merge lines, {mapInfo.MapName} line1: {line1} {mapCopy1.Players.Count}, line2: {line2} {mapCopy2.Players.Count}");
                    }
                }
            }
        }
    }
}
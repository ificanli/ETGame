using System;
using System.Collections.Generic;

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
            // 如果有合线
            while (self.WaitMergetQueue.Count > 0)
            {
                MergeLineInfo mergeLineInfo = self.WaitMergetQueue.Peek();
                if (TimeInfo.Instance.FrameTime - mergeLineInfo.Time > 1)
                {
                    self.WaitMergetQueue.Dequeue();
                    
                    // 合线，把mapCopy2合并到mapCopy1
                    await mapInfo.MergeLines(mergeLineInfo.LineNum1, mergeLineInfo.LineNum2);
                    
                    Log.Info($"merge line ok, {mapInfo.MapName} {mergeLineInfo.LineNum1} {mergeLineInfo.LineNum2}");
                }
                else
                {
                    break;
                }
            }
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
            
            foreach ((int line, long mapId) in mapInfo.Lines)
            {
                MapCopy mapCopy = mapInfo.GetCopy(mapId);
                if (mapCopy.Status != MapCopyStatus.Running)
                {
                    continue;
                }
                if (mapCopy.WaitEnterPlayer.Count > 0)
                {
                    // 有等待进入的玩家，不能合线
                    continue;
                }

                if (mapCopy.Players.Count == 0)
                {
                    continue;
                }
                foreach ((int line2, long mapId2) in mapInfo.Lines)
                {
                    if (line2 <= line)
                    {
                        continue;
                    }
                    MapCopy mapCopy2 = mapInfo.GetCopy(mapId2);
                    if (mapCopy.Status != MapCopyStatus.Running)
                    {
                        continue;
                    }
                    if (mapCopy.WaitEnterPlayer.Count > 0)
                    {
                        // 有等待进入的玩家，不能合线
                        continue;
                    }
                    
                    if (mapCopy.Players.Count == 0)
                    {
                        continue;
                    }
                    
                    // 记录下来，可以合线
                    if (mapCopy.Players.Count + mapCopy2.Players.Count <= mapConfig.RecommendPlayerNum)
                    {
                        mapCopy.Status = MapCopyStatus.WaitMerge;
                        mapCopy2.Status = MapCopyStatus.WaitMerge;
                        self.WaitMergetQueue.Enqueue(new MergeLineInfo() {LineNum1 = line, LineNum2 = line2, Time = TimeInfo.Instance.FrameTime});
                        // 通知两个场景的玩家，1分钟后合线
                        Log.Info($"map can merge lines, {mapInfo.MapName} line1: {line} {mapCopy.Players.Count}, line2: {line2} {mapCopy2.Players.Count}");
                    }
                }
            }
        }
    }
}
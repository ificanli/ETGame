using System;
using System.Collections.Generic;

namespace ET.Server
{
    [EntitySystemOf(typeof(MergeLinesComponent))]
    public static partial class MergeLinesComponentSystem
    {
        [Invoke(TimerInvokeType.MergeLinesCheckTimer)]
        public class MergeLinesCheckTimer : ATimer<MergeLinesComponent>
        {
            protected override void Run(MergeLinesComponent self)
            {
                try
                {
                    self?.CheckAsync().NoContext();
                }
                catch (Exception e)
                {
                    Log.Error($"merge lines timer error: {self.Id}\n{e}");
                }
            }
        }

        [EntitySystem]
        private static void Awake(this MergeLinesComponent self)
        {
            self.Timer = self.Root().GetComponent<TimerComponent>().NewRepeatedTimer(1000, TimerInvokeType.MergeLinesCheckTimer, self);
        }
        
        [EntitySystem]
        private static void Destroy(this MergeLinesComponent self)
        {
            if (self.Root().IsDisposed)
            {
                return;
            }
            self.Root().GetComponent<TimerComponent>().Remove(ref self.Timer);
        }

        private static async ETTask CheckAsync(this MergeLinesComponent self)
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
                foreach ((int line2, long mapId2) in mapInfo.Lines)
                {
                    if (line <= line2)
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
                    
                    // 记录下来，可以合线
                    if (mapCopy.Players.Count + mapCopy2.Players.Count < mapConfig.RecommendPlayerNum)
                    {
                        mapCopy.Status = MapCopyStatus.WaitMerge;
                        mapCopy2.Status = MapCopyStatus.WaitMerge;
                        self.WaitMergetQueue.Enqueue(new MergeLineInfo() {LineNum1 = line, LineNum2 = line2, Time = TimeInfo.Instance.FrameTime});
                        // 通知两个场景的玩家，1分钟后合线
                    }
                }
            }
            
            // 如果有合线
            while (self.WaitMergetQueue.Count > 0)
            {
                MergeLineInfo mergeLineInfo = self.WaitMergetQueue.Peek();
                if (TimeInfo.Instance.FrameTime - mergeLineInfo.Time > 60 * 1000)
                {
                    self.WaitMergetQueue.Dequeue();
                    
                    // 合线，把mapCopy2合并到mapCopy1
                    await mapInfo.MergeLines(mergeLineInfo.LineNum1, mergeLineInfo.LineNum2);
                    
                    Log.Info($"合线成功: {mapInfo.MapName} {mergeLineInfo.LineNum1} {mergeLineInfo.LineNum2}");
                }
                else
                {
                    break;
                }
            }
        }
    }
}
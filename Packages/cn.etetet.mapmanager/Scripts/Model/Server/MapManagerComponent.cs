using System.Collections.Generic;

namespace ET.Server
{
    // 每种地图一个MapInfo，MapInfo管理多个MapCopy
    [ChildOf(typeof(MapManagerComponent))]
    public class MapInfo: Entity, IAwake<string>
    {
        public string MapName;
    }
    
    public enum MapCopyStatus
    {
        Running = 1,
        // 等待结束
        WaitFinish = 2,
        // 结束
        Finished = 3,
        // 等待合线
        WaitMerge = 4,
    }
    
    // 地图副本或者分线，一个MapCopy对应一个Fiber
    [ChildOf(typeof(MapInfo))]
    public class MapCopy: Entity, IAwake<int>, IDestroy
    {
        // 该副本已经进入的所有玩家
        public HashSet<long> Players = new();
        
        // 等待进入的玩家, key 为玩家id，value为进入副本的时间
        public Dictionary<long, long> WaitEnterPlayer = new();
        
        public MapCopyStatus Status = MapCopyStatus.Running;

        public long MergeTime = 0;

        public int FiberId;
    }
    
    
    
    [ComponentOf(typeof(Scene))]
    public class MapManagerComponent: Entity, IAwake
    {
        public Dictionary<string, EntityRef<MapInfo>> MapInfos = new();
    }
}
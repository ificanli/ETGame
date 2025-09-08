using System.Collections.Generic;

namespace ET.Server
{
    public struct MergeLineInfo
    {
        public long LineNum1;
        public long LineNum2;
        public long Time;
    }
    
    [ComponentOf(typeof(MapInfo))]
    public class MergeLinesComponent: Entity, IAwake, IDestroy
    {
        public Queue<MergeLineInfo> WaitMergetQueue = new();
    }
}
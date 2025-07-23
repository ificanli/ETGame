using System.Collections.Generic;

namespace ET.Server
{
    public struct MergeLineInfo
    {
        public int LineNum1;
        public int LineNum2;
        public long Time;
    }
    
    [ComponentOf(typeof(MapInfo))]
    public class MergeLinesComponent: Entity, IAwake, IDestroy
    {
        public Queue<MergeLineInfo> WaitMergetQueue = new();
    }
}
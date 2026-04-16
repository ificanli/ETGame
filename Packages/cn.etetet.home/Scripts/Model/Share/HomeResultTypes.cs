using System.Collections.Generic;

namespace ET
{
    public struct HomeCollectResult
    {
        public int ErrorCode;
        public List<int> ItemConfigIds;
        public List<int> ItemCounts;
        public long WealthDelta;
        public long CollectTickCount;
        public int CollectIntervalMs;
    }

    public struct HomeProductionStartResult
    {
        public int ErrorCode;
        public long OrderId;
    }
}

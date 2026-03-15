using System.Collections.Generic;

namespace ET
{
    public struct HomeCollectResult
    {
        public int ErrorCode;
        public List<int> ItemConfigIds;
        public List<int> ItemCounts;
    }

    public struct HomeProductionStartResult
    {
        public int ErrorCode;
        public long OrderId;
    }
}

using System.Collections.Generic;

namespace ET.Client
{
    public struct Wait_MatchSuccess : IWaitType
    {
        public int Error { get; set; }
        public int GameMode;
        public string MapName;
        public long MapId;
        public List<long> PlayerIds;
    }
}

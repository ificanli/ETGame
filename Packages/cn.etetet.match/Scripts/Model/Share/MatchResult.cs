using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 匹配结果，TryMatch 成功时产出
    /// </summary>
    public struct MatchResult
    {
        public int GameMode;
        public string MapName;
        public long MapId;
        public List<long> PlayerIds;
        public List<long> HumanPlayerIds;
        public List<long> RobotPlayerIds;
        public Dictionary<long, List<long>> GatePlayerIds;
        public Dictionary<long, int> PlayerTeamIds;
    }
}

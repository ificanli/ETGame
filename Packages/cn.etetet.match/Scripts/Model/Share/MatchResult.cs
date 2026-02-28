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
        public List<long> PlayerIds;
    }
}

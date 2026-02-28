using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 匹配成功事件，供高层包（如 battle）订阅
    /// </summary>
    public struct MatchSuccessEvent
    {
        public int GameMode;
        public string MapName;
        public List<long> PlayerIds;
    }
}

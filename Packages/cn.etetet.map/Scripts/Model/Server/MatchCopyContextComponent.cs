using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 匹配副本上下文，记录本局玩家与机器人补位信息。
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class MatchCopyContextComponent : Entity, IAwake, IDestroy
    {
        public int GameMode;

        public string MapName;

        public long MapId;

        public HashSet<long> HumanPlayerIds = new();

        public HashSet<long> EnteredHumanPlayerIds = new();

        public List<long> RobotPlayerIds = new();

        public Dictionary<long, int> PlayerTeamIds = new();

        public bool RobotsSpawned;
    }
}

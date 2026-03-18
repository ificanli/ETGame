using System.Collections.Generic;

namespace ET.Server
{
    [ComponentOf(typeof(Unit))]
    public class RogueProgressComponent : Entity, IAwake
    {
        public int Level { get; set; }
        public int CurrentExp { get; set; }
        public int NeedExp { get; set; }
        public int CurrentGold { get; set; }

        public long ChoiceSerial { get; set; }
        public bool ChoicePending { get; set; }

        public List<int> PendingOptionIds { get; set; } = new();
        public List<int> PendingOptionRerollCounts { get; set; } = new();
        public List<int> PendingChoiceLevels { get; set; } = new();
        public List<long> AppliedBuffIds { get; set; } = new();
        public List<int> SelectedOptionIds { get; set; } = new();
        public List<long> AppliedOptionBuffIds { get; set; } = new();
        public List<string> ClaimedPointRewardIds { get; set; } = new();
        public Dictionary<int, long> AppliedLevelNumericTotals { get; set; } = new();
        public Dictionary<int, int> CommonShowTagCounts { get; set; } = new();
        public Dictionary<int, long> AppliedShowTagBuffIds { get; set; } = new();
    }
}

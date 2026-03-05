using System.Collections.Generic;

namespace ET.Server
{
    [ComponentOf(typeof(Unit))]
    public class RogueProgressComponent : Entity, IAwake
    {
        public int Level { get; set; }
        public int CurrentExp { get; set; }
        public int NeedExp { get; set; }

        public long ChoiceSerial { get; set; }
        public bool ChoicePending { get; set; }

        public List<int> PendingOptionIds { get; set; } = new();
        public List<int> PendingChoiceLevels { get; set; } = new();
    }
}

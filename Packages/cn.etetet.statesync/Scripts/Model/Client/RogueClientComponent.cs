using System.Collections.Generic;

namespace ET.Client
{
    [ComponentOf(typeof(Scene))]
    public class RogueClientComponent : Entity, IAwake
    {
        public int Level { get; set; }
        public int CurrentExp { get; set; }
        public int NeedExp { get; set; }

        public long ChoiceSerial { get; set; }
        public List<RogueClientOptionData> ChoiceOptions { get; set; } = new();
    }

    public struct RogueClientOptionData
    {
        public int OptionId;
        public int BuffConfigId;
        public int NameTextId;
        public int DescTextId;
        public string Icon;
    }
}

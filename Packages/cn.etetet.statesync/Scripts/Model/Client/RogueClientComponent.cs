using System.Collections.Generic;

namespace ET.Client
{
    [ComponentOf(typeof(Scene))]
    public class RogueClientComponent : Entity, IAwake
    {
        public int Level { get; set; }
        public int CurrentExp { get; set; }
        public int NeedExp { get; set; }
        public int CurrentGold { get; set; }

        public long ChoiceSerial { get; set; }
        public bool ChoicePopupPending { get; set; }
        public bool ChoicePopupOpening { get; set; }
        public List<RogueClientOptionData> ChoiceOptions { get; set; } = new();
    }

    public struct RogueClientOptionData
    {
        public int OptionId;
        public int BuffConfigId;
        public string Name;
        public string Desc;
        public string ImagePath;
        public string BTConfig;
        public int NameTextId;
        public int DescTextId;
        public string Icon;
        public int Quality;
        public int[] ShowTags;
        public int[] HideTags;
    }
}

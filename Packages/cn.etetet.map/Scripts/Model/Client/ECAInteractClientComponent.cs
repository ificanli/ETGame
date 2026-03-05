using System.Collections.Generic;

namespace ET.Client
{
    [ComponentOf(typeof(Scene))]
    public class ECAInteractClientComponent : Entity, IAwake
    {
        public HashSet<string> InRangePointIds { get; set; } = new();
        public string FocusPointId { get; set; }

        public string SearchingPointId { get; set; }
        public int SearchState { get; set; }
        public long SearchRemainMs { get; set; }

        public string OpenContainerPointId { get; set; }
        public int ContainerOutputMode { get; set; }
        public List<ContainerClientItemData> ContainerItems { get; set; } = new();
    }

    public struct ContainerClientItemData
    {
        public int SlotIndex;
        public int ConfigId;
        public int Count;
    }
}

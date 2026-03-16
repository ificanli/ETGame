using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Client
{
    [ComponentOf(typeof(Scene))]
    public class ECAInteractClientComponent : Entity, IAwake
    {
        public HashSet<string> InRangePointIds { get; set; } = new();
        public Dictionary<string, int> PointButtonTextIds { get; set; } = new();
        public Dictionary<string, bool> PointCanInteract { get; set; } = new();
        public Dictionary<string, int> PointStates { get; set; } = new();
        public string FocusPointId { get; set; }

        public string SearchingPointId { get; set; }
        public int SearchState { get; set; }
        public long SearchRemainMs { get; set; }

        public string OpenContainerPointId { get; set; }
        public string OpenContainerUiKey { get; set; }
        public int ContainerOutputMode { get; set; }
        public List<ContainerClientItemData> ContainerItems { get; set; } = new();

        public string ConcealmentConfigMapName { get; set; }
        public List<LocalConcealmentAreaData> LocalConcealmentAreas { get; set; } = new();
        public bool SelfInConcealmentArea { get; set; }
        public float SelfConcealmentAlpha { get; set; }
    }

    public struct ContainerClientItemData
    {
        public int SlotIndex;
        public int ConfigId;
        public int Count;
    }

    public struct LocalConcealmentAreaData
    {
        public float3 Position;
        public float Radius;
        public float SelfAlpha;
    }
}

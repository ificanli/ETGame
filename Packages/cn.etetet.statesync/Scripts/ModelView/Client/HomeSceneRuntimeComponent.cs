using System.Collections.Generic;
using UnityEngine;

namespace ET.Client
{
    [EnableClass]
    public sealed class HomeSceneMaterialEntry
    {
        public Material Material;
        public Color BaseColor;
    }

    [EnableClass]
    public sealed class HomeSceneNodeData
    {
        public int SlotId;
        public long BuildingId;
        public GameObject RootObject;
        public readonly List<HomeSceneMaterialEntry> Materials = new();
    }

    [EnableClass]
    public sealed class HomeSceneHitData
    {
        public int SlotId;
        public long BuildingId;
    }

    [ComponentOf(typeof(Scene))]
    public class HomeSceneRuntimeComponent : Entity, IAwake, IUpdate, IDestroy
    {
        public string LastDataSnapshot = string.Empty;
        public int LastSelectedSlotId;
        public long LastSelectedBuildingId;
        public GameObject RootObject;
        public readonly Dictionary<int, HomeSceneHitData> HitLookup = new();
        public readonly List<HomeSceneNodeData> Nodes = new();
        public readonly List<Material> RuntimeMaterials = new();
    }
}

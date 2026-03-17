using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 装备槽位类型
    /// </summary>
    public enum EquipSlotType
    {
        Weapon = 1,
        Weapon2 = 2,
        Armor = 3,
        Bag = 4,
        BagContent = 5,
    }

    public partial class LobbyPanelComponent : Entity, ILateUpdate
    {
        public EntityRef<YIUILoopScrollChild> m_HeroLoop;
        public YIUILoopScrollChild HeroLoop => m_HeroLoop;

        public EntityRef<YIUILoopScrollChild> m_EquipBagLoop;
        public YIUILoopScrollChild EquipBagLoop => m_EquipBagLoop;

        public EntityRef<YIUILoopScrollChild> m_WarehouseLoop;
        public YIUILoopScrollChild WarehouseLoop => m_WarehouseLoop;

        public EntityRef<YIUI3DDisplayChild> m_HeroDisplay;
        public YIUI3DDisplayChild HeroDisplay => m_HeroDisplay;

        public EquipSlotType CurrentSelectingSlot;

        public List<int> BagEquipIds = new();

        public bool HasLoadoutSnapshot;

        public string CurrentHeroDisplayResName = string.Empty;
        public string CurrentHeroDisplayCameraName = string.Empty;

        public string LastLoadoutSnapshot = string.Empty;
        public int SelectedWarehouseConfigId;
        public long SelectedWarehouseItemUid;
        public bool IsDragging;
        public bool DraggingIsWarehouse;
        public long DraggingItemUid;
        public int DraggingConfigId;
        public int DraggingAnchorSlotIndex;
        public int DraggingAreaType;
        public int DraggingFixedSlotType;
        public RectTransform DraggingView;
        public Vector3 DragWorldOffset;

        public Vector2 GridSpacing = new Vector2(8f, 8f);
        public Vector2 GridPadding = new Vector2(8f, 8f);

        public RectTransform WarehouseGridRoot;
        public RectTransform WarehouseItemsLayer;
        public RectTransform WarehouseItemTemplate;

        public readonly Dictionary<long, RectTransform> CurrentBagItemViews = new();
        public readonly Dictionary<long, RectTransform> SecureItemViews = new();
        public readonly Dictionary<long, RectTransform> WarehouseItemViews = new();
        public readonly Dictionary<int, RectTransform> CurrentBagGridCellViews = new();
        public readonly Dictionary<int, RectTransform> SecureGridCellViews = new();
        public readonly Dictionary<int, RectTransform> WarehouseGridCellViews = new();
    }
}

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YIUIFramework;

namespace ET.Client
{
    public struct BattleRecordSummaryViewData
    {
        public long RecordId;
        public long PlayerId;
        public int GameMode;
        public string MapName;
        public long MapId;
        public long StartedAt;
        public long FinishedAt;
        public int ResultType;
        public bool IsSuccess;
        public int KillNum;
        public long TotalWealth;
    }

    public struct BattleRecordEventViewData
    {
        public long Timestamp;
        public int EventType;
        public long PlayerId;
        public long Value;
        public string Text;
    }

    public struct LoadoutAreaLayoutRect
    {
        public bool Active;
        public Vector2 OffsetFromBoardTopLeft;
        public Vector2 Size;
    }

    public struct LoadoutAreaLayoutInsets
    {
        public bool Active;
        public float Left;
        public float Top;
        public float Right;
        public float Bottom;
    }

    public struct LoadoutOwnedAreaLayoutCache
    {
        public bool Initialized;
        public RectTransform Background;
        public RectTransform Title;
        public RectTransform Hint;
        public LoadoutAreaLayoutInsets BackgroundInsets;
        public LoadoutAreaLayoutRect TitleRect;
        public LoadoutAreaLayoutRect HintRect;
    }

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
        public const float UnifiedCellSize = 96f;

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
        public bool IsMatchFlowRunning;
        public int SelectedMatchGameMode = GameModeType.OneVsOne;
        public LoadoutItemSourceMode CurrentItemSourceMode = LoadoutItemSourceMode.Warehouse;
        public int SelectedShopConfigId;
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
        public int FixedSlotClickVersion;
        public int OwnedGridItemClickVersion;
        public int WarehouseItemClickVersion;
        public bool WarehouseScrollForwarding;
        public int WarehousePressPointerId;
        public float WarehousePressStartedAt;
        public Vector2 WarehousePressPosition;
        public long WarehousePressItemUid;
        public RectTransform WarehousePressView;

        public Vector2 GridSpacing = new Vector2(4f, 4f);
        public Vector2 GridPadding = new Vector2(4f, 4f);
        public float WarehousePreferredCellSize = UnifiedCellSize;

        public EntityRef<BattleRecordPanelComponent> BattleRecordPanel;

        public RectTransform WarehouseGridRoot;
        public RectTransform WarehouseItemsLayer;
        public RectTransform WarehouseItemTemplate;
        public bool IsWarehouseTabActive;
        public RectTransform LoadoutEquipContentRoot;
        public RectTransform LoadoutWarehouseContentRoot;
        public RectTransform LoadoutContentSwitchRoot;
        public Button LoadoutEquipTabButton;
        public Button LoadoutWarehouseTabButton;
        public Graphic LoadoutEquipTabGraphic;
        public Graphic LoadoutWarehouseTabGraphic;
        public TMP_Text LoadoutEquipTabText;
        public TMP_Text LoadoutWarehouseTabText;
        public RectTransform LoadoutSourceToggleRoot;
        public Button LoadoutShopButton;
        public Button LoadoutWarehouseButton;
        public Graphic LoadoutShopButtonGraphic;
        public Graphic LoadoutWarehouseButtonGraphic;
        public TMP_Text LoadoutShopButtonText;
        public TMP_Text LoadoutWarehouseButtonText;

        public readonly Dictionary<long, RectTransform> CurrentBagItemViews = new();
        public readonly Dictionary<long, RectTransform> SecureItemViews = new();
        public readonly Dictionary<long, RectTransform> WarehouseItemViews = new();
        public readonly Dictionary<int, RectTransform> CurrentBagGridCellViews = new();
        public readonly Dictionary<int, RectTransform> SecureGridCellViews = new();
        public readonly Dictionary<int, RectTransform> WarehouseGridCellViews = new();

        public RectTransform BagContentWrapper;
        public RectTransform SecureContentWrapper;
        public RectTransform LoadoutOwnedAreaParentRoot;
        public VerticalLayoutGroup LoadoutOwnedAreaLayoutGroup;
        public bool LoadoutOwnedAreaLayoutGroupInitialized;
        public LayoutElement CurrentBagRootLayoutElement;
        public LayoutElement SecureRootLayoutElement;
        public bool LoadoutOwnedAreaLayoutInitialized;
        public Vector2 LoadoutBagAreaTopLeft;
        public float LoadoutOwnedAreaVerticalGap;
        public LoadoutOwnedAreaLayoutCache CurrentBagLayoutCache;
        public LoadoutOwnedAreaLayoutCache SecureLayoutCache;
        public EntityRef<ItemClickedComponent> ItemClickedCommon;
    }
}

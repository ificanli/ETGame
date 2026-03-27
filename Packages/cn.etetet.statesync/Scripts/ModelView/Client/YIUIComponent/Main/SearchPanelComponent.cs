using System;
using UnityEngine;
using UnityEngine.UI;
using YIUIFramework;
using System.Collections.Generic;
using ET;
using TMPro;

namespace ET.Client
{
    /// <summary>
    /// Author  YIUI
    /// Date    2026.3.5
    /// Desc
    /// </summary>
    public partial class SearchPanelComponent : Entity, ILateUpdate
    {
        public const string OnEventOpenSearchingViewInvoke = "SearchPanelComponent.OnEventOpenSearchingViewInvoke";
        public bool LastShowState;
        public string LastFocusPointId;
        public string LastOpenContainerPointId;
        public string LastContainerSnapshot;
        public SearchPanelOpenMode OpenMode;
        public SearchPanelCorpseSubType CorpseSubType;
        public string CurrentPointId;
        public string TitleText;
        public string SubTitleText;

        public int ContainerCols;
        public int ContainerRows;
        public int BagCols;
        public int BagRows;
        public int SecureCols;
        public int SecureRows;
        public Vector2 CellSize;
        public Vector2 CellSpacing;
        public Vector2 CellPadding;

        public GridPlacementSolver ContainerSolver;
        public GridPlacementSolver BagSolver;
        public GridPlacementSolver SecureSolver;

        public readonly Dictionary<long, RectTransform> ContainerItemViews = new();
        public readonly Dictionary<long, RectTransform> BagItemViews = new();
        public readonly Dictionary<long, RectTransform> SecureItemViews = new();
        public readonly Dictionary<int, RectTransform> ContainerGridCellViews = new();
        public readonly Dictionary<int, RectTransform> BagGridCellViews = new();
        public readonly Dictionary<int, RectTransform> SecureGridCellViews = new();

        public RectTransform ContainerGridRoot;
        public RectTransform BagGridRoot;
        public RectTransform SecureBoardRoot;
        public RectTransform SecureItemsLayer;
        public RectTransform SecureItemTemplate;
        public RectTransform SecureGridRoot;
        public Dropdown QuickChooseDropdown;
        public RectTransform QuickChooseButtonRoot;
        public TMP_Text QuickChooseButtonLabel;
        public Image QuickChooseButtonImage;
        public TMP_Text ModeTitleText;
        public TMP_Text ModeSubTitleText;
        public TMP_Text ContainerTitleText;
        public TMP_Text BagTitleText;
        public Image ModeAccentImage;
        public Vector2 BagBoardAnchorMin;
        public Vector2 BagBoardAnchorMax;
        public Vector2 BagBoardAnchoredPosition;
        public Vector2 BagBoardSizeDelta;
        public Vector2 ContainerBoardAnchorMin;

        public bool IsDragging;
        public bool DraggingIsBag;
        public long DraggingItemId;
        public RectTransform DraggingView;
        public Vector3 DragWorldOffset;
        public int QuickChooseMinQuality;

        // 搜索动效相关字段
        /// <summary>
        /// 每个槽位的搜索开始时间（毫秒时间戳），Key=SlotIndex
        /// </summary>
        public readonly Dictionary<int, long> SlotSearchStartTimes = new();

        /// <summary>
        /// 已完成搜索的槽位集合，Key=SlotIndex
        /// </summary>
        public readonly HashSet<int> SearchedSlots = new();

        /// <summary>
        /// 每个槽位的搜索动效GameObject引用，Key=SlotIndex
        /// </summary>
        public readonly Dictionary<int, GameObject> SlotSearchingEffects = new();

        /// <summary>
        /// 每个槽位的搜索持续时间（毫秒），Key=SlotIndex，根据物品品质决定
        /// </summary>
        public readonly Dictionary<int, long> SlotSearchDurations = new();

        /// <summary>
        /// 当前容器的PointId，用于判断是否切换了容器
        /// </summary>
        public string CurrentSearchingPointId;
        public EntityRef<ItemClickedComponent> ItemClickedCommon;
    }
}

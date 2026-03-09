using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

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

        public int ContainerCols;
        public int ContainerRows;
        public int BagCols;
        public int BagRows;
        public Vector2 CellSize;
        public Vector2 CellSpacing;
        public Vector2 CellPadding;

        public GridPlacementSolver ContainerSolver;
        public GridPlacementSolver BagSolver;

        public readonly Dictionary<long, RectTransform> ContainerItemViews = new();
        public readonly Dictionary<long, RectTransform> BagItemViews = new();
        public readonly Dictionary<int, RectTransform> ContainerGridCellViews = new();
        public readonly Dictionary<int, RectTransform> BagGridCellViews = new();

        public RectTransform ContainerGridRoot;
        public RectTransform BagGridRoot;

        public bool IsDragging;
        public bool DraggingIsBag;
        public long DraggingItemId;
        public RectTransform DraggingView;
        public Vector3 DragWorldOffset;
    }
}

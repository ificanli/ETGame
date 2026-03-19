namespace ET.Client
{
    [EntitySystemOf(typeof(SearchPanelOpenContextComponent))]
    public static partial class SearchPanelOpenContextComponentSystem
    {
        [EntitySystem]
        private static void Awake(this SearchPanelOpenContextComponent self)
        {
            self.ClearOpenContext();
        }

        public static void PrepareBackpackOpen(this SearchPanelOpenContextComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            self.OpenMode = SearchPanelOpenMode.BackpackInspect;
            self.CorpseSubType = SearchPanelCorpseSubType.Unknown;
            self.CurrentPointId = null;
            self.TitleText = null;
            self.SubTitleText = null;
        }

        public static void PrepareContainerOpen(this SearchPanelOpenContextComponent self, string pointId)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            SearchPanelModeResolveResult result = SearchPanelModeHelper.ResolveContainerMode(pointId);
            self.OpenMode = result.OpenMode;
            self.CorpseSubType = result.CorpseSubType;
            self.CurrentPointId = pointId;
            self.TitleText = null;
            self.SubTitleText = null;
        }

        public static void ClearOpenContext(this SearchPanelOpenContextComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            self.OpenMode = SearchPanelOpenMode.Unknown;
            self.CorpseSubType = SearchPanelCorpseSubType.Unknown;
            self.CurrentPointId = null;
            self.TitleText = null;
            self.SubTitleText = null;
        }
    }

    /// <summary>
    /// SearchPanel 打开上下文读写辅助。
    /// </summary>
    public static class SearchPanelOpenContextHelper
    {
        public static SearchPanelOpenContextComponent GetOrAdd(Scene root)
        {
            if (root == null || root.IsDisposed)
            {
                return null;
            }

            SearchPanelOpenContextComponent context = root.GetComponent<SearchPanelOpenContextComponent>();
            return context ?? root.AddComponent<SearchPanelOpenContextComponent>();
        }
    }
}

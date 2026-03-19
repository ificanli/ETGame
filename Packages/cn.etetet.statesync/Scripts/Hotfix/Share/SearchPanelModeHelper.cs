using System;

namespace ET
{
    /// <summary>
    /// SearchPanel 模式解析与关闭判定 Helper。
    /// </summary>
    public static class SearchPanelModeHelper
    {
        public static SearchPanelModeResolveResult ResolveContainerMode(string pointId)
        {
            SearchPanelModeResolveResult result = new SearchPanelModeResolveResult
            {
                OpenMode = SearchPanelOpenMode.ContainerSearch,
                CorpseSubType = SearchPanelCorpseSubType.Unknown,
            };

            if (!string.IsNullOrWhiteSpace(pointId) &&
                pointId.StartsWith(SearchPanelOpenConst.PlayerCorpsePointPrefix, StringComparison.OrdinalIgnoreCase))
            {
                result.OpenMode = SearchPanelOpenMode.CorpseLoot;
                result.CorpseSubType = SearchPanelCorpseSubType.Player;
            }

            return result;
        }

        public static bool ShouldUseContainerClose(SearchPanelOpenMode openMode)
        {
            return openMode == SearchPanelOpenMode.ContainerSearch || openMode == SearchPanelOpenMode.CorpseLoot;
        }

        public static bool ShouldUseBagOnlyLayout(SearchPanelOpenMode openMode)
        {
            return openMode == SearchPanelOpenMode.BackpackInspect;
        }

        public static bool ShouldShowQuickActionControls(SearchPanelOpenMode openMode)
        {
            return openMode == SearchPanelOpenMode.ContainerSearch || openMode == SearchPanelOpenMode.CorpseLoot;
        }
    }
}

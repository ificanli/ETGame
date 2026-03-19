namespace ET
{
    /// <summary>
    /// SearchPanel 模式对应的显示文案与样式 key。
    /// </summary>
    public static class SearchPanelDisplayHelper
    {
        public static SearchPanelDisplayInfo Resolve(
            SearchPanelOpenMode openMode,
            SearchPanelCorpseSubType corpseSubType,
            int containerOutputMode,
            string titleTextOverride,
            string subTitleTextOverride)
        {
            SearchPanelDisplayInfo info = openMode switch
            {
                SearchPanelOpenMode.ContainerSearch => ResolveContainerInfo(containerOutputMode),
                SearchPanelOpenMode.CorpseLoot => ResolveCorpseInfo(corpseSubType),
                SearchPanelOpenMode.BackpackInspect => ResolveBagInfo(),
                _ => ResolveBagInfo(),
            };

            if (!string.IsNullOrWhiteSpace(titleTextOverride))
            {
                info.TitleText = titleTextOverride;
            }

            if (!string.IsNullOrWhiteSpace(subTitleTextOverride))
            {
                info.SubTitleText = subTitleTextOverride;
            }

            return info;
        }

        private static SearchPanelDisplayInfo ResolveBagInfo()
        {
            return new SearchPanelDisplayInfo
            {
                StyleKey = "bag",
                TitleText = "背包",
                SubTitleText = "物品查看",
                ContainerTitleText = string.Empty,
                BagTitleText = "背包",
                QuickActionText = string.Empty,
            };
        }

        private static SearchPanelDisplayInfo ResolveContainerInfo(int containerOutputMode)
        {
            if (containerOutputMode == ContainerOutputMode.GroundDrop)
            {
                return new SearchPanelDisplayInfo
                {
                    StyleKey = "ground_drop",
                    TitleText = "地面掉落",
                    SubTitleText = "临时战利品",
                    ContainerTitleText = "掉落物",
                    BagTitleText = "背包",
                    QuickActionText = "一键拾取",
                };
            }

            return new SearchPanelDisplayInfo
            {
                StyleKey = "container",
                TitleText = "搜索容器",
                SubTitleText = "容器",
                ContainerTitleText = "容器",
                BagTitleText = "背包",
                QuickActionText = "一键拾取",
            };
        }

        private static SearchPanelDisplayInfo ResolveCorpseInfo(SearchPanelCorpseSubType corpseSubType)
        {
            string corpseText = corpseSubType switch
            {
                SearchPanelCorpseSubType.Player => "玩家尸体",
                SearchPanelCorpseSubType.Monster => "怪物尸体",
                SearchPanelCorpseSubType.Boss => "首领尸体",
                SearchPanelCorpseSubType.Npc => "NPC尸体",
                _ => "尸体",
            };

            string styleKey = corpseSubType switch
            {
                SearchPanelCorpseSubType.Player => "corpse_player",
                SearchPanelCorpseSubType.Monster => "corpse_monster",
                SearchPanelCorpseSubType.Boss => "corpse_boss",
                _ => "corpse",
            };

            return new SearchPanelDisplayInfo
            {
                StyleKey = styleKey,
                TitleText = "搜索尸体",
                SubTitleText = corpseText,
                ContainerTitleText = corpseText,
                BagTitleText = "背包",
                QuickActionText = "一键搜刮",
            };
        }
    }
}

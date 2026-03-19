namespace ET.Test
{
    /// <summary>
    /// TDD: 验证 SearchPanel 三模式的显示文案与样式 key 解析。
    /// </summary>
    public class Statesync_SearchPanel_DisplayInfo_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.CreateOneFiber(
                context.Fiber, SceneType.TestEmpty, nameof(Statesync_SearchPanel_DisplayInfo_Test));

            SearchPanelDisplayInfo bagInfo = SearchPanelDisplayHelper.Resolve(
                SearchPanelOpenMode.BackpackInspect,
                SearchPanelCorpseSubType.Unknown,
                ContainerOutputMode.ContainerPanel,
                null,
                null);
            if (bagInfo.StyleKey != "bag" || bagInfo.TitleText != "背包" || bagInfo.QuickActionText != string.Empty)
            {
                Log.Console($"unexpected bag display info: style={bagInfo.StyleKey}, title={bagInfo.TitleText}, action={bagInfo.QuickActionText}");
                return 1;
            }

            SearchPanelDisplayInfo containerInfo = SearchPanelDisplayHelper.Resolve(
                SearchPanelOpenMode.ContainerSearch,
                SearchPanelCorpseSubType.Unknown,
                ContainerOutputMode.ContainerPanel,
                null,
                null);
            if (containerInfo.StyleKey != "container" || containerInfo.TitleText != "搜索容器" || containerInfo.QuickActionText != "一键拾取")
            {
                Log.Console($"unexpected container display info: style={containerInfo.StyleKey}, title={containerInfo.TitleText}, action={containerInfo.QuickActionText}");
                return 2;
            }

            SearchPanelDisplayInfo groundDropInfo = SearchPanelDisplayHelper.Resolve(
                SearchPanelOpenMode.ContainerSearch,
                SearchPanelCorpseSubType.Unknown,
                ContainerOutputMode.GroundDrop,
                null,
                null);
            if (groundDropInfo.StyleKey != "ground_drop" || groundDropInfo.TitleText != "地面掉落")
            {
                Log.Console($"unexpected ground drop display info: style={groundDropInfo.StyleKey}, title={groundDropInfo.TitleText}");
                return 3;
            }

            SearchPanelDisplayInfo corpseInfo = SearchPanelDisplayHelper.Resolve(
                SearchPanelOpenMode.CorpseLoot,
                SearchPanelCorpseSubType.Player,
                ContainerOutputMode.ContainerPanel,
                null,
                null);
            if (corpseInfo.StyleKey != "corpse_player" || corpseInfo.SubTitleText != "玩家尸体" || corpseInfo.QuickActionText != "一键搜刮")
            {
                Log.Console($"unexpected corpse display info: style={corpseInfo.StyleKey}, subtitle={corpseInfo.SubTitleText}, action={corpseInfo.QuickActionText}");
                return 4;
            }

            SearchPanelDisplayInfo overrideInfo = SearchPanelDisplayHelper.Resolve(
                SearchPanelOpenMode.CorpseLoot,
                SearchPanelCorpseSubType.Monster,
                ContainerOutputMode.ContainerPanel,
                "自定义标题",
                "自定义副标题");
            if (overrideInfo.TitleText != "自定义标题" || overrideInfo.SubTitleText != "自定义副标题")
            {
                Log.Console($"override title failed: title={overrideInfo.TitleText}, subtitle={overrideInfo.SubTitleText}");
                return 5;
            }

            return ErrorCode.ERR_Success;
        }
    }
}

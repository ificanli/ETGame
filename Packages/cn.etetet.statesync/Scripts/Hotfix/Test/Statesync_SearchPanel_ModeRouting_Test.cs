namespace ET.Test
{
    /// <summary>
    /// TDD: 验证 SearchPanel 三模式的核心模式解析与关闭判定。
    /// </summary>
    public class Statesync_SearchPanel_ModeRouting_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.CreateOneFiber(
                context.Fiber, SceneType.TestEmpty, nameof(Statesync_SearchPanel_ModeRouting_Test));

            SearchPanelModeResolveResult corpseResult = global::ET.SearchPanelModeHelper.ResolveContainerMode("corpse_1001");
            if (corpseResult.OpenMode != SearchPanelOpenMode.CorpseLoot)
            {
                Log.Console($"corpse point should resolve to CorpseLoot, actual: {corpseResult.OpenMode}");
                return 1;
            }

            if (corpseResult.CorpseSubType != SearchPanelCorpseSubType.Player)
            {
                Log.Console($"corpse point should resolve to Player subtype, actual: {corpseResult.CorpseSubType}");
                return 2;
            }

            SearchPanelModeResolveResult containerResult = global::ET.SearchPanelModeHelper.ResolveContainerMode("container_01");
            if (containerResult.OpenMode != SearchPanelOpenMode.ContainerSearch)
            {
                Log.Console($"normal container should resolve to ContainerSearch, actual: {containerResult.OpenMode}");
                return 3;
            }

            if (containerResult.CorpseSubType != SearchPanelCorpseSubType.Unknown)
            {
                Log.Console($"normal container corpse subtype should be Unknown, actual: {containerResult.CorpseSubType}");
                return 4;
            }

            SearchPanelModeResolveResult emptyResult = global::ET.SearchPanelModeHelper.ResolveContainerMode(string.Empty);
            if (emptyResult.OpenMode != SearchPanelOpenMode.ContainerSearch)
            {
                Log.Console($"empty point id should fallback to ContainerSearch, actual: {emptyResult.OpenMode}");
                return 5;
            }

            if (!global::ET.SearchPanelModeHelper.ShouldUseContainerClose(SearchPanelOpenMode.ContainerSearch))
            {
                Log.Console("ContainerSearch should use container close");
                return 6;
            }

            if (!global::ET.SearchPanelModeHelper.ShouldUseContainerClose(SearchPanelOpenMode.CorpseLoot))
            {
                Log.Console("CorpseLoot should use container close");
                return 7;
            }

            if (global::ET.SearchPanelModeHelper.ShouldUseContainerClose(SearchPanelOpenMode.BackpackInspect))
            {
                Log.Console("BackpackInspect should not use container close");
                return 8;
            }

            if (global::ET.SearchPanelModeHelper.ShouldUseContainerClose(SearchPanelOpenMode.Unknown))
            {
                Log.Console("Unknown mode should not use container close");
                return 9;
            }

            if (!global::ET.SearchPanelModeHelper.ShouldUseBagOnlyLayout(SearchPanelOpenMode.BackpackInspect))
            {
                Log.Console("BackpackInspect should use bag only layout");
                return 10;
            }

            if (global::ET.SearchPanelModeHelper.ShouldUseBagOnlyLayout(SearchPanelOpenMode.ContainerSearch))
            {
                Log.Console("ContainerSearch should not use bag only layout");
                return 11;
            }

            if (!global::ET.SearchPanelModeHelper.ShouldShowQuickActionControls(SearchPanelOpenMode.ContainerSearch))
            {
                Log.Console("ContainerSearch should show quick action controls");
                return 12;
            }

            if (!global::ET.SearchPanelModeHelper.ShouldShowQuickActionControls(SearchPanelOpenMode.CorpseLoot))
            {
                Log.Console("CorpseLoot should show quick action controls");
                return 13;
            }

            if (global::ET.SearchPanelModeHelper.ShouldShowQuickActionControls(SearchPanelOpenMode.BackpackInspect))
            {
                Log.Console("BackpackInspect should not show quick action controls");
                return 14;
            }

            return ErrorCode.ERR_Success;
        }
    }
}

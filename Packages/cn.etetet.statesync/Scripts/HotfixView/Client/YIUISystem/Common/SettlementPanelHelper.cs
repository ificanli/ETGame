namespace ET.Client
{
    public static class SettlementPanelHelper
    {
        public static async ETTask TryOpenOrRefresh(Scene root)
        {
            SettlementClientComponent runtime = root?.GetComponent<SettlementClientComponent>();
            if (root == null || root.IsDisposed || runtime == null || !runtime.HasSettlement)
            {
                await ETTask.CompletedTask;
                return;
            }

            YIUIRootComponent yiuiRoot = root.YIUIRoot();
            if (yiuiRoot == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            EntityRef<Scene> rootRef = root;
            YIUIMgrComponent yiuiMgr = root.YIUIMgr();
            if (yiuiMgr?.GetPanel<SettlementPanelComponent>() == null ||
                !yiuiMgr.ActiveSelf<SettlementPanelComponent>())
            {
                await yiuiRoot.OpenPanelAsync<SettlementPanelComponent>();
                root = rootRef;
                if (root == null || root.IsDisposed)
                {
                    return;
                }
            }

            root.YIUIMgr()?.GetPanel<SettlementPanelComponent>()?.RefreshView();
        }
    }
}

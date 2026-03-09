using YIUIFramework;

namespace ET.Client
{
    public static class RoguePanelChoiceHelper
    {
        public static async ETTask TryChooseOption(RoguePanelComponent panel, int index)
        {
            if (panel == null || panel.IsDisposed)
            {
                return;
            }

            Scene root = panel.Root();
            RogueClientComponent runtime = RogueClientHelper.GetOrAddRuntime(root);
            if (runtime == null || index < 0 || index >= runtime.ChoiceOptions.Count)
            {
                await ETTask.CompletedTask;
                return;
            }

            EntityRef<RoguePanelComponent> panelRef = panel;
            EntityRef<Scene> rootRef = root;
            await RogueClientHelper.ChooseOption(root, runtime.ChoiceOptions[index].OptionId);

            panel = panelRef;
            root = rootRef;
            if (panel == null || panel.IsDisposed)
            {
                return;
            }

            runtime = RogueClientHelper.GetOrAddRuntime(root);
            if (runtime == null || runtime.ChoiceSerial == 0 || runtime.ChoiceOptions.Count == 0)
            {
                if (root.YIUIMgr()?.GetPanel<RoguePanelComponent>() != null)
                {
                    await root.YIUIMgr().ClosePanelAsync<RoguePanelComponent>();
                }
                return;
            }

            panel.RefreshView();
        }
    }
}

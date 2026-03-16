using YIUIFramework;

namespace ET.Client
{
    public static class RoguePanelChoiceHelper
    {
        public static async ETTask TryChooseOption(RoguePanelComponent panel, int index)
        {
            if (panel == null || panel.IsDisposed)
            {
                Log.Warning($"[RogueClient] choose option aborted: panel missing, index={index}");
                return;
            }

            Scene root = panel.Root();
            RogueClientComponent runtime = RogueClientHelper.GetOrAddRuntime(root);
            Log.Info(
                $"[RogueClient] try choose option: index={index}, serial={runtime?.ChoiceSerial ?? 0}, optionCount={runtime?.ChoiceOptions.Count ?? 0}, pending={runtime?.ChoicePopupPending ?? false}");
            if (runtime == null || index < 0 || index >= runtime.ChoiceOptions.Count)
            {
                Log.Warning(
                    $"[RogueClient] choose option blocked: index={index}, serial={runtime?.ChoiceSerial ?? 0}, optionCount={runtime?.ChoiceOptions.Count ?? 0}");
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
                Log.Info(
                    $"[RogueClient] choice finished or cleared, close panel: serial={runtime?.ChoiceSerial ?? 0}, optionCount={runtime?.ChoiceOptions.Count ?? 0}");
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

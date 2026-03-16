namespace ET.Client
{
    [Event(SceneType.Client)]
    public class EventRogueChoicePopup_OpenPanel : AEvent<Scene, EventRogueChoicePopup>
    {
        protected override async ETTask Run(Scene root, EventRogueChoicePopup args)
        {
            if (root == null || root.IsDisposed)
            {
                await ETTask.CompletedTask;
                return;
            }

            RogueClientComponent runtime = RogueClientHelper.GetOrAddRuntime(root);
            if (runtime == null || !runtime.ChoicePopupPending || runtime.ChoiceSerial <= 0 || runtime.ChoiceOptions.Count == 0 || runtime.ChoicePopupOpening)
            {
                Log.Info(
                    $"[RogueInitClient] open panel skipped: runtime={(runtime != null)}, pending={runtime?.ChoicePopupPending ?? false}, serial={runtime?.ChoiceSerial ?? 0}, optionCount={runtime?.ChoiceOptions.Count ?? 0}, opening={runtime?.ChoicePopupOpening ?? false}");
                await ETTask.CompletedTask;
                return;
            }

            Log.Info($"[RogueInitClient] begin open popup panel, serial={runtime.ChoiceSerial}, optionCount={runtime.ChoiceOptions.Count}");
            runtime.SetPopupOpening(true);
            EntityRef<Scene> rootRef = root;
            try
            {
                for (int retry = 0; retry < 60; ++retry)
                {
                    root = rootRef;
                    if (root == null || root.IsDisposed)
                    {
                        return;
                    }

                    runtime = root.GetComponent<RogueClientComponent>();
                    if (runtime == null || !runtime.ChoicePopupPending || runtime.ChoiceSerial <= 0 || runtime.ChoiceOptions.Count == 0)
                    {
                        Log.Info(
                            $"[RogueInitClient] open panel aborted during retry, retry={retry}, runtime={(runtime != null)}, pending={runtime?.ChoicePopupPending ?? false}, serial={runtime?.ChoiceSerial ?? 0}, optionCount={runtime?.ChoiceOptions.Count ?? 0}");
                        return;
                    }

                    YIUIRootComponent yiuiRoot = root.YIUIRoot();
                    if (yiuiRoot != null)
                    {
                        MainPanelComponent mainPanel = root.YIUIMgr()?.GetPanel<MainPanelComponent>();
                        if (mainPanel == null)
                        {
                            TimerComponent waitMainPanelTimer = root.Root()?.TimerComponent;
                            if (waitMainPanelTimer == null || waitMainPanelTimer.IsDisposed)
                            {
                                return;
                            }

                            await waitMainPanelTimer.WaitFrameAsync();
                            continue;
                        }

                        if (root.YIUIMgr()?.GetPanel<RoguePanelComponent>() == null)
                        {
                            await yiuiRoot.OpenPanelAsync<RoguePanelComponent>();
                            root = rootRef;
                            if (root == null || root.IsDisposed)
                            {
                                return;
                            }
                        }

                        RoguePanelComponent panel = root.YIUIMgr()?.GetPanel<RoguePanelComponent>();
                        if (panel != null)
                        {
                            panel.RefreshView();
                            root.GetComponent<RogueClientComponent>()?.SetPopupShown();
                            RogueClientComponent currentRuntime = root.GetComponent<RogueClientComponent>();
                            Log.Info($"[RogueInitClient] popup panel opened successfully, serial={currentRuntime?.ChoiceSerial ?? 0}, retry={retry}");
                            return;
                        }
                    }

                    root = rootRef;
                    if (root == null || root.IsDisposed)
                    {
                        return;
                    }

                    TimerComponent timerComponent = root.Root()?.TimerComponent;
                    if (timerComponent == null || timerComponent.IsDisposed)
                    {
                        return;
                    }

                    await timerComponent.WaitFrameAsync();
                }

                root = rootRef;
                runtime = root?.GetComponent<RogueClientComponent>();
                if (runtime != null)
                {
                    Log.Warning(
                        $"[RogueInitClient] open popup retry exhausted: serial={runtime.ChoiceSerial}, optionCount={runtime.ChoiceOptions.Count}, yiuiRoot={(root?.YIUIRoot() != null)}, roguePanel={(root?.YIUIMgr()?.GetPanel<RoguePanelComponent>() != null)}");
                    Log.Warning($"[RogueClient] open popup retry exhausted: serial={runtime.ChoiceSerial}, optionCount={runtime.ChoiceOptions.Count}");
                }
            }
            finally
            {
                root = rootRef;
                root?.GetComponent<RogueClientComponent>()?.SetPopupOpening(false);
            }
        }
    }
}

namespace ET.Client
{
    [Event(SceneType.Client)]
    public class EventRogueProgressChanged_RefreshRogueUi : AEvent<Scene, EventRogueProgressChanged>
    {
        protected override async ETTask Run(Scene root, EventRogueProgressChanged args)
        {
            if (root == null || root.IsDisposed)
            {
                await ETTask.CompletedTask;
                return;
            }

            root.YIUIMgr()?.GetPanel<MainPanelComponent>()?.RefreshRogueLevelBar(true);
            RefreshMyUnitHpView(root);
            await ETTask.CompletedTask;
        }

        private static void RefreshMyUnitHpView(Scene root)
        {
            Scene currentScene = root.CurrentScene();
            Unit myUnit = UnitHelper.GetMyUnitFromCurrentScene(currentScene);
            if (myUnit?.Children == null)
            {
                return;
            }

            foreach (Entity child in myUnit.Children.Values)
            {
                if (child is not YIUIChild uiChild || uiChild.IsDisposed)
                {
                    continue;
                }

                uiChild.GetComponent<HPViewComponent>()?.RefreshRogueProgress(true);
            }
        }
    }
}

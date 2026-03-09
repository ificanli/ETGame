namespace ET.Client
{
    [Event(SceneType.Client)]
    public class EventRogueChoicePopup_OpenPanel : AEvent<Scene, EventRogueChoicePopup>
    {
        protected override async ETTask Run(Scene root, EventRogueChoicePopup args)
        {
            var yiuiRoot = root.YIUIRoot();
            if (yiuiRoot == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            EntityRef<Scene> rootRef = root;
            await yiuiRoot.OpenPanelAsync<RoguePanelComponent>();
            root = rootRef;
            root.YIUIMgr()?.GetPanel<RoguePanelComponent>()?.RefreshView();
        }
    }
}

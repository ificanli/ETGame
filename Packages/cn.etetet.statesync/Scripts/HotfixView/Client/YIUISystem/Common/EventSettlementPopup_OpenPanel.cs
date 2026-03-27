namespace ET.Client
{
    [Event(SceneType.Client)]
    public class EventSettlementPopup_OpenPanel : AEvent<Scene, EventSettlementPopup>
    {
        protected override async ETTask Run(Scene root, EventSettlementPopup args)
        {
            Scene currentScene = root?.GetComponent<CurrentScenesComponent>()?.Scene;
            if (currentScene == null || currentScene.IsDisposed)
            {
                await ETTask.CompletedTask;
                return;
            }

            // 回城前不要立刻弹结算，避免在切场过程中闪一下又被 Home 界面顶掉。
            if (currentScene.Name.GetSceneConfigName() != "Home")
            {
                await ETTask.CompletedTask;
                return;
            }

            await SettlementPanelHelper.TryOpenOrRefresh(root);
        }
    }
}

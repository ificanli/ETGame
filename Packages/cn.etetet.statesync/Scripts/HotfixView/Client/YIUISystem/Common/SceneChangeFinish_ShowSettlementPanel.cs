namespace ET.Client
{
    [Event(SceneType.Client)]
    public class SceneChangeFinish_ShowSettlementPanel : AEvent<Scene, SceneChangeFinish>
    {
        protected override async ETTask Run(Scene root, SceneChangeFinish args)
        {
            SettlementClientComponent runtime = root.GetComponent<SettlementClientComponent>();
            if (runtime == null || !runtime.HasSettlement || !runtime.PendingOpen)
            {
                await ETTask.CompletedTask;
                return;
            }

            Scene currentScene = root.GetComponent<CurrentScenesComponent>()?.Scene;
            if (currentScene == null || currentScene.IsDisposed ||
                currentScene.Name.GetSceneConfigName() != "Home")
            {
                await ETTask.CompletedTask;
                return;
            }

            await SettlementPanelHelper.TryOpenOrRefresh(root);
        }
    }
}

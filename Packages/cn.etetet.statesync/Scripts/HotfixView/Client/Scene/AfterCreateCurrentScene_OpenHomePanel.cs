namespace ET.Client
{
    [Event(SceneType.Current)]
    public class AfterCreateCurrentScene_OpenHomePanel : AEvent<Scene, AfterCreateCurrentScene>
    {
        protected override async ETTask Run(Scene scene, AfterCreateCurrentScene args)
        {
            if (scene.Name.GetSceneConfigName() != "Home")
            {
                await ETTask.CompletedTask;
                return;
            }

            Scene root = scene.Root();
            if (root == null || root.IsDisposed)
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
            if (root.YIUIMgr()?.GetPanel<LobbyPanelComponent>() == null)
            {
                await yiuiRoot.OpenPanelAsync<LobbyPanelComponent>();
                root = rootRef;
                if (root == null || root.IsDisposed)
                {
                    return;
                }
            }

            if (root.YIUIMgr()?.GetPanel<HomePanelComponent>() == null)
            {
                await root.OpenHomePanelAsync();
            }
        }
    }
}

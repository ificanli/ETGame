using System;

namespace ET.Client
{
    [Event(SceneType.Client)]
    public class SceneChangeStart_CloseHomePanel : AEvent<Scene, SceneChangeStart>
    {
        protected override async ETTask Run(Scene root, SceneChangeStart args)
        {
            if (!args.ChangeScene)
            {
                await ETTask.CompletedTask;
                return;
            }

            EntityRef<Scene> rootRef = root;
            await root.CloseHomePanelAsync(false);
            root = rootRef;
            if (root == null || root.IsDisposed)
            {
                return;
            }

            YIUIMgrComponent yiuiMgr = root.YIUIMgr();
            if (yiuiMgr == null || yiuiMgr.IsDisposed || yiuiMgr.GetPanel<LobbyPanelComponent>() == null)
            {
                return;
            }

            try
            {
                await yiuiMgr.ClosePanelAsync<LobbyPanelComponent>(false);
            }
            catch (Exception exception)
            {
                Log.Warning($"[LobbyUI] scene change start close lobby panel ignored exception: {exception.Message}");
            }
        }
    }
}

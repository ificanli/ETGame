namespace ET.Client
{
    [Event(SceneType.Client)]
    public class SceneChangeStart_RogueCleanup : AEvent<Scene, SceneChangeStart>
    {
        protected override async ETTask Run(Scene root, SceneChangeStart args)
        {
            if (!args.ChangeScene)
            {
                await ETTask.CompletedTask;
                return;
            }

            RogueClientHelper.ResetRuntime(root);
            if (root.YIUIMgr()?.GetPanel<RoguePanelComponent>() != null)
            {
                await root.YIUIMgr().ClosePanelAsync<RoguePanelComponent>();
            }

            await ETTask.CompletedTask;
        }
    }
}

namespace ET.Client
{
    /// <summary>
    /// AUD-021: 搜索容器打开音效
    /// </summary>
    [Event(SceneType.Client)]
    public class ECAContainerOpenUIEvent_Audio : AEvent<Scene, ECAContainerOpenUIEvent>
    {
        protected override async ETTask Run(Scene root, ECAContainerOpenUIEvent args)
        {
            AudioHelper.PlayUi(root, AudioEventId.SfxContainerOpen);
            await ETTask.CompletedTask;
        }
    }
}

namespace ET.Client
{
    /// <summary>
    /// AUD-017: 肉鸽升级音效
    /// </summary>
    [MessageHandler(SceneType.Client)]
    public class M2C_RogueLevelUp_AudioHandler : MessageHandler<Scene, M2C_RogueLevelUp>
    {
        protected override async ETTask Run(Scene root, M2C_RogueLevelUp message)
        {
            AudioHelper.PlaySfx(root, AudioEventId.SfxLevelUp);
            await ETTask.CompletedTask;
        }
    }

    /// <summary>
    /// AUD-018: 肉鸽三选一弹出音效
    /// </summary>
    [Event(SceneType.Client)]
    public class EventRogueChoicePopup_Audio : AEvent<Scene, EventRogueChoicePopup>
    {
        protected override async ETTask Run(Scene root, EventRogueChoicePopup args)
        {
            AudioHelper.PlayUi(root, AudioEventId.SfxChoicePopup);
            await ETTask.CompletedTask;
        }
    }
}

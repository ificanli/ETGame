namespace ET.Client
{
    /// <summary>
    /// AUD-012: 击中敌人反馈音效
    /// </summary>
    [MessageHandler(SceneType.Client)]
    public class M2C_WeaponHit_AudioHandler : MessageHandler<Scene, M2C_WeaponHit>
    {
        protected override async ETTask Run(Scene root, M2C_WeaponHit message)
        {
            AudioHelper.PlaySfx(root, AudioEventId.SfxWeaponHit);
            await ETTask.CompletedTask;
        }
    }
}

namespace ET.Client
{
    /// <summary>
    /// AUD-006/007: 武器开火音效（步枪/冲锋枪/霰弹枪/火箭炮根据 WeaponTypeId 区分）
    /// </summary>
    [MessageHandler(SceneType.Client)]
    public class M2C_WeaponFire_AudioHandler : MessageHandler<Scene, M2C_WeaponFire>
    {
        protected override async ETTask Run(Scene root, M2C_WeaponFire message)
        {
            global::ET.WeaponConfig weaponConfig = global::ET.WeaponConfigCategory.Instance.GetOrDefault(message.WeaponId);
            if (weaponConfig != null)
            {
                AudioHelper.PlayWeaponFire(root, weaponConfig.WeaponTypeId);
            }

            await ETTask.CompletedTask;
        }
    }
}

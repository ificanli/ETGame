namespace ET.Client
{
    [MessageHandler(SceneType.Client)]
    public class M2C_SwitchWeaponHandler : MessageHandler<Scene, M2C_SwitchWeapon>
    {
        protected override async ETTask Run(Scene root, M2C_SwitchWeapon message)
        {
            Scene currentScene = root.CurrentScene();
            UnitComponent unitComponent = currentScene?.GetComponent<UnitComponent>();
            if (unitComponent == null)
            {
                return;
            }

            Unit unit = unitComponent.Get(message.UnitId);
            if (unit == null)
            {
                return;
            }

            // 更新武器组件的当前槽位
            WeaponComponent weaponComp = unit.GetComponent<WeaponComponent>();
            if (weaponComp != null)
            {
                weaponComp.SwitchWeapon(message.SlotIndex);
            }

            // 发布武器切换事件，通知UI更新
            EventSystem.Instance.Publish(root, new EventWeaponSwitched
            {
                Scene = root,
                UnitId = message.UnitId,
                SlotIndex = message.SlotIndex
            });

            // TODO: 切换武器模型和动画
            // 这里需要根据 message.WeaponId 获取武器配置，然后切换模型
            // 例如：
            // WeaponConfig weaponConfig = WeaponConfigCategory.Instance.Get(message.WeaponId);
            // 切换角色手持的武器模型
            // 切换动画状态机

            Log.Info($"[M2C_SwitchWeapon] Unit {message.UnitId} switched to weapon slot {message.SlotIndex}, weaponId={message.WeaponId}");

            await ETTask.CompletedTask;
        }
    }
}

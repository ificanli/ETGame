namespace ET.Client
{
    /// <summary>
    /// HUD 武器槽位组件系统
    /// 用于显示和切换武器
    /// </summary>
    public static partial class HUDWeaponSlotComponentSystem
    {
        // TODO: 在 Unity 中创建对应的 UI 组件
        // 需要两个按钮：Weapon1Button 和 Weapon2Button
        // 点击按钮时调用 WeaponSwitchHelper.SwitchWeapon(root, slotIndex)

        /// <summary>
        /// 武器槽位1按钮点击事件
        /// </summary>
        public static void OnWeapon1ButtonClick(Scene root)
        {
            WeaponSwitchHelper.SwitchWeapon(root, 1);
        }

        /// <summary>
        /// 武器槽位2按钮点击事件
        /// </summary>
        public static void OnWeapon2ButtonClick(Scene root)
        {
            WeaponSwitchHelper.SwitchWeapon(root, 2);
        }

        /// <summary>
        /// 更新武器槽位显示
        /// </summary>
        public static void UpdateWeaponSlots(Scene root, int slot1WeaponId, int slot2WeaponId)
        {
            // TODO: 根据武器ID更新UI显示
            // 例如：显示武器图标、名称等
            // WeaponConfig config1 = WeaponConfigCategory.Instance.Get(slot1WeaponId);
            // WeaponConfig config2 = WeaponConfigCategory.Instance.Get(slot2WeaponId);
            // 更新 UI 显示
        }

        /// <summary>
        /// 更新弹药显示
        /// </summary>
        public static void UpdateAmmoDisplay(Scene root, int slot1Ammo, int slot2Ammo)
        {
            // TODO: 更新弹药数量显示
            // 例如：Slot1AmmoText.text = $"{slot1Ammo}";
        }
    }
}

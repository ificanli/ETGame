using System;

namespace ET.Server
{
    /// <summary>
    /// 武器系统测试用例
    /// 用于验证武器配置和系统是否正常工作
    /// </summary>
    [Invoke(TimerInvokeType.WeaponSystemTest)]
    public class WeaponSystemTest : ATimer<Scene>
    {
        protected override void Run(Scene scene)
        {
            try
            {
                Log.Info("========== 武器系统测试开始 ==========");

                // 测试1：验证武器配置加载
                TestWeaponConfigLoad();

                // 测试2：验证装备配置
                TestEquipmentConfig();

                // 测试3：验证物品配置
                TestItemConfig();

                Log.Info("========== 武器系统测试完成 ==========");
            }
            catch (Exception e)
            {
                Log.Error($"武器系统测试失败: {e}");
            }
        }

        private static void TestWeaponConfigLoad()
        {
            Log.Info("--- 测试1：武器配置加载 ---");

            int[] weaponIds = { 50001, 50002, 50003, 50004, 50005, 50006, 50007, 50008, 50009, 50010, 50011 };

            foreach (int weaponId in weaponIds)
            {
                WeaponConfig config = WeaponConfigCategory.Instance.Get(weaponId);
                if (config != null)
                {
                    Log.Info($"✓ 武器 {weaponId} (type={config.WeaponTypeId}) 加载成功");
                    Log.Info($"  - 伤害系数: {config.Damage}%");
                    Log.Info($"  - 射程: {config.AttackRange}m");
                    Log.Info($"  - 弹夹: {config.MagazineSize}");
                    Log.Info($"  - 子弹数: {config.BulletCount}");
                    Log.Info($"  - 散射角: {config.SpreadAngle}°");
                    Log.Info($"  - 锁定类型: {config.FireLockTypeId}");
                }
                else
                {
                    Log.Error($"✗ 武器 {weaponId} 加载失败！");
                }
            }
        }

        private static void TestEquipmentConfig()
        {
            Log.Info("--- 测试2：装备配置验证 ---");

            int[] weaponIds = { 50001, 50002, 50003, 50004, 50005, 50006, 50007, 50008, 50009, 50010, 50011 };

            foreach (int weaponId in weaponIds)
            {
                EquipmentConfig config = EquipmentConfigCategory.Instance.Get(weaponId);
                if (config != null)
                {
                    Log.Info($"✓ 装备 {weaponId} 配置正确，槽位: {config.EquipSlot}");
                    if (config.EquipSlot != 6)
                    {
                        Log.Warning($"  警告：武器槽位应该是 6 (MainHand)，当前是 {config.EquipSlot}");
                    }
                }
                else
                {
                    Log.Error($"✗ 装备 {weaponId} 配置缺失！");
                }
            }
        }

        private static void TestItemConfig()
        {
            Log.Info("--- 测试3：物品配置验证 ---");

            int[] weaponIds = { 50001, 50002, 50003, 50004, 50005, 50006, 50007, 50008, 50009, 50010, 50011 };

            foreach (int weaponId in weaponIds)
            {
                ItemConfig config = ItemConfigCategory.Instance.Get(weaponId);
                if (config != null)
                {
                    Log.Info($"✓ 物品 {weaponId} ({config.Name}) 配置正确");
                }
                else
                {
                    Log.Error($"✗ 物品 {weaponId} 配置缺失！");
                }
            }
        }
    }
}

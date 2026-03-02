using Unity.Mathematics;

namespace ET
{
    [EntitySystemOf(typeof(WeaponComponent))]
    public static partial class WeaponComponentSystem
    {
        [EntitySystem]
        private static void Awake(this WeaponComponent self, int rifleId, int smgId)
        {
            self.RifleId = rifleId;
            self.SMGId = smgId;
            self.CurrentWeapon = WeaponType.Rifle;

            // 初始化弹药（从配置读取，这里先硬编码）
            self.RifleAmmo = 30;  // 步枪30发
            self.SMGAmmo = 25;    // 冲锋枪25发

            self.RifleReloading = false;
            self.SMGReloading = false;
            self.RifleLastFireTime = 0;
            self.SMGLastFireTime = 0;
        }

        [EntitySystem]
        private static void Destroy(this WeaponComponent self)
        {
            self.RifleId = 0;
            self.SMGId = 0;
            self.CurrentWeapon = WeaponType.None;
        }

        /// <summary>
        /// 消耗弹药
        /// </summary>
        public static void ConsumeAmmo(this WeaponComponent self, int weaponId, int count)
        {
            if (weaponId == self.RifleId)
            {
                self.RifleAmmo = math.max(0, self.RifleAmmo - count);
            }
            else if (weaponId == self.SMGId)
            {
                self.SMGAmmo = math.max(0, self.SMGAmmo - count);
            }
        }

        /// <summary>
        /// 补充弹药（换弹）
        /// </summary>
        public static void RefillAmmo(this WeaponComponent self, int weaponId)
        {
            if (weaponId == self.RifleId)
            {
                self.RifleAmmo = 30;  // 恢复到弹匣容量
            }
            else if (weaponId == self.SMGId)
            {
                self.SMGAmmo = 25;
            }
        }

        /// <summary>
        /// 获取当前弹药数
        /// </summary>
        public static int GetAmmo(this WeaponComponent self, int weaponId)
        {
            if (weaponId == self.RifleId)
            {
                return self.RifleAmmo;
            }
            else if (weaponId == self.SMGId)
            {
                return self.SMGAmmo;
            }
            return 0;
        }

        /// <summary>
        /// 检查是否可以射击
        /// </summary>
        public static bool CanFire(this WeaponComponent self, int weaponId)
        {
            // 检查弹药
            if (self.GetAmmo(weaponId) <= 0)
            {
                return false;
            }

            // 检查是否正在换弹
            if (self.IsReloading(weaponId))
            {
                return false;
            }

            // 检查射击间隔（这里先硬编码，后续从配置读取）
            long now = TimeInfo.Instance.ServerNow();
            long lastFireTime = weaponId == self.RifleId ? self.RifleLastFireTime : self.SMGLastFireTime;
            long interval = weaponId == self.RifleId ? 500 : 100; // 步枪0.5秒，冲锋枪0.1秒

            if (now - lastFireTime < interval)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 记录射击时间
        /// </summary>
        public static void RecordFireTime(this WeaponComponent self, int weaponId)
        {
            long now = TimeInfo.Instance.ServerNow();
            if (weaponId == self.RifleId)
            {
                self.RifleLastFireTime = now;
            }
            else if (weaponId == self.SMGId)
            {
                self.SMGLastFireTime = now;
            }
        }

        /// <summary>
        /// 设置换弹状态
        /// </summary>
        public static void SetReloading(this WeaponComponent self, int weaponId, bool reloading)
        {
            if (weaponId == self.RifleId)
            {
                self.RifleReloading = reloading;
            }
            else if (weaponId == self.SMGId)
            {
                self.SMGReloading = reloading;
            }
        }

        /// <summary>
        /// 检查是否正在换弹
        /// </summary>
        public static bool IsReloading(this WeaponComponent self, int weaponId)
        {
            if (weaponId == self.RifleId)
            {
                return self.RifleReloading;
            }
            else if (weaponId == self.SMGId)
            {
                return self.SMGReloading;
            }
            return false;
        }

        /// <summary>
        /// 切换武器
        /// </summary>
        public static void SwitchWeapon(this WeaponComponent self, WeaponType weaponType)
        {
            self.CurrentWeapon = weaponType;
        }
    }
}

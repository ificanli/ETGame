namespace ET
{
    /// <summary>
    /// 武器组件，挂载在 Unit 上。
    /// 管理双武器（步枪+冲锋枪）的弹药、射击间隔、换弹状态。
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class WeaponComponent : Entity, IAwake<int, int>, IDestroy
    {
        /// <summary>步枪配置ID</summary>
        public int RifleId;

        /// <summary>冲锋枪配置ID</summary>
        public int SMGId;

        /// <summary>当前装备的武器</summary>
        public WeaponType CurrentWeapon;

        /// <summary>步枪当前弹药</summary>
        public int RifleAmmo;

        /// <summary>冲锋枪当前弹药</summary>
        public int SMGAmmo;

        /// <summary>步枪是否正在换弹</summary>
        public bool RifleReloading;

        /// <summary>冲锋枪是否正在换弹</summary>
        public bool SMGReloading;

        /// <summary>步枪上次射击时间（毫秒）</summary>
        public long RifleLastFireTime;

        /// <summary>冲锋枪上次射击时间（毫秒）</summary>
        public long SMGLastFireTime;
    }
}

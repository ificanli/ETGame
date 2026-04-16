namespace ET
{
    /// <summary>
    /// 武器类型枚举
    /// </summary>
    public enum WeaponType
    {
        None              = 0,
        Shotgun           = 1,   // 散弹枪：近距离扩散多弹丸
        Rifle1            = 2,   // [已废弃] 旧步枪1号
        Rifle2            = 3,   // [已废弃] 旧步枪2号
        RocketLauncher    = 4,   // [已废弃] 旧火箭炮
        SMG               = 5,   // 冲锋枪：可移动射击，高射速低单发
        AutoRifle         = 6,   // 自动步枪：中距离连发主力
        SniperRifle       = 7,   // 狙击步枪：远距离高伤单发
        Pistol            = 8,   // 手枪：可移动射击，均衡副武器
        GrenadeLauncher   = 9,   // 榴弹炮：位置锁定，范围爆发
        RayGun            = 10,  // 射线枪：持续射线，高射速低伤害
    }
}

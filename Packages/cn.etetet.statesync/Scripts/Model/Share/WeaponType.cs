namespace ET
{
    /// <summary>
    /// 武器类型枚举
    /// </summary>
    public enum WeaponType
    {
        None           = 0,
        Shotgun        = 1,  // 霰弹枪：射程低，一次散射4发，散射高，静止攻击
        Rifle1         = 2,  // 步枪1号：强制目标锁定，射速快，伤害高，静止攻击
        Rifle2         = 3,  // 步枪2号：方向锁定，射程远，攻速快，静止攻击
        RocketLauncher = 4,  // 火箭炮：位置锁定，射程远，静止攻击
    }
}

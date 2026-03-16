namespace ET
{
    /// <summary>
    /// 枪械改造类型，用于 RogueEffectExecuteType.WeaponModifier 的 RefId。
    /// </summary>
    public static class WeaponModType
    {
        public const int AttackRange = 1;       // 攻击距离
        public const int AttackSpeed = 2;       // 攻击速度
        public const int MagazineCapacity = 3;  // 弹匣容量
        public const int ReloadSpeed = 4;       // 换弹速度
        public const int BulletDamage = 5;      // 子弹伤害
        public const int Penetration = 6;       // 穿透
        public const int CritRate = 7;          // 暴击率
        public const int BounceChance = 8;      // 弹射概率
    }
}

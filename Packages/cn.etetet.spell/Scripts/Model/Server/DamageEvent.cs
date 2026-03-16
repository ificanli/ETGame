namespace ET.Server
{
    public struct UnitDie
    {
        public EntityRef<Unit> Unit;
        public EntityRef<Unit> Target;
        public long TargetId;
        public int TargetUnitType;
    }

    /// <summary>
    /// Spell 伤害拦截信息。订阅者设置 Intercepted = true 后，DamageHelper 跳过自身的扣血和死亡逻辑。
    /// </summary>
    [EnableClass]
    public class SpellDamageInterceptInfo
    {
        public bool Intercepted;
    }

    /// <summary>
    /// Spell 伤害即将写入 HP 前发布。订阅者可通过 Info.Intercepted 拦截并接管伤害流程。
    /// </summary>
    public struct BeforeSpellDamageApply
    {
        public EntityRef<Unit> Attacker;
        public EntityRef<Unit> Target;
        public long FinalDamage;
        public int SpellConfigId;
        public SpellDamageInterceptInfo Info;
    }
}

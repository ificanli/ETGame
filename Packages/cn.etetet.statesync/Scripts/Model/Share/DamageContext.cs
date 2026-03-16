namespace ET
{
    /// <summary>
    /// 统一伤害上下文。所有伤害路径（Spell/Bullet）都走这套结构。
    /// Pipeline: BaseDamage → 攻击方增伤 → 防御方减伤 → 致命判定 → 实际扣血 → 后置触发
    /// </summary>
    [EnableClass]
    public class DamageContext
    {
        public long SourceUnitId;
        public long TargetUnitId;
        public int WeaponId;
        public bool IsBullet;
        public bool IsBoss;
        public bool IsCritical;

        /// <summary>基础伤害（未经修正）</summary>
        public long BaseDamage;

        /// <summary>最终伤害（经过增伤/减伤后）</summary>
        public long FinalDamage;

        /// <summary>false 时致命伤害被拦截，目标不会死亡</summary>
        public bool CanDie = true;

        /// <summary>吸血百分比（千分比）</summary>
        public int LifeStealPermille;

        /// <summary>实际造成的伤害（扣血后回写）</summary>
        public long ActualDamage;

        /// <summary>目标是否因本次伤害死亡</summary>
        public bool TargetKilled;

        public void Reset()
        {
            this.SourceUnitId = 0;
            this.TargetUnitId = 0;
            this.WeaponId = 0;
            this.IsBullet = false;
            this.IsBoss = false;
            this.IsCritical = false;
            this.BaseDamage = 0;
            this.FinalDamage = 0;
            this.CanDie = true;
            this.LifeStealPermille = 0;
            this.ActualDamage = 0;
            this.TargetKilled = false;
        }
    }
}

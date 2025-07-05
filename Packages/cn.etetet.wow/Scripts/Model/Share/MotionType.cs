namespace ET
{
    [Module(ModuleName.Spell)]
    public enum MotionType
    {
        None,
        MoveSpeed,
        Idle,
        Run,
        MeleeAttack1,
        MeleeAttack2,
        MeleeAttack3,
        GetHit,
        Dead,
        SpellCastDirected,
        ReadySpellDirected,
        SpellCastOmni,
        ReadySpellOmni,
        Tame,
    }
}
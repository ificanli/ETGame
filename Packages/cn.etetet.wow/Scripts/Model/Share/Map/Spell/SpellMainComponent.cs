namespace ET
{
    [ComponentOf(typeof(Buff))]
    public class SpellMainComponent: Entity, IAwake
    {
        public EntityRef<Spell> Spell { get; set; }
    }
}
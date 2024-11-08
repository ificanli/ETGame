namespace ET
{
    [ComponentOf(typeof(Unit))]
    public class SpellComponent: Entity, IAwake
    {
        public EntityRef<Spell> Current { get; set; }

        public ETCancellationToken CancellationToken { get; set; }
    }
}
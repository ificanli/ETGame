namespace ET
{
    [ComponentOf(typeof(Unit))]
    public class SpellComponent: Entity
    {
        public EntityRef<Spell> Current;

        public ETCancellationToken CancellationToken;
    }
}
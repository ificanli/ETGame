namespace ET.Server
{
    [EntitySystemOf(typeof(BuffSpellModRecordComponent))]
    public static partial class BuffSpellModRecordComponentSystem
    {
        [EntitySystem]
        private static void Awake(this BuffSpellModRecordComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this BuffSpellModRecordComponent self)
        {
            Unit unit = self.Parent.Parent.GetParent<Unit>();
            if (unit.IsDisposed)
            {
                return;
            }
            SpellComponent spellComponent = unit.GetComponent<SpellComponent>();

            for (int i = 0; i < self.Records.Count; i += 3)
            {
                int spellId = self.Records[i];
                SpellModType spellModType = (SpellModType)self.Records[i + 1];
                int value = self.Records[i + 2];
                spellComponent.AddMod(spellId, spellModType, -value);
            }
        }

        public static void Add(this BuffSpellModRecordComponent self, int spellId, SpellModType spellModType, int value)
        {
            self.Records.Add(spellId);
            self.Records.Add((int)spellModType);
            self.Records.Add(value);
        }
    }
}
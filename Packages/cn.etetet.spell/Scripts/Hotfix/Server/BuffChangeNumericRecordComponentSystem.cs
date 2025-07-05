namespace ET.Server
{
    [Module(ModuleName.Spell)]
    [EntitySystemOf(typeof(BuffChangeNumericRecordComponent))]
    public static partial class BuffChangeNumericRecordComponentSystem
    {
        [EntitySystem]
        private static void Awake(this BuffChangeNumericRecordComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this BuffChangeNumericRecordComponent self)
        {
            Unit unit = self.Parent.Parent.GetParent<Unit>();
            if (unit.IsDisposed)
            {
                return;
            }
            NumericComponent numericComponent = unit.GetComponent<NumericComponent>();

            for (int i = 0; i < self.Records.Count; i += 2)
            {
                NumericType numericType = (NumericType)self.Records[i];
                long value = self.Records[i + 1];
                numericComponent.Set(numericType, numericComponent.GetAsLong(numericType) - value);
            }
        }

        public static void Add(this BuffChangeNumericRecordComponent self, NumericType numericType, long value)
        {
            self.Records.Add((int)numericType);
            self.Records.Add(value);
        }
    }
}
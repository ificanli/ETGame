namespace ET
{
    [EntitySystemOf(typeof(BuffData))]
    public static partial class BuffDataSystem
    {
        [EntitySystem]
        private static void Awake(this BuffData self)
        {
            Buff buff = self.GetParent<Buff>();
            buff.BuffData = self;
        }
    }
}
namespace ET.Server
{
    [EntitySystemOf(typeof(BuffUnitComponent))]
    public static partial class BuffUnitComponentSystem
    {
        [EntitySystem]
        private static void Awake(this BuffUnitComponent self)
        {

        }
        [EntitySystem]
        private static void Destroy(this BuffUnitComponent self)
        {
            UnitComponent unitComponent = self.Scene().GetComponent<UnitComponent>();
            if (unitComponent.IsDisposed)
            {
                return;
            }
            
            foreach (long unitId in self.UnitIds)
            {
                unitComponent.Remove(unitId);
            }
        }
    }
}
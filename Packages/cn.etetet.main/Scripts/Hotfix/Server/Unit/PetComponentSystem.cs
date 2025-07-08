namespace ET.Server
{
    [EntitySystemOf(typeof(PetComponent))]
    public static partial class PetComponentSystem
    {
        [EntitySystem]
        private static void Awake(this PetComponent self)
        {

        }
        
        public static Unit GetOwner(this PetComponent self)
        {
            if (self.OwnerId == 0)
            {
                return null;
            }
            Unit pet = self.Parent.GetParent<UnitComponent>().Get(self.OwnerId);
            return pet;
        }
    }
}
namespace ET
{
    [EntitySystemOf(typeof(UnitPetComponent))]
    public static partial class UnitPetComponentSystem
    {
        [EntitySystem]
        private static void Awake(this UnitPetComponent self)
        {
        }
        
        public static Unit GetPet(this UnitPetComponent self)
        {
            if (self.PetId == 0)
            {
                return null;
            }

            Unit pet = self.Parent.GetParent<UnitComponent>().Get(self.PetId);
            return pet;
        }
    }
}
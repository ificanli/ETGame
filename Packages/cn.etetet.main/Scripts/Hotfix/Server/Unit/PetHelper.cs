namespace ET.Server
{
    public static class PetHelper
    {
        public static Unit GetOwner(Unit pet)
        {
            long ownerId = pet.GetComponent<PetComponent>().OwnerId;
            Unit owner = pet.GetParent<UnitComponent>().Get(ownerId);
            return owner;
        }
        
        public static Unit GetPet(Unit unit)
        {
            long petId = unit.GetComponent<UnitPetComponent>().PetId;
            Unit pet = unit.GetParent<UnitComponent>().Get(petId);
            return pet;
        }
    }
}
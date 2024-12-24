namespace ET.Server
{
    public class BTTameBeastHandler: ABTHandler<BTTameBeast>
    {
        protected override int Run(BTTameBeast node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            Unit target = env.GetEntity<Unit>(node.Target);

            UnitPetComponent unitPetComponent = unit.GetComponent<UnitPetComponent>() ?? unit.AddComponent<UnitPetComponent>();
            unitPetComponent.PetId = target.Id;
            target.UnitType = UnitType.Pet;
            target.AddComponent<PetComponent>().OwnerId = unit.Id;
            target.RemoveComponent<AIComponent>();
            target.AddComponent<AIComponent, int>(node.AIConfigId);
            return 0;
        }
    }
}
namespace ET.Server
{
    public class BTTameBeastHandler: ABTHandler<BTTameBeast>
    {
        protected override int Run(BTTameBeast node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            Unit target = env.GetEntity<Unit>(node.Target);
            Buff buff = env.GetEntity<Buff>(node.Buff);

            UnitPetComponent unitPetComponent = unit.GetComponent<UnitPetComponent>() ?? unit.AddComponent<UnitPetComponent>();
            unitPetComponent.PetId = target.Id;
            target.AddComponent<PetComponent>().OwnerId = unit.Id;
            target.GetComponent<AIComponent>().AIConfigId = node.AIConfigId;
            return 0;
        }
    }
}
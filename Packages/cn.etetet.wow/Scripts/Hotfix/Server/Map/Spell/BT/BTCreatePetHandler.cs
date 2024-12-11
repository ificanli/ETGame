namespace ET.Server
{
    public class BTCreatePetHandler: ABTHandler<BTCreatePet>
    {
        protected override int Run(BTCreatePet node, BTEnv env)
        {
            Unit caster = env.GetEntity<Unit>(node.Caster);
            Unit pet = UnitFactory.CreatePet(env.Scene, caster, IdGenerater.Instance.GenerateId(), node.UnitConfigId);
            env.AddEntity(node.Pet, pet);
            return 0;
        }
    }
}
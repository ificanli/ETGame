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
            
            // 清除所有仇恨
            target.GetComponent<ThreatComponent>().ClearThreat();
            // 清除目标
            target.GetComponent<TargetComponent>().Unit = null;
            
            target.AddComponent<PetComponent>().OwnerId = unit.Id;
            BuffHelper.CreateBuff(target, unit.Id, IdGenerater.Instance.GenerateId(), node.AIConfigId, null);
            return 0;
        }
    }
}
using Unity.Mathematics;

namespace ET.Server
{
    public class BTCreateWardHandler : ABTHandler<BTCreateWard>
    {
        protected override int Run(BTCreateWard node, BTEnv env)
        {
            Unit caster = env.GetEntity<Unit>(node.Caster);
            Buff buff = env.GetEntity<Buff>(node.Buff);
            if (caster == null || caster.IsDisposed || buff == null || buff.IsDisposed)
            {
                return 1;
            }

            if (node.UnitConfigId <= 0 || node.VisionRadius <= 0f)
            {
                return 1;
            }

            float3 targetPos;
            if (!env.TryGetStruct(node.TargetPos, out targetPos))
            {
                SpellTargetComponent spellTargetComponent = buff.GetBuffData().GetComponent<SpellTargetComponent>();
                if (spellTargetComponent == null)
                {
                    return 1;
                }

                targetPos = spellTargetComponent.Position;
            }

            Unit wardUnit = UnitFactory.Create(env.Scene, IdGenerater.Instance.GenerateId(), node.UnitConfigId);
            if (wardUnit == null || wardUnit.IsDisposed)
            {
                return 1;
            }

            wardUnit.RemoveComponent<AOIEntity>();

            SyncWardTransform(wardUnit, targetPos);
            SyncWardCamp(wardUnit, caster);
            SyncWardPhase(wardUnit);

            WardComponent wardComponent = wardUnit.AddComponent<WardComponent>();
            wardComponent.OwnerUnitId = caster.Id;
            wardComponent.CampId = caster.GetComponent<CampComponent>()?.CampId ?? 0;
            wardComponent.VisionRadius = node.VisionRadius;

            wardUnit.AddComponent<AOIEntity>();

            BuffUnitComponent buffUnitComponent = buff.GetComponent<BuffUnitComponent>() ?? buff.AddComponent<BuffUnitComponent>();
            buffUnitComponent.UnitIds.Add(wardUnit.Id);

            if (!string.IsNullOrEmpty(node.Unit))
            {
                env.AddEntity(node.Unit, wardUnit);
            }

            return 0;
        }

        private static void SyncWardTransform(Unit wardUnit, float3 targetPos)
        {
            wardUnit.Position = targetPos;
            wardUnit.Rotation = quaternion.identity;
        }

        private static void SyncWardCamp(Unit wardUnit, Unit caster)
        {
            CampComponent casterCamp = caster.GetComponent<CampComponent>();
            if (casterCamp == null)
            {
                return;
            }

            CampComponent wardCamp = wardUnit.GetComponent<CampComponent>();
            if (wardCamp != null)
            {
                wardUnit.RemoveComponent<CampComponent>();
            }

            wardUnit.AddComponent<CampComponent, int>(casterCamp.CampId);
        }

        private static void SyncWardPhase(Unit wardUnit)
        {
            NumericComponent numericComponent = wardUnit.NumericComponent;
            if (numericComponent == null)
            {
                return;
            }

            numericComponent.SetNoEvent(NumericType.Phase, 0);
        }
    }
}

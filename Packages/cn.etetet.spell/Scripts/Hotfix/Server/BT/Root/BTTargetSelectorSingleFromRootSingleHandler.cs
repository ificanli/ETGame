using Unity.Mathematics;

namespace ET.Server
{
    public class BTTargetSelectorSingleFromRootSingleHandler: ABTHandler<TargetSelectorSingleFromRootSingle>
    {
        protected override int Run(TargetSelectorSingleFromRootSingle node, BTEnv env)
        {
            Scene scene = env.Scene;
            Buff buff = env.GetEntity<Buff>(node.Buff);
            Buff rootBuff = buff.GetComponent<BuffParentComponent>().RootBuff;
            SpellTargetComponent rootSpellTargetComponent = rootBuff.GetComponent<SpellTargetComponent>();

            Unit caster = buff.GetCaster();
            Unit target = scene.GetComponent<UnitComponent>().Get(rootSpellTargetComponent.Units[0]);
            if (target == null)
            {
                return TextConstDefine.SpellCast_NotSelectTarget;
            }

            if (node.MaxDistance > 0 && math.distance(target.Position, caster.Position) > node.MaxDistance)
            {
                return TextConstDefine.SpellCast_TargetTooFar;
            }
            
            buff.GetComponent<SpellTargetComponent>().Units.Add(target.Id);

            env.AddEntity(node.Unit, target);
            return 0;
        }
    }
}
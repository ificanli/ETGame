using Unity.Mathematics;

namespace ET.Client
{
    [Event(SceneType.Current)]
    [Module(ModuleName.Spell)]
    public class OnSpellTriggerEvent: AEvent<Scene, OnSpellTrigger>
    {
        protected override async ETTask Run(Scene scene, OnSpellTrigger args)
        {
            int spellConfigId = args.SpellConfigId;
            SpellConfig spellConfig = SpellConfigCategory.Instance.Get(spellConfigId);
            
            C2M_SpellCast c2MSpellCast = C2M_SpellCast.Create();
            c2MSpellCast.SpellConfigId = spellConfigId;
            
            Unit unit = args.Unit;
            EntityRef<Unit> unitRef = unit;
            // 这里根据技能目标选择方式，等待客户端目标选择
            await TargetSelectDispatcher.Instance.Handle(spellConfig.TargetSelector, unit, spellConfig);

            unit = unitRef;
            TargetComponent targetComponent = unit.GetComponent<TargetComponent>();
            c2MSpellCast.TargetPosition = targetComponent.Position;
            SpellHelper.Cast(unit, c2MSpellCast);
        }
    }
}
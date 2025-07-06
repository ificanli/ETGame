namespace ET.Client
{
    [Module(ModuleName.Spell)]
    public class TargetSelectorPositionHandler: TargetSelectHandler<TargetSelectorPosition>
    {
        protected override async ETTask<int> Run(TargetSelectorPosition node, Unit unit, SpellConfig spellConfig)
        {
            // 创建技能指示器
            SpellIndicatorComponent spellIndicatorComponent = unit.GetComponent<SpellIndicatorComponent>();
            TargetComponent targetComponent = unit.GetComponent<TargetComponent>();
            targetComponent.Position = await spellIndicatorComponent.WaitSpellIndicator(node);
            return 0;
        }
    }
}
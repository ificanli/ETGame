using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Client
{
    [Module(ModuleName.Spell)]
    public class TargetSelectorCircleHandler : TargetSelectHandler<TargetSelectorCircle>
    {
        protected override async ETTask<int> Run(TargetSelectorCircle node, Unit unit, SpellConfig spellConfig)
        {
            // 创建技能指示器
            SpellIndicatorComponent spellIndicatorComponent = unit.GetComponent<SpellIndicatorComponent>();
            TargetComponent targetComponent = unit.GetComponent<TargetComponent>();
            targetComponent.Position = await spellIndicatorComponent.WaitSpellIndicator(node);
            return 0;
        }
    }
}
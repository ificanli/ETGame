using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Client
{
    public class TargetSelectorCasterCircleHandler : TargetSelectHandler<TargetSelectorCasterCircle>
    {
        protected override async ETTask<int> Run(TargetSelectorCasterCircle node, Unit unit, SpellConfig spellConfig)
        {
            await ETTask.CompletedTask;
            return 0;
        }
    }
}
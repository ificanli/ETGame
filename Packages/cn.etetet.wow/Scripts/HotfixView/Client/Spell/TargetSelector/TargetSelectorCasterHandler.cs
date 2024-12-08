using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Client
{
    public class TargetSelectorCasterHandler : TargetSelectHandler<TargetSelectorCaster>
    {
        protected override async ETTask<int> Run(TargetSelectorCaster node, Unit unit, SpellConfig spellConfig)
        {
            await ETTask.CompletedTask;
            return 0;
        }
    }
}
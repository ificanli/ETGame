using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Client
{
    public class TSTargetSelectorSingleHandler : TargetSelectHandler<TargetSelectorSingle>
    {
        protected override async ETTask<int> Run(TargetSelectorSingle node, Unit unit, SpellConfig spellConfig)
        {
            await ETTask.CompletedTask;
            return 0;
        }
    }
}
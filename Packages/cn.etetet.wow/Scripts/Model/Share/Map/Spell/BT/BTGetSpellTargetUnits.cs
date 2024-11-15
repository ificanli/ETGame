using System.Collections.Generic;

namespace ET
{
    public class BTGetSpellTargetUnits: BTNode
    {
        [BTInput(typeof(Spell))]
        public string Spell;

        [BTOutput(typeof(List<EntityRef<Unit>>))]
        public string Units;
    }
}
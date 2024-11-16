using System.Collections.Generic;

namespace ET
{
    public class BTGetSpellCaster: BTNode
    {
        [BTInput(typeof(Spell))]
#if UNITY
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
#endif
        public string Spell;

        [BTOutput(typeof(Unit))]
#if UNITY
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
#endif
        public string Unit;
    }
}
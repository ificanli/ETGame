using System.Collections.Generic;
using Unity.Mathematics;

namespace ET
{
    public abstract class TargetSelector : BTRoot
    {
        //[Sirenix.OdinInspector.ReadOnly]
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        [BTOutput(typeof(Buff))]
        public string Buff = "Buff";
        
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        [BTOutput(typeof(Unit))]
        public string Caster = "Caster";
        
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        [BTOutput(typeof(Unit))]
        public string Owner = "Owner";

        public int MaxDistance;

        public int MinDistance;
    }
    

    

    

    

    

}
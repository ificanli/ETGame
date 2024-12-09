using System.Collections.Generic;
using Unity.Mathematics;

namespace ET
{
    public abstract class TargetSelector : BTNode
    {
        //[Sirenix.OdinInspector.ReadOnly]
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Buff))]
        public string Buff = BTEvnKey.Buff;
        
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTOutput(typeof(Unit))]
        public string Caster = BTEvnKey.Caster;
        
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTOutput(typeof(Unit))]
        public string Owner = BTEvnKey.Owner;

        public int MaxDistance;

        public int MinDistance;
    }
    

    

    

    

    

}
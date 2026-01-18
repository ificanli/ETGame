using Sirenix.OdinInspector;

namespace ET
{
    //[LabelText("效果Buff每帧 (服务器)")]
    //[HideReferenceObjectPicker]
    public class EffectServerBuffTick: EffectNode
    {
        [BTOutput(typeof(Buff))]
        [ReadOnly]
        [BoxGroup("输出参数")]
        public string Buff = "Buff";
        
        [BTOutput(typeof(Unit))]
        [BoxGroup("输出参数")]
        public string Unit = "Unit";
        
        // 用来允许BTAsyncNode作为孩子
        [BTOutput(typeof(BTAsyncNode))]
        [BoxGroup("输出参数")]
        public string RootMustBeBuffTick = "RootMustBeBuffTick";

        public bool Override;
    }
}
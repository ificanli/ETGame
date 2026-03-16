using MongoDB.Bson.Serialization.Attributes;
using Sirenix.OdinInspector;

namespace ET
{
    //[LabelText("效果Buff每帧 (服务器)")]
    //[HideReferenceObjectPicker]
    public class EffectServerBuffTick: EffectNode
    {
        [BTOutput(typeof(Buff))]
        //[ReadOnly]
        [BoxGroup("输出参数")]
        public string Buff = "Buff";

        [BTOutput(typeof(Unit))]
        [BoxGroup("输出参数")]
        public string Unit = "Unit";

        [BTOutput(typeof(Unit))]
        [BoxGroup("输出参数")]
        public string Caster = "Caster";
        
#if UNITY_EDITOR
        [HideIf("@true")]
        [BsonIgnore]
        // 用来允许BTCoroutine作为孩子
        [BTOutput(typeof(BTCoroutine))]
        [BoxGroup("输出参数")]
        public string RootMustBeBuffTick = "RootMustBeBuffTick";
#endif

        public bool Override;
    }
}

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
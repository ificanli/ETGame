using MongoDB.Bson.Serialization.Attributes;
using Sirenix.OdinInspector;

namespace ET
{
    [BTNodeTitleColor(0f, 0.5f, 0f, 1f)]
    public abstract class BTCoroutine: BTNode
    {
        [BoxGroup("输入参数")]
        [BTInput(typeof(Buff))]
        [LabelWidth(100)]
        public string Buff = "Buff";
        
#if UNITY_EDITOR
        [HideIf("@true")]
        [BsonIgnore]
        // 用来防止这种节点挂到非BuffTick上
        [BTInput(typeof(BTCoroutine))]
        [BoxGroup("输入参数")]
        public string RootMustBeBuffTick = "RootMustBeBuffTick";
#endif
    }
}
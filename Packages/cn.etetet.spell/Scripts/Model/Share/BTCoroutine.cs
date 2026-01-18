using Sirenix.OdinInspector;

namespace ET
{
    public abstract class BTCoroutine: BTNode
    {
        [BoxGroup("输入参数")]
        [BTInput(typeof(Buff))]
        [LabelWidth(100)]
        public string Buff = "Buff";
        
        // 用来防止这种节点挂到非BuffTick上
        [BTInput(typeof(BTCoroutine))]
        [BoxGroup("输入参数")]
        public string RootMustBeBuffTick = "RootMustBeBuffTick";
    }
}
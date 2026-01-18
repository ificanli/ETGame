using Sirenix.OdinInspector;

namespace ET
{
    public abstract class AINode: BTNode
    {
        [BoxGroup("输入参数")]
        [BTInput(typeof(Unit))]
        [LabelWidth(100)]
        public string Unit = "Unit";

        [BoxGroup("输入参数")]
        [BTOutput(typeof(Buff))]
        [LabelWidth(100)]
        public string Buff = "Buff";
    }
}
namespace ET
{
    public class BTGetBuffOwner: BTNode
    {
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Buff))]
        public string Buff;
        
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        [BTInput(typeof(Unit))]
        public string Unit;
    }
}
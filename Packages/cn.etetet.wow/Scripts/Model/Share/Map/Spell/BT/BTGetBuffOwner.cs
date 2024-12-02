namespace ET
{
    public class BTGetBuffOwner: BTNode
    {
#if UNITY
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
#endif
        [BTInput(typeof(Buff))]
        public string Buff;
        
#if UNITY
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
#endif
        [BTInput(typeof(Unit))]
        public string Unit;
    }
}
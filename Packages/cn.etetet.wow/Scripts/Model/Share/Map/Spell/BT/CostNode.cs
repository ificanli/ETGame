namespace ET
{
    public abstract class CostNode: BTNode
    {
#if UNITY
        [Sirenix.OdinInspector.ReadOnly]
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
#endif
        [BTInput(typeof(Buff))]
        public string Buff = BTEvnKey.Buff;
    }
}
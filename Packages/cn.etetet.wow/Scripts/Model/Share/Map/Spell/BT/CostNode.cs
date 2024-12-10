namespace ET
{
    public abstract class CostNode: BTNode
    {
#if UNITY
        //[Sirenix.OdinInspector.ReadOnly]
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
#endif
        [BTInput(typeof(Buff))]
        public string Buff = BTEvnKey.Buff;
        
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        [BTOutput(typeof(Unit))]
        public string Caster = BTEvnKey.Caster;
        
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        [BTOutput(typeof(Unit))]
        public string Owner = BTEvnKey.Owner;
    }
}
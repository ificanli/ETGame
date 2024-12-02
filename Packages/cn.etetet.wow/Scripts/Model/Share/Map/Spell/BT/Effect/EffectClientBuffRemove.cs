namespace ET
{
    public class EffectClientBuffRemove: EffectNode
    {
        [BTOutput(typeof(Buff))]
#if UNITY
        [Sirenix.OdinInspector.ReadOnly]
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
#endif
        public string Buff = BTEvnKey.Buff;
    }
}
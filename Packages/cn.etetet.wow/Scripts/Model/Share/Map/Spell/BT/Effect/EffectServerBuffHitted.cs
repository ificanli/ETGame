namespace ET
{
    // Buff被攻击的时候触发
    public class EffectServerBuffHitted: EffectNode
    {
        [BTOutput(typeof(Buff))]
#if UNITY
        [Sirenix.OdinInspector.ReadOnly]
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
#endif
        public string Buff = BTEvnKey.Buff;
        
        // 攻击者
        public string Unit = BTEvnKey.Unit;
    }
}
namespace ET
{
    public class BTRootServerBuffTick: BTNode
    {
        [BTOutput(typeof(Buff))]
#if UNITY
        [Sirenix.OdinInspector.ReadOnly]
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
#endif
        public string Buff = BTEvnKey.Buff;
    }
}
namespace ET
{
    public class BTAddFakeBulletBuff: BTNode
    {
#if UNITY
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
#endif
        [BTInput(typeof(Unit))]
        public string Caster;
        
#if UNITY
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
#endif
        [BTInput(typeof(Unit))]
        public string Target;
        
#if UNITY
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
#endif
        [BTInput(typeof(Buff))]
        public string Buff;
        
#if UNITY
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
#endif
        [BTInput(typeof(Buff))]
        public string OutputBuff;

        public int ConfigId;

        public int Speed;
    }
}
namespace ET
{
    [System.Serializable]
    public class BTAddFakeBulletBuff: BTAction
    {
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Unit))]
        public string Caster;
        
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Unit))]
        public string Target;
        
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Buff))]
        public string Buff;
        
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        [BTInput(typeof(Buff))]
        public string OutputBuff;

        public int ConfigId;

        public int Speed;
    }
}
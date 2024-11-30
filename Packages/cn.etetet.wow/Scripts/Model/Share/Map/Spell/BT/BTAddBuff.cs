namespace ET
{
    public class BTAddBuff: BTNode
    {
#if UNITY
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
#endif
        [BTInput(typeof(Unit))]
        public string Unit;
        
#if UNITY
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
#endif
        [BTInput(typeof(Spell))]
        public string Spell;
        
        public int ConfigId;
    }
}
namespace ET
{
    public class BTCreateSpell : BTNode
    {
#if UNITY
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
#endif
        [BTInput(typeof(Unit))]
        public string Unit;
        
#if UNITY
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
#endif
        [BTInput(typeof(Buff))]
        public string Buff;
        
        public int SpellConfigId;
    }
}
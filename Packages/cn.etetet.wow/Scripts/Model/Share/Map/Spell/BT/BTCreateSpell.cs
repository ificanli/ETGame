namespace ET
{
    public class BTCreateSpell : BTNode
    {
#if UNITY
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
#endif
        [BTInput(typeof(Unit))]
        public string Unit;
        
        public int SpellConfigId;
    }
}
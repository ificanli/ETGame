namespace ET
{
    public class BTSpellMod : BTAction
    {
        [BTInput(typeof(Unit))]
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        public string Unit;
        
        [BTInput(typeof(Buff))]
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        public string Buff;
        
        public int SpellId;
        public SpellModType SpellModType;
        public int Value;
    }
}
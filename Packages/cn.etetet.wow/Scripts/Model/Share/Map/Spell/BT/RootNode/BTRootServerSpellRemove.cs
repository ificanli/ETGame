namespace ET
{
    public class BTRootServerSpellRemove: BTNode
    {
        [BTOutput(typeof(Spell))]
#if UNITY
        [Sirenix.OdinInspector.ReadOnly]
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
#endif
        public string Spell = BTEvnKey.Spell;
    }
}
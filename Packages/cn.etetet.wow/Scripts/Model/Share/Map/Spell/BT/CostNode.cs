namespace ET
{
    public abstract class CostNode: BTNode
    {
#if UNITY
        [Sirenix.OdinInspector.ReadOnly]
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
#endif
        [BTInput(typeof(Spell))]
        public string Spell = BTEvnKey.Spell;
    }
}
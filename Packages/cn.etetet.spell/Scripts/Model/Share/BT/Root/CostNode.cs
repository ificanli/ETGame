namespace ET
{
    [Module(ModuleName.Spell)]
    public class CostNode: BTRoot
    {
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        [BTOutput(typeof(Buff))]
        public string Buff = "Buff";
        
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        [BTOutput(typeof(Unit))]
        public string Caster = "Caster";
        
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        [BTOutput(typeof(Unit))]
        public string Owner = "Owner";

        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        [BTOutput(typeof(bool))]
        public string Check = "Check";
    }
}
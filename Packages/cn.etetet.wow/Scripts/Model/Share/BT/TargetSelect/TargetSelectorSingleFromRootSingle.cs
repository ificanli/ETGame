namespace ET
{
    [System.Serializable]
    [Module(ModuleName.Spell)]
    public class TargetSelectorSingleFromRootSingle : TargetSelector
    {
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        [BTOutput(typeof(Unit))]
        public string Unit = "Unit";
    }
}
namespace ET
{
    [System.Serializable]
    public class TargetSelectorSingle : TargetSelector
    {
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        [BTOutput(typeof(Unit))]
        public string Unit = "Unit";
        
        public UnitType UnitType;
    }
}
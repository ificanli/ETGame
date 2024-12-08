namespace ET
{
    [System.Serializable]
    public class TargetSelectorSingle : TargetSelector
    {
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        [BTOutput(typeof(Unit))]
        public string Unit = BTEvnKey.Unit;
        
        public float MaxDistance;
        
        public UnitType UnitType;
    }
}
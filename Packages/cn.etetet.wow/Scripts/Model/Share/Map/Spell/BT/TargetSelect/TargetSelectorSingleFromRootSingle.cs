namespace ET
{
    [System.Serializable]
    public class TargetSelectorSingleFromRootSingle : TargetSelector
    {
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        [BTOutput(typeof(Unit))]
        public string Unit = BTEvnKey.Unit;
        
        public float MaxDistance;
    }
}
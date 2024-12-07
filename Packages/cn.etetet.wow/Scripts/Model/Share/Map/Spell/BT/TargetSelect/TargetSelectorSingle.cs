namespace ET
{
    [System.Serializable]
    public class TargetSelectorSingle : TargetSelector
    {
        
#if UNITY
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
#endif
        [BTOutput(typeof(Unit))]
        public string Unit = BTEvnKey.Unit;
        
#if UNITY
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
#endif
        [BTOutput(typeof(Buff))]
        public string Buff = BTEvnKey.Buff;
        
        public float MaxDistance;
        
        public UnitType UnitType;
    }
}
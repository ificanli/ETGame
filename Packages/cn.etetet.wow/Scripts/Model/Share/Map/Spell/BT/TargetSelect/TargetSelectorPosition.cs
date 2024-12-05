namespace ET
{
    [System.Serializable]
    public class TargetSelectorPosition : TargetSelector
    {
#if UNITY
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
#endif
        [BTOutput(typeof(Unit))]
        public string Pos = BTEvnKey.Pos;
        
#if UNITY
        public UnityEngine.GameObject SpellIndicator;
#endif
        public int MaxDistance;

        public int Radius;
    }
}
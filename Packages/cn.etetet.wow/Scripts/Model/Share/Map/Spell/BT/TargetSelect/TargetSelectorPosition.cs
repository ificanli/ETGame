namespace ET
{
    [System.Serializable]
    public class TargetSelectorPosition : TargetSelector
    {
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        [BTOutput(typeof(Unit))]
        public string Pos = BTEvnKey.Pos;
        
#if UNITY
        public UnityEngine.GameObject SpellIndicator;
#endif
        public int MaxDistance;

        public int Radius;
    }
}
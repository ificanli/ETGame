namespace ET
{
    [System.Serializable]
    public class TargetSelectorPosition : TargetSelector
    {
#if UNITY
        public UnityEngine.GameObject SpellIndicator;
#endif
        public int MaxDistance;
    }
}
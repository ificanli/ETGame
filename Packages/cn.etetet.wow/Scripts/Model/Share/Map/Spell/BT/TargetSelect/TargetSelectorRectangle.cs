namespace ET
{
    [System.Serializable]
    public class TargetSelectorRectangle : TargetSelector
    {
#if UNITY
        public UnityEngine.GameObject SpellIndicator;
#endif
        public float Width;
        public float Length;
        public int MaxDistance;
    }
}
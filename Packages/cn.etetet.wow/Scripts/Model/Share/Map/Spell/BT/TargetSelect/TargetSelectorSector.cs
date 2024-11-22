namespace ET
{
    [System.Serializable]
    public class TargetSelectorSector : TargetSelector
    {
#if UNITY
        public UnityEngine.GameObject SpellIndicator;
#endif
        public int Radius;
        public int Angle;
        public int MaxDistance;
    }
}
namespace ET
{
    [EnableClass]
    public abstract class TargetSelector
    {
    }

    [System.Serializable]
    public class TargetSelectorSingle : TargetSelector
    {
        public float MaxDistance;
    }
    
    [System.Serializable]
    public class TargetSelectorCircle : TargetSelector
    {
#if UNITY
        public UnityEngine.GameObject SpellIndicator;
#endif
        public int Radius;
        public int MaxDistance;
    }
    
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
    
    [System.Serializable]
    public class TargetSelectorCaster : TargetSelector
    {
    }
    
    [System.Serializable]
    public class TargetSelectorPosition : TargetSelector
    {
#if UNITY
        public UnityEngine.GameObject SpellIndicator;
#endif
        public int MaxDistance;
    }
    
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
namespace ET
{
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
        public float Radius;
    }
    
    [System.Serializable]
    public class TargetSelectorSector : TargetSelector
    {
        public float Radius;
        public int Angle;
    }
    
    [System.Serializable]
    public class TargetSelectorCaster : TargetSelector
    {
    }
    
    [System.Serializable]
    public class TargetSelectorPosition : TargetSelector
    {
    }
    
    [System.Serializable]
    public class TargetSelectorRectangle : TargetSelector
    {
    }
}
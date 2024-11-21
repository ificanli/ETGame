using System.Collections.Generic;
using Unity.Mathematics;

namespace ET
{
    public abstract class TargetSelector : BTNode
    {
#if UNITY
        [Sirenix.OdinInspector.ReadOnly]
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
#endif
        [BTInput(typeof(Spell))]
        public string Spell = BTEvnKey.Spell;
    }
     
    [System.Serializable]
    public class TargetSelectorSingle : TargetSelector
    {
        
#if UNITY
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
#endif
        [BTOutput(typeof(Unit))]
        public string Unit = BTEvnKey.Unit;
        
        public float MaxDistance;
    }
    
    [System.Serializable]
    public class TargetSelectorCircle : TargetSelector
    {
#if UNITY
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
#endif
        [BTOutput(typeof(Unit))]
        public string Unit = BTEvnKey.Unit;
        
        
#if UNITY
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
#endif
        [BTOutput(typeof(List<Unit>))]
        public string Units = BTEvnKey.Units;
        
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
using System.Collections.Generic;

namespace ET
{
    [System.Serializable]
    public class TargetSelectorCircle : TargetSelector
    {
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        [BTOutput(typeof(Unit))]
        public string Unit = BTEvnKey.Unit;
        
        
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        [BTOutput(typeof(List<long>))]
        public string Units = BTEvnKey.Units;
        
#if UNITY
        public UnityEngine.GameObject SpellIndicator;
#endif
        
        public int Radius;
        
        public UnitType UnitType;
    }
}
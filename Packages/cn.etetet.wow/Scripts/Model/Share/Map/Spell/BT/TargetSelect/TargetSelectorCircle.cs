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
        
        public UnityEngine.GameObject SpellIndicator;
        
        public int Radius;
        public int MaxDistance;

        public UnitType UnitType;
    }
}
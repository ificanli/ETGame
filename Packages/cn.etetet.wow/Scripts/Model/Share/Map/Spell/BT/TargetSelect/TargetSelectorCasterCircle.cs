using System.Collections.Generic;

namespace ET
{
    [System.Serializable]
    public class TargetSelectorCasterCircle : TargetSelector
    {
#if UNITY
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
#endif
        [BTOutput(typeof(Unit))]
        public string Unit = BTEvnKey.Unit;
        
#if UNITY
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
#endif
        [BTOutput(typeof(List<EntityRef<Unit>>))]
        public string Units = BTEvnKey.Units;
       
        public int Radius;
        
        public UnitType UnitType;
    }
}
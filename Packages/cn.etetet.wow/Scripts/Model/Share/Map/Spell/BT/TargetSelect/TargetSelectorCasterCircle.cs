using System.Collections.Generic;

namespace ET
{
    [System.Serializable]
    public class TargetSelectorCasterCircle : TargetSelector
    {
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        [BTOutput(typeof(Unit))]
        public string Unit = "Unit";
        
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        [BTOutput(typeof(List<EntityRef<Unit>>))]
        public string Units = "Units";
       
        public int Radius;
        
        public UnitType UnitType;
    }
}
using Sirenix.OdinInspector;
using System.Collections.Generic;

namespace ET
{
    [System.Serializable]
    public class TargetSelectorSector : TargetSelector
    {
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        [BTOutput(typeof(Unit))]
        public string Unit = "Unit";

        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        [BTOutput(typeof(List<long>))]
        public string Units = "Units";

        [InlineProperty]
        [HideReferenceObjectPicker]
        [LabelText("技能指示器")]
        public OdinUnityObject SpellIndicator = new();

        public int Radius;

        public int Angle;

        public UnitType UnitType;
    }
}

using Sirenix.OdinInspector;

namespace ET
{
    [System.Serializable]
    [Module(ModuleName.Spell)]
    public class TargetSelectorSector : TargetSelector
    {
        [InlineProperty] // 去掉折叠和标题
        [HideReferenceObjectPicker]
        [LabelText("技能指示器")]
        public OdinUnityObject SpellIndicator = new();
        
        public int Radius;
        public int Angle;
        
        public UnitType UnitType;
    }
}
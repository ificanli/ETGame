using Sirenix.OdinInspector;

namespace ET
{
    [Module(ModuleName.Spell)]
    public class BTAnimatorSetInt: BTAction
    {
        [BTInput(typeof(Unit))]
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        public string Unit;
        
        [LabelWidth(80)]
        public MotionType MotionType;
        public int Value;
    }
}
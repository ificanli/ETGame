namespace ET
{
    public class BTAnimatorSetBool: BTAction
    {
        [BTInput(typeof(Unit))]
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        public string Unit;
        
        public MotionType MotionType;
        public bool Value;
    }
}
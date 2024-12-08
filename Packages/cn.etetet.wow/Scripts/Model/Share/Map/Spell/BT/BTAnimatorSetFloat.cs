namespace ET
{
    public class BTAnimatorSetFloat: BTNode
    {
        [BTInput(typeof(Unit))]
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        public string Unit;
        
        public MotionType MotionType;
        public float Value;
    }
}
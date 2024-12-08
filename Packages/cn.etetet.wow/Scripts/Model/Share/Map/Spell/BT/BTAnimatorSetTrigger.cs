namespace ET
{
    public class BTAnimatorSetTrigger: BTNode
    {
        [BTInput(typeof(Unit))]
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        public string Unit;
        
        public MotionType MotionType;
    }
}
namespace ET
{
    public class BTAnimatorSetTrigger: BTNode
    {
        [BTInput(typeof(Unit))]
        public string Unit;
        
        public MotionType MotionType;
    }
}
namespace ET
{
    public class BTAnimatorSetFloat: BTNode
    {
        [BTInput(typeof(Unit))]
        public string Unit;
        
        public MotionType MotionType;
        public float Value;
    }
}
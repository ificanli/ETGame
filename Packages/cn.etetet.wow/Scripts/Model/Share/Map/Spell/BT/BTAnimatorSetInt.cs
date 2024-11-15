namespace ET
{
    public class BTAnimatorSetInt: BTNode
    {
        [BTInput(typeof(Unit))]
        public string Unit;
        
        public MotionType MotionType;
        public int Value;
    }
}
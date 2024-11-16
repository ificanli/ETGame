namespace ET
{
    public class BTAnimatorSetTrigger: BTNode
    {
        [BTInput(typeof(Unit))]
#if UNITY
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
#endif
        public string Unit;
        
        public MotionType MotionType;
    }
}
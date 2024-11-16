namespace ET
{
    public class BTNumericChange : BTNode
    {
        [BTInput(typeof(Unit))]
#if UNITY
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
#endif
        public string Unit;
        
        public NumericType NumericType;
        public int Value;
    }
}
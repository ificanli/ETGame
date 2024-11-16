namespace ET
{
    public class BTBuffNumericChange : BTNode
    {
        [BTInput(typeof(Buff))]
#if UNITY
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
#endif
        public string Buff;
        
        public NumericType NumericType;
        public int Value;
    }
}
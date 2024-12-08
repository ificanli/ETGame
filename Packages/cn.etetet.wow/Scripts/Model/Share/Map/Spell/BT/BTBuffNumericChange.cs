namespace ET
{
    public class BTBuffNumericChange : BTNode
    {
        [BTInput(typeof(Buff))]
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        public string Buff;
        
        public NumericType NumericType;
        public int Value;
    }
}
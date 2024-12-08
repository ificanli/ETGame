namespace ET
{
    public class BTNumericChange : BTNode
    {
        [BTInput(typeof(Unit))]
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        public string Unit;
        
        public NumericType NumericType;
        public int Value;
    }
}
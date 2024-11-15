namespace ET
{
    public class BTBuffNumericChange : BTNode
    {
        [BTInput(typeof(Buff))]
        public string Buff;
        
        public NumericType NumericType;
        public int Value;
    }
}
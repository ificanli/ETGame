namespace ET
{
    [Module(ModuleName.Spell)]
    public class BTNumericChange : BTAction
    {
        [BTInput(typeof(Unit))]
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        public string Unit;
        
        [BTInput(typeof(Buff))]
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        public string Buff;
        
        public NumericType NumericType;
        public int Value;
    }
}
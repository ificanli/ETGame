namespace ET
{
    [Module(ModuleName.Spell)]
    public class BTCostNumeric: BTAction
    {
        [BTInput(typeof(Unit))]
        public string Caster = "Caster";
        
        [BTInput(typeof(bool))]
        public string Check;
        
        public NumericType NumericType;
        
        public long Value;
    }
}
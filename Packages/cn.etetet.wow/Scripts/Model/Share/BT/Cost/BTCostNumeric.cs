namespace ET
{
    [Module(ModuleName.Spell)]
    public class BTCostNumeric: BTAction
    {
        [BTInput(typeof(Unit))]
        public string Caster = "Caster";
        
        [BTInput(typeof(bool))]
        public string Check;
        
#if UNITY_EDITOR
        [Sirenix.OdinInspector.ValueDropdown(nameof(GetNumericTypeOptions))]
        [Sirenix.OdinInspector.LabelText("数值类型")]
#endif
        public int NumericType;
        
        public long Value;
        
#if UNITY_EDITOR
        private static System.Collections.Generic.IEnumerable<Sirenix.OdinInspector.ValueDropdownItem<int>> GetNumericTypeOptions()
        {
            var options = new System.Collections.Generic.List<Sirenix.OdinInspector.ValueDropdownItem<int>>();
            
            // 反射获取NumericType的所有const字段
            var fields = typeof(NumericType).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            
            foreach (var field in fields)
            {
                if (field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(int))
                {
                    int value = (int)field.GetValue(null);
                    string name = field.Name;
                    options.Add(new Sirenix.OdinInspector.ValueDropdownItem<int>($"{name} ({value})", value));
                }
            }
            
            // 按值排序
            options.Sort((a, b) => a.Value.CompareTo(b.Value));
            
            return options;
        }
#endif
    }
}
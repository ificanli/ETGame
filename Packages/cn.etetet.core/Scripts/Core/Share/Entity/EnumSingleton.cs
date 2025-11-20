using System;
using System.Reflection;

namespace ET
{
    public class EnumSingleton<T>: Singleton<T> where T: EnumSingleton<T>
    {
        protected readonly DoubleMap<int, string> enumValueString = new();
        
        protected void Init(Type type)
        {
            FieldInfo[] fieldInfos = type.GetFields(BindingFlags.Static | BindingFlags.Public);
            foreach (FieldInfo fieldInfo in fieldInfos)
            {
                if (fieldInfo.FieldType != typeof(int))
                {
                    continue;
                }
                this.enumValueString.Add((int)fieldInfo.GetValue(null), fieldInfo.Name);	
            }
        }
        
        public string GetStringByValue(int value)
        {
            return this.enumValueString.GetValueByKey(value);
        }
        
        public int GetValueByName(string name)
        {
            return this.enumValueString.GetKeyByValue(name);
        }
    }
}
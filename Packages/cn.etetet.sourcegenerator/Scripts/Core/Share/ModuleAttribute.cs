using System;

namespace ET
{
    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    public class ModuleAttribute : Attribute
    {
        public string Name { get; }
        public ModuleAttribute(string name)
        {
            Name = name;
        }
    }
}
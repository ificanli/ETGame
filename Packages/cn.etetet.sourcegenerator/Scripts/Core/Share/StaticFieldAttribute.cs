using System;

namespace ET
{
    /// <summary>
    /// 静态字段需加此标签
    /// valueToAssign: 初始化时的字段值
    /// assignNewTypeInstance: 从默认构造函数初始化
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class StaticFieldAttribute: Attribute
    {
    }
}


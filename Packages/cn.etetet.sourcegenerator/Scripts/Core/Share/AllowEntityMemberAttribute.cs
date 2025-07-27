using System;

namespace ET
{
    // 在您的项目中添加以下特性，用于标记允许引用 Entity 的字段或属性：
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class AllowEntityMemberAttribute : Attribute
    {
    }
}
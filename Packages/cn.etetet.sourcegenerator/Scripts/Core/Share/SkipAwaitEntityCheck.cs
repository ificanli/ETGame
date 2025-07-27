using System;

namespace ET
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class SkipAwaitEntityCheckAttribute: Attribute
    {
        
    }
}
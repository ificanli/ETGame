using System;

namespace ET
{
    /// <summary>
    /// 每个XXConfigCategory单例的接口
    /// </summary>
    public interface ILubanConfig
    {
        //Model中的所有配置初始化完毕后的调用
        //Ref的初始化
        void ResolveRef();
    }

    public interface ILubanConfigSystem : ISystemType
    {
        //所有配置初始化完毕后的调用
        //解决需要在Hotfix中初始化其他数据时使用
        void LubanConfig(ILubanConfig data);
    }

    [EntitySystem]
    public abstract class LubanConfigSystem<T> : SystemObject, ILubanConfigSystem where T : ILubanConfig
    {
        Type ISystemType.Type()
        {
            return typeof(T);
        }

        Type ISystemType.SystemType()
        {
            return typeof(ILubanConfigSystem);
        }

        void ILubanConfigSystem.LubanConfig(ILubanConfig data)
        {
            this.LubanConfig((T)data);
        }

        protected abstract void LubanConfig(T self);
    }
}
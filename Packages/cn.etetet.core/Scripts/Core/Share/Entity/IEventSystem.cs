using System;

namespace ET
{
    public interface IEvent<T> where T: struct
    {
    }

    public interface IEventSystem
    {
    }
    
    public interface AEventSystem<in T>: ISystemType, IEventSystem where T: struct
    {
        void Run(Entity e, T t);
    }
    
    public abstract class EventSystem<E, T> : SystemObject, AEventSystem<T> where E: Entity, IEvent<T> where T: struct
    {
        public void Run(Entity e, T t)
        {
            this.Event((E)e, t);
        }

        public Type SystemType()
        {
            return typeof(AEventSystem<T>);
        }

        public Type Type()
        {
            return typeof(E);
        }

        protected abstract void Event(E e, T t);
    }
}
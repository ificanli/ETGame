using System;
using System.Collections.Generic;
using System.Linq;

namespace ET
{
    public class World : IDisposable
    {
        [StaticField]
        private static World instance;

        [StaticField]
        public static World Instance
        {
            get
            {
                return instance ??= new World();
            }
        }

        private int idGenerator;

        private readonly SortedDictionary<int, ASingleton> idSingletons = new();
        private readonly Dictionary<Type, ASingleton> singletons = new();

        private World()
        {
        }

        public void Dispose()
        {
            instance = null;

            lock (this)
            {
                // dispose剩下的singleton，主要为了把instance置空
                foreach (var kv in this.idSingletons.Reverse())
                {
                    kv.Value.Dispose();
                }
                idSingletons.Clear();
                singletons.Clear();
            }
        }

        public T AddSingleton<T>() where T : ASingleton, ISingletonAwake, new()
        {
            T singleton = new();
            singleton.Awake();

            AddSingleton(singleton);
            return singleton;
        }

        public T AddSingleton<T, A>(A a) where T : ASingleton, ISingletonAwake<A>, new()
        {
            T singleton = new();
            singleton.Awake(a);

            AddSingleton(singleton);
            return singleton;
        }

        public T AddSingleton<T, A, B>(A a, B b) where T : ASingleton, ISingletonAwake<A, B>, new()
        {
            T singleton = new();
            singleton.Awake(a, b);

            AddSingleton(singleton);
            return singleton;
        }

        public T AddSingleton<T, A, B, C>(A a, B b, C c) where T : ASingleton, ISingletonAwake<A, B, C>, new()
        {
            T singleton = new();
            singleton.Awake(a, b, c);

            AddSingleton(singleton);
            return singleton;
        }

        public void AddSingleton(ASingleton singleton)
        {
            lock (this)
            {
                Type type = singleton.GetType();
                int id = idGenerator++;
                singletons[type] = singleton;
                idSingletons[id] = singleton;
                singleton.Register(id);
            }            
        }

        public void RemoveSingleton<T>() where T : ASingleton
        {
            lock (this)
            {
                Type type = typeof(T);
                if (!this.singletons.Remove(type, out ASingleton singleton))
                {
                    return;
                }

                this.idSingletons.Remove(singleton.Id);
                singleton.Dispose();
            }
        }
    }
}
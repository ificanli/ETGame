using System;
using System.Collections.Generic;

namespace ET
{
    public class World: IDisposable
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

        private readonly SortedDictionary<int, HashSet<ASingleton>> removeOrder = new();
        private readonly Dictionary<Type, ASingleton> singletons = new();
        
        private World()
        {
        }
        
        public void Dispose()
        {
            instance = null;
            
            lock (this)
            {
                foreach (var kv in this.removeOrder)
                {
                    foreach (ASingleton singleton in kv.Value)
                    {
                        singleton.Dispose();
                    }
                }
                this.removeOrder.Clear();
                this.singletons.Clear();
            }
        }
        
        private void AddToOrder(ASingleton singleton)
        {
            int order = singleton.RemoveOrder();
            if (!this.removeOrder.TryGetValue(order, out HashSet<ASingleton> set))            
            {
                set = new HashSet<ASingleton>();
                this.removeOrder[order] = set;
            }
            set.Add(singleton);
        }

        private void RemoveFromOrder(ASingleton singleton)
        {
            int order = singleton.RemoveOrder();
            if (this.removeOrder.TryGetValue(order, out HashSet<ASingleton> set))
            {
                set.Remove(singleton);
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
                this.AddToOrder(singleton);
                this.singletons[singleton.GetType()] = singleton;
            }            
            singleton.Register();
        }

        public void RemoveSingleton<T>()
        {
            ASingleton singleton = null;
            lock (this)
            {
                Type type = typeof(T);
                if (!this.singletons.Remove(type, out singleton))
                {
                  return;
                }
                this.RemoveFromOrder(singleton);
            }
            singleton?.Dispose();
        }
    }
}
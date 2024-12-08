using System;
using System.Collections;
using System.Collections.Generic;

namespace ET
{
    public abstract class IValue<T>: DisposeObject
    {
        public T Value
        {
            get;
            protected set;
        }
    }
    
    [EnableClass]
    public class ValueTypeWrap<T>: IValue<T>, IPool
    {
        public static ValueTypeWrap<T> Create(T value, bool isFromPool = true)
        {
            ValueTypeWrap<T> vw = ObjectPool.Fetch<ValueTypeWrap<T>>(isFromPool);
            vw.Value = value;
            return vw;
        }

        public bool IsFromPool { get; set; }

        public override void Dispose()
        {
            this.Value = default;
            ObjectPool.Recycle(this);
        }
    }
    
    public static class BTEvnKey
    {
        public const string Buff = "Buff";
        public const string Owner = "Owner";
        public const string Unit = "Unit";
        public const string Caster = "Caster";
        public const string Attacker = "Attacker";
        public const string Target = "Target";
        public const string Units = "Units";
        public const string Pos = "Pos";
        public const string BuffRemoveType = "BuffRemoveType";
    }
    
    public class BTEnv: DisposeObject, IPool
    {
        public static BTEnv Create(Scene scene, bool isFromPool = true)
        {
            BTEnv env = ObjectPool.Fetch<BTEnv>(isFromPool);
            env.Scene = scene;
            return env;
        }

        public Scene Scene { get; set; }

        public bool IsFromPool { get; set; }
        
        private readonly Dictionary<string, object> dict = new();

        public override void Dispose()
        {
            this.dict.Clear();
            this.Scene = null;

            foreach (var kv in dict)
            {
                if (kv.Value is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }

        public void CopyTo(BTEnv env)
        {
            foreach (KeyValuePair<string, object> keyValuePair in this.dict)
            {
                env.dict.Add(keyValuePair.Key, keyValuePair.Value);
            }
        }
        
        public T GetCollection<T>(string key) where T: IEnumerable
        {
            if (!this.dict.TryGetValue(key, out object value))
            {
                throw new Exception($"btenv not found key: {key} {typeof(T).FullName}");
            }
            
            return (T)value;
        }

        public T GetStruct<T>(string key) where T : struct
        {
            if (!this.dict.TryGetValue(key, out object value))
            {
                throw new Exception($"btenv not found key: {key} {typeof(T).FullName}");
            }

            try
            {
                IValue<T> iValue = (IValue<T>) value;
                return iValue.Value;
            }
            catch (InvalidCastException e)
            {
                throw new Exception($"不能把{value.GetType()}转换为{typeof (T)}", e);
            }
        }
        
        public T GetEntity<T>(string key) where T: Entity
        {
            if (!this.dict.TryGetValue(key, out object value))
            {
                throw new Exception($"btenv not found key: {key} {typeof(T).FullName}");
            }

            try
            {
                IValue<EntityRef<T>> iValue = (ValueTypeWrap<EntityRef<T>>) value;
                return iValue.Value;
            }
            catch (InvalidCastException e)
            {
                throw new Exception($"不能把{value.GetType()}转换为{typeof (T)}", e);
            }
        }

        public bool ContainKey(string key)
        {
            return this.dict.ContainsKey(key);
        }

        public void AddCollection<T>(string key, T list) where T: IEnumerable
        {
            this.dict[key] = list;
        }

        public void AddEntity<T>(string key, T value) where T: Entity
        {
            EntityRef<T> entityRef = value;
            ValueTypeWrap<EntityRef<T>> wrap = ValueTypeWrap<EntityRef<T>>.Create(entityRef);
            this.dict[key] = wrap;
        }
        
        public void AddStruct<T>(string key, T value) where T: struct
        {
            ValueTypeWrap<T> wrap = ValueTypeWrap<T>.Create(value);
            this.dict[key] = wrap;
        }
    }
}
using System;
using System.Collections.Generic;

namespace ET
{
    public interface IValue<T>
    {
        T Value
        {
            get;
        }
    }
    
    public class ValueTypeWrap<T>: DisposeObject, IValue<T>, IPool
    {
        public static ValueTypeWrap<T> Create(T value, bool isFromPool = true)
        {
            ValueTypeWrap<T> vw = ObjectPool.Fetch<ValueTypeWrap<T>>(isFromPool);
            vw.Value = value;
            return vw;
        }
        
        public T Value
        {
            get;
            private set;
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
        public const string Spell = "Spell";
        public const string Buff = "Buff";
        public const string Unit = "Unit";
    }
    
    public class BTEnv: DisposeObject, IPool
    {
        public static BTEnv Create(bool isFromPool = true)
        {
            BTEnv env = ObjectPool.Fetch<BTEnv>(isFromPool);
            return env;
        }
        
        public bool IsFromPool { get; set; }
        
        private readonly Dictionary<string, object> dict = new();

        private readonly HashSet<DisposeObject> disposers = new();

        public override void Dispose()
        {
            this.dict.Clear();

            foreach (DisposeObject disposer in this.disposers)
            {
                disposer.Dispose();
            }

            this.disposers.Clear();
            
            ObjectPool.Recycle(this);
        }

        public void CopyTo(BTEnv env)
        {
            foreach (KeyValuePair<string, object> keyValuePair in this.dict)
            {
                env.dict.Add(keyValuePair.Key, keyValuePair.Value);
            }

            foreach (DisposeObject disposer in this.disposers)
            {
                env.disposers.Add(disposer);
            }
        }

        public T Get<T>(string key)
        {
            if (!this.dict.TryGetValue(key, out object value))
            {
                return default;
            }

            try
            {
                if (typeof (T).IsClass)
                {
                    return (T) value;
                }

                IValue<T> iValue = (IValue<T>) value;
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

        public void Add<T>(string key, T value)
        {
            if (typeof (T).IsClass)
            {
                this.dict[key] = value;
                return;
            }

            ValueTypeWrap<T> wrap = ValueTypeWrap<T>.Create(value);
            this.dict[key] = wrap;
            this.disposers.Add(wrap);
        }
    }
}
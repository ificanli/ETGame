using System;
using System.Collections;
using System.Collections.Generic;

namespace ET
{
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
            foreach (var kv in dict)
            {
                if (kv.Value is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            
            this.dict.Clear();
            this.Scene = null;
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
        
        public T TryGetEntity<T>(string key) where T: Entity
        {
            if (!this.dict.TryGetValue(key, out object value))
            {
                return null;
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

        public void AddEntity<T>(string key, T entity) where T: Entity
        {
            ValueTypeWrap<EntityRef<T>> wrap = null;
            if (this.dict.TryGetValue(key, out object value))
            {
                wrap = (ValueTypeWrap<EntityRef<T>>) value;
                wrap.Value = entity;
            }
            else
            {
                wrap = ValueTypeWrap<EntityRef<T>>.Create(entity);
                this.dict.Add(key, wrap);
            }
        }
        
        public void AddStruct<T>(string key, T value) where T: struct
        {
            ValueTypeWrap<T> wrap = null;
            if (this.dict.TryGetValue(key, out object obj))
            {
                wrap = (ValueTypeWrap<T>) obj;
                wrap.Value = value;
            }
            else
            {
                wrap = ValueTypeWrap<T>.Create(value);
                this.dict.Add(key, wrap);
            }
        }
    }
}
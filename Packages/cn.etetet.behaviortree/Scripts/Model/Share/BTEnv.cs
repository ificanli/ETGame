using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace ET
{
    public class BTEnv: DisposeObject, IPool
    {
        public long EntityId { get; private set; }

        public static BTEnv Create(Scene scene, long entityId, bool isFromPool = true)
        {
            BTEnv env = ObjectPool.Fetch<BTEnv>(isFromPool);
            env.Scene = scene;
            
#if UNITY_EDITOR
            env.EntityId = entityId;
            env.RunPath = new List<int>();
#endif
            return env;
        }

        public EntityRef<Scene> Scene;

        public bool IsFromPool { get; set; }

        private readonly Dictionary<string, object> dict = new();

#if UNITY_EDITOR
        /// <summary>
        /// Debug snapshot data
        /// </summary>
        private StringBuilder debugSnapshot;

        public List<int> RunPath { get; private set; }
#endif

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
#if UNITY_EDITOR
            this.debugSnapshot = null;
            this.RunPath = null;
#endif
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
        
        public bool TryGetEntity<T>(string key, out T t) where T: Entity
        {
            t = null; 
            if (key == null)
            {
                return false;
            }
            if (!this.dict.TryGetValue(key, out object value))
            {
                return false;
            }

            try
            {
                IValue<EntityRef<T>> iValue = (ValueTypeWrap<EntityRef<T>>) value;
                t = iValue.Value;
                return true;
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
            try
            {
                this.AddSnapshot($"{key}: {MongoHelper.ToJson(list)}");
            }
            catch (Exception e)
            {
                Log.Error($"add snapshot fail: {typeof(T).FullName} {e}");
            }
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
            
            try
            {
                this.AddSnapshot($"{key}: {MongoHelper.ToJson(entity)}");
            }
            catch (Exception e)
            {
                Log.Error($"add snapshot fail: {typeof(T).FullName} {e}");
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

            try
            {
                this.AddSnapshot($"{key}: {MongoHelper.ToJson(value)}");
            }
            catch (Exception e)
            {
                Log.Error($"add snapshot fail: {typeof(T).FullName} {e}");
            }
        }

        #region Debug Snapshot
        
        [Conditional("UNITY_EDITOR")]
        public void AddPath(int nodeId)
        {
            if (nodeId == 0)
            {
                throw new Exception("add node error!");
            }
#if UNITY_EDITOR
            this.RunPath.Add(nodeId);
#endif
        }

        /// <summary>
        /// Add debug snapshot line
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        public void AddSnapshot(string text)
        {
#if UNITY_EDITOR
            this.debugSnapshot ??= new StringBuilder();
            this.debugSnapshot.AppendLine(text);
#endif
        }

        /// <summary>
        /// Get debug snapshot StringBuilder (direct reference, for performance)
        /// </summary>
        public StringBuilder GetSnapshot()
        {
#if UNITY_EDITOR
            return this.debugSnapshot;
#else
            return null;
#endif
        }
        #endregion
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using MemoryPack;
using MongoDB.Bson.Serialization.Attributes;

namespace ET
{
    [Flags]
    public enum EntityStatus: byte
    {
        None = 0,
        IsFromPool = 1,
        IsRegister = 1 << 1,
        IsComponent = 1 << 2,
        NoDeserializeSystem = 1 << 3,  // 不需要执行反序列化System
        IsSerializeWithParent = 1 << 4,
    }

    [MemoryPackable(GenerateType.NoGenerate)]
    public abstract partial class Entity: DisposeObject, IPool
    {
        // 给source generater调用的
        public static T Fetch<T>() where T : Entity
        {
            return ObjectPool.Fetch<T>();
        }
        
        public virtual long GetLongHashCode()
        {
            return this.GetType().TypeHandle.Value.ToInt64();
        }
        
        public virtual long GetComponentLongHashCode(Type type)
        {
            return type.TypeHandle.Value.ToInt64();
        }
        
#if ENABLE_VIEW && UNITY_EDITOR
        [BsonIgnore]
        [UnityEngine.HideInInspector]
        [MemoryPackIgnore]
        public UnityEngine.GameObject ViewGO;
#endif

        [MemoryPackIgnore]
        [BsonIgnore]
        public int InstanceId { get; protected set; }

        [BsonIgnore]
        private EntityStatus status = EntityStatus.None;

        [MemoryPackIgnore]
        [BsonIgnore]
        public bool IsFromPool
        {
            get => (this.status & EntityStatus.IsFromPool) == EntityStatus.IsFromPool;
            set
            {
                if (value)
                {
                    this.status |= EntityStatus.IsFromPool;
                }
                else
                {
                    this.status &= ~EntityStatus.IsFromPool;
                }
            }
        }

        [BsonIgnore]
        protected bool IsRegister
        {
            get => (this.status & EntityStatus.IsRegister) == EntityStatus.IsRegister;
            set
            {
                if (this.IsRegister == value)
                {
                    return;
                }

                if (value)
                {
                    this.status |= EntityStatus.IsRegister;
                }
                else
                {
                    this.status &= ~EntityStatus.IsRegister;
                }

                if (value)
                {
                    this.RegisterSystem();
                }

#if ENABLE_VIEW && UNITY_EDITOR
                if (value)
                {
                    this.ViewGO = new UnityEngine.GameObject(this.ViewName);
                    this.ViewGO.AddComponent<ComponentView>().Component = this;
                    this.ViewGO.transform.SetParent(this.parent == null? 
                            UnityEngine.GameObject.Find("Global/Scenes").transform : this.parent.ViewGO.transform);
                }
                else
                {
                    UnityEngine.Object.Destroy(this.ViewGO);
                }
#endif
            }
        }

        protected virtual void RegisterSystem()
        {
            this.iScene.Fiber.EntitySystem.RegisterSystem(this);
        }

        protected virtual string ViewName
        {
            get
            {
                return this.GetType().FullName;
            }
        }

        [BsonIgnore]
        protected bool IsComponent
        {
            get => (this.status & EntityStatus.IsComponent) == EntityStatus.IsComponent;
            set
            {
                if (value)
                {
                    this.status |= EntityStatus.IsComponent;
                }
                else
                {
                    this.status &= ~EntityStatus.IsComponent;
                }
            }
        }

        [BsonIgnore]
        protected bool IsNoDeserializeSystem
        {
            get => (this.status & EntityStatus.NoDeserializeSystem) == EntityStatus.NoDeserializeSystem;
            set
            {
                if (value)
                {
                    this.status |= EntityStatus.NoDeserializeSystem;
                }
                else
                {
                    this.status &= ~EntityStatus.NoDeserializeSystem;
                }
            }
        }
        
        [BsonIgnore]
        public bool IsSerializeWithParent
        {
            get => (this.status & EntityStatus.IsSerializeWithParent) == EntityStatus.IsSerializeWithParent;
            set
            {
                if (value)
                {
                    this.status |= EntityStatus.IsSerializeWithParent;
                }
                else
                {
                    this.status &= ~EntityStatus.IsSerializeWithParent;
                }
            }
        }

        [MemoryPackIgnore]
        [BsonIgnore]
        public bool IsDisposed => this.InstanceId == 0;
        
        [AllowEntityMember]
        private Entity parent;

        // 可以改变parent，但是不能设置为null
        [MemoryPackIgnore]
        [BsonIgnore]
        public Entity Parent
        {
            get => this.parent;
            protected set
            {
                if (value == null)
                {
                    throw new Exception($"cant set parent null: {this.GetType().FullName}");
                }

                if (value == this)
                {
                    throw new Exception($"cant set parent self: {this.GetType().FullName}");
                }

                // 严格限制parent必须要有iSence,也就是说parent必须在数据树上面
                if (value.IScene == null)
                {
                    throw new Exception($"cant set parent because parent iSence is null: {this.GetType().FullName} {value.GetType().FullName}");
                }

                if (this.parent != null) // 之前有parent
                {
                    // parent相同，不设置
                    if (this.parent == value)
                    {
                        Log.Error($"重复设置了Parent: {this.GetType().FullName} parent: {this.parent.GetType().FullName}");
                        return;
                    }

                    this.parent.RemoveChild(this.Id, false);
                }

                this.parent = value;
                this.IsComponent = false;
                this.parent.AddToChildren(this);

                if (this is IScene scene)
                {
                    scene.Fiber = this.parent.iScene.Fiber;
                    this.IScene = scene;
                }
                else
                {
                    this.IScene = this.parent.iScene;
                }

#if ENABLE_VIEW && UNITY_EDITOR
                this.ViewGO.GetComponent<ComponentView>().Component = this;
                this.ViewGO.transform.SetParent(this.parent == null ?
                        UnityEngine.GameObject.Find("Global").transform : this.parent.ViewGO.transform);
                foreach (Entity child in this.Children.Values)
                {
                    child.ViewGO.transform.SetParent(this.ViewGO.transform);
                }
                foreach (Entity comp in this.Components.Values)
                {
                    comp.ViewGO.transform.SetParent(this.ViewGO.transform);
                }
#endif
            }
        }

        // 该方法只能在AddComponent中调用，其他人不允许调用
        [BsonIgnore]
        private Entity ComponentParent
        {
            set
            {
                if (value == null)
                {
                    throw new Exception($"cant set parent null: {this.GetType().FullName}");
                }

                if (value == this)
                {
                    throw new Exception($"cant set parent self: {this.GetType().FullName}");
                }

                // 严格限制parent必须要有iSence,也就是说parent必须在数据树上面
                if (value.IScene == null)
                {
                    throw new Exception($"cant set parent because parent iSence is null: {this.GetType().FullName} {value.GetType().FullName}");
                }

                if (this.parent != null) // 之前有parent
                {
                    // parent相同，不设置
                    if (this.parent == value)
                    {
                        Log.Error($"重复设置了Parent: {this.GetType().FullName} parent: {this.parent.GetType().FullName}");
                        return;
                    }

                    this.parent.RemoveComponent(this.GetType(), false);
                }

                this.parent = value;
                this.IsComponent = true;
                this.parent.AddToComponents(this);
                
                if (this is IScene scene)
                {
                    scene.Fiber = this.parent.iScene.Fiber;
                    this.IScene = scene;
                }
                else
                {
                    this.IScene = this.parent.iScene;
                }
            }
        }

        public T GetParent<T>() where T : Entity
        {
            return this.parent as T;
        }

        [BsonIgnoreIfDefault]
        [BsonDefaultValue(0L)]
        [BsonElement]
        [BsonId]
        public long Id { get; protected set; }

        [BsonIgnore]
        protected IScene iScene;

        [MemoryPackIgnore]
        [BsonIgnore]
        public IScene IScene
        {
            get
            {
                return this.iScene;
            }
            protected set
            {
                if (value == null)
                {
                    throw new Exception($"iScene cant set null: {this.GetType().FullName}");
                }

                if (this.iScene == value)
                {
                    return;
                }

                if (this.iScene != null)
                {
                    this.iScene = value;
                    return;
                }

                this.iScene = value;
                
                if (this.InstanceId == 0)
                {
                    this.InstanceId = iScene.Fiber.NewInstanceId();
                }

                this.IsRegister = true;
                
                if (this is ISerializeToEntity)
                {
                    this.IsSerializeWithParent = true;
                }

                // 反序列化出来的需要设置父子关系
                if (this.components != null)
                {
                    foreach ((long _, Entity component) in this.components)
                    {
                        component.IsComponent = true;
                        component.parent = this;
                        component.IScene = this.iScene;
                    }
                }

                if (this.children != null)
                {
                    foreach ((long _, Entity child) in this.children)
                    {
                        child.IsComponent = false;
                        child.parent = this;
                        child.IScene = this.iScene;
                    }
                }
                    
                if (!this.IsNoDeserializeSystem)
                {
                    EntitySystemSingleton.Instance.Deserialize(this);
                }
            }
        }

        [MemoryPackInclude]
        [BsonElement]
        [BsonIgnoreIfNull]
        protected ChildrenCollection children;

        [MemoryPackIgnore]
        [BsonIgnore]
        public ChildrenCollection Children
        {
            get
            {
                return this.children ??= ObjectPool.Fetch<ChildrenCollection>();
            }
        }

        private void AddToChildren(Entity entity)
        {
            this.Children.Add(entity.Id, entity);
        }

        [MemoryPackInclude]
        [BsonElement]
        [BsonIgnoreIfNull]
        protected ComponentsCollection components;

        [MemoryPackIgnore]
        [BsonIgnore]
        public ComponentsCollection Components
        {
            get
            {
                return this.components ??= ObjectPool.Fetch<ComponentsCollection>();
            }
        }

        public int ComponentsCount()
        {
            if (this.components == null)
            {
                return 0;
            }
            return this.components.Count;
        }
        
        public int ChildrenCount()
        {
            if (this.children == null)
            {
                return 0;
            }
            return this.children.Count;
        }

        public override void Dispose()
        {
            if (this.IsDisposed)
            {
                return;
            }

            this.IsRegister = false;
            this.InstanceId = 0;

            // 清理Children
            if (this.children != null)
            {
                foreach (Entity child in this.children.Values)
                {
                    child.Dispose();
                }

                this.children.Dispose();
                this.children = null;
            }

            // 清理Component
            if (this.components != null)
            {
                foreach (var kv in this.components)
                {
                    kv.Value.Dispose();
                }

                this.components.Dispose();
                this.components = null;
            }

            // 触发Destroy事件
            if (this is IDestroy)
            {
                EntitySystemSingleton.Instance.Destroy(this);
            }

            this.iScene = null;

            if (this.parent != null && !this.parent.IsDisposed)
            {
                if (this.IsComponent)
                {
                    this.parent.RemoveComponent(this.GetType(), false);
                }
                else
                {
                    this.parent.RemoveChild(this.Id, false);
                }
            }

            this.parent = null;

            base.Dispose();
            
            // 把status字段除了IsFromPool其它的status标记都还原
            bool isFromPool = this.IsFromPool;
            this.status = EntityStatus.None;
            this.IsFromPool = isFromPool;
            
            ObjectPool.Recycle(this);
        }

        private void AddToComponents(Entity component)
        {
            this.Components.Add(component.GetLongHashCode(), component);
        }

        public K GetChild<K>(long id) where K : Entity
        {
            this.CheckThread();
            
            if (this.children == null)
            {
                return null;
            }

            this.children.TryGetValue(id, out Entity child);
            return child as K;
        }

        public bool RemoveChild(long id, bool isDispose = true)
        {
            if (this.children == null)
            {
                return false;
            }

            if (!this.children.Remove(id, out Entity child))
            {
                return false;
            }

            if (this.children.Count == 0)
            {
                this.children.Dispose();
                this.children = null;
            }

            if (isDispose)
            {
                child.Dispose();
            }
            else
            {
                child.Reset();
            }

            return true;
        }

        public void RemoveComponent<K>(bool isDispose = true) where K : Entity
        {
            if (this.IsDisposed)
            {
                return;
            }

            if (this.components == null)
            {
                return;
            }

            Type type = typeof (K);

            if (!this.components.Remove(this.GetComponentLongHashCode(type), out Entity c))
            {
                return;
            }

            if (isDispose)
            {
                c.Dispose();
            }
            else
            {
                c.Reset();
            }
        }

        public void RemoveComponent(Type type, bool isDispose = true)
        {
            if (this.IsDisposed)
            {
                return;
            }
            
            if (this.components == null)
            {
                return;
            }

            if (!this.components.Remove(this.GetComponentLongHashCode(type), out Entity c))
            {
                return;
            }

            if (isDispose)
            {
                c.Dispose();
            }
            else
            {
                c.Reset();
            }
        }

        [Conditional("DEBUG")]
        private void CheckThread()
        {
            Fiber fiber = this.IScene.Fiber;
            if (fiber.ThreadId != Environment.CurrentManagedThreadId)
            {
                throw new Exception($"Fiber {fiber.Id} {fiber.Name} is not in fiber thread {fiber.ThreadId} {Environment.CurrentManagedThreadId}");
            }
        }

        public K GetComponent<K>() where K : Entity
        {
            this.CheckThread();
            
            if (this.components == null)
            {
                return null;
            }

            // 如果有IGetComponent接口，则触发GetComponentSystem
            if (this is IGetComponentSys)
            {
                EntitySystemSingleton.Instance.GetComponentSys(this, typeof(K));
            }
            
            Entity component;
            if (!this.components.TryGetValue(this.GetComponentLongHashCode(typeof (K)), out component))
            {
                return default;
            }

            return (K) component;
        }

        public Entity GetComponent(Type type)
        {
            this.CheckThread();
            
            if (this.components == null)
            {
                return null;
            }

            // 如果有IGetComponent接口，则触发GetComponentSystem
            // 这个要在tryget之前调用，因为有可能components没有，但是执行GetComponentSystem后又有了
            if (this is IGetComponentSys)
            {
                EntitySystemSingleton.Instance.GetComponentSys(this, type);
            }
            
            Entity component;
            if (!this.components.TryGetValue(this.GetComponentLongHashCode(type), out component))
            {
                return null;
            }

            return component;
        }

        private static Entity Create(Type type, bool isFromPool)
        {
            Entity component = (Entity) ObjectPool.Fetch(type, isFromPool);

            component.IsFromPool = isFromPool;
            component.IsNoDeserializeSystem = true;
            component.Id = 0;
            return component;
        }

        public Entity AddComponent(Entity component)
        {
            this.CheckThread();
            
            Type type = component.GetType();
            if (this.components != null && this.components.ContainsKey(this.GetComponentLongHashCode(type)))
            {
                throw new Exception($"entity already has component: {type.FullName}");
            }

            component.ComponentParent = this;

            return component;
        }

        public Entity AddComponent(Type type, bool isFromPool = false)
        {
            this.CheckThread();
            
            if (this.components != null && this.components.ContainsKey(this.GetComponentLongHashCode(type)))
            {
                throw new Exception($"entity already has component: {type.FullName}");
            }

            Entity component = Create(type, isFromPool);
            component.Id = this.Id;
            component.ComponentParent = this;
            EntitySystemSingleton entitySystemSingleton = EntitySystemSingleton.Instance;
            entitySystemSingleton.Awake(component);

            return component;
        }

        public K AddComponentWithId<K>(long id, bool isFromPool = false) where K : Entity, IAwake, new()
        {
            this.CheckThread();
            
            Type type = typeof (K);
            if (this.components != null && this.components.ContainsKey(this.GetComponentLongHashCode(type)))
            {
                throw new Exception($"entity already has component: {type.FullName}");
            }

            Entity component = Create(type, isFromPool);
            component.Id = id;
            component.ComponentParent = this;
            EntitySystemSingleton entitySystemSingleton = EntitySystemSingleton.Instance;
            entitySystemSingleton.Awake(component);

            return component as K;
        }

        public K AddComponentWithId<K, P1>(long id, P1 p1, bool isFromPool = false) where K : Entity, IAwake<P1>, new()
        {
            this.CheckThread();
            
            Type type = typeof (K);
            if (this.components != null && this.components.ContainsKey(this.GetComponentLongHashCode(type)))
            {
                throw new Exception($"entity already has component: {type.FullName}");
            }

            Entity component = Create(type, isFromPool);
            component.Id = id;
            component.ComponentParent = this;
            EntitySystemSingleton entitySystemSingleton = EntitySystemSingleton.Instance;
            entitySystemSingleton.Awake(component, p1);

            return component as K;
        }

        public K AddComponentWithId<K, P1, P2>(long id, P1 p1, P2 p2, bool isFromPool = false) where K : Entity, IAwake<P1, P2>, new()
        {
            this.CheckThread();
            
            Type type = typeof (K);
            if (this.components != null && this.components.ContainsKey(this.GetComponentLongHashCode(type)))
            {
                throw new Exception($"entity already has component: {type.FullName}");
            }

            Entity component = Create(type, isFromPool);
            component.Id = id;
            component.ComponentParent = this;
            EntitySystemSingleton entitySystemSingleton = EntitySystemSingleton.Instance;
            entitySystemSingleton.Awake(component, p1, p2);

            return component as K;
        }

        public K AddComponentWithId<K, P1, P2, P3>(long id, P1 p1, P2 p2, P3 p3, bool isFromPool = false) where K : Entity, IAwake<P1, P2, P3>, new()
        {
            this.CheckThread();
            
            Type type = typeof (K);
            if (this.components != null && this.components.ContainsKey(this.GetComponentLongHashCode(type)))
            {
                throw new Exception($"entity already has component: {type.FullName}");
            }

            Entity component = Create(type, isFromPool);
            component.Id = id;
            component.ComponentParent = this;
            EntitySystemSingleton entitySystemSingleton = EntitySystemSingleton.Instance;
            entitySystemSingleton.Awake(component, p1, p2, p3);

            return component as K;
        }

        public K AddComponent<K>(bool isFromPool = false) where K : Entity, IAwake, new()
        {
            return this.AddComponentWithId<K>(this.Id, isFromPool);
        }

        public K AddComponent<K, P1>(P1 p1, bool isFromPool = false) where K : Entity, IAwake<P1>, new()
        {
            return this.AddComponentWithId<K, P1>(this.Id, p1, isFromPool);
        }

        public K AddComponent<K, P1, P2>(P1 p1, P2 p2, bool isFromPool = false) where K : Entity, IAwake<P1, P2>, new()
        {
            return this.AddComponentWithId<K, P1, P2>(this.Id, p1, p2, isFromPool);
        }

        public K AddComponent<K, P1, P2, P3>(P1 p1, P2 p2, P3 p3, bool isFromPool = false) where K : Entity, IAwake<P1, P2, P3>, new()
        {
            return this.AddComponentWithId<K, P1, P2, P3>(this.Id, p1, p2, p3, isFromPool);
        }

        public Entity AddChild(Entity entity)
        {
            entity.Parent = this;
            return entity;
        }

        public T AddChild<T>(bool isFromPool = false) where T : Entity, IAwake
        {
            this.CheckThread();
            
            Type type = typeof (T);
            T component = (T) Entity.Create(type, isFromPool);
            component.Id = IdGenerater.Instance.GenerateId();
            component.Parent = this;

            EntitySystemSingleton.Instance.Awake(component);
            return component;
        }

        public T AddChild<T, A>(A a, bool isFromPool = false) where T : Entity, IAwake<A>
        {
            this.CheckThread();
            
            Type type = typeof (T);
            T component = (T) Entity.Create(type, isFromPool);
            component.Id = IdGenerater.Instance.GenerateId();
            component.Parent = this;

            EntitySystemSingleton.Instance.Awake(component, a);
            return component;
        }

        public T AddChild<T, A, B>(A a, B b, bool isFromPool = false) where T : Entity, IAwake<A, B>
        {
            this.CheckThread();
            
            Type type = typeof (T);
            T component = (T) Entity.Create(type, isFromPool);
            component.Id = IdGenerater.Instance.GenerateId();
            component.Parent = this;

            EntitySystemSingleton.Instance.Awake(component, a, b);
            return component;
        }

        public T AddChild<T, A, B, C>(A a, B b, C c, bool isFromPool = false) where T : Entity, IAwake<A, B, C>
        {
            this.CheckThread();
            
            Type type = typeof (T);
            T component = (T) Entity.Create(type, isFromPool);
            component.Id = IdGenerater.Instance.GenerateId();
            component.Parent = this;

            EntitySystemSingleton.Instance.Awake(component, a, b, c);
            return component;
        }

        public T AddChildWithId<T>(long id, bool isFromPool = false) where T : Entity, IAwake
        {
            this.CheckThread();
            
            Type type = typeof (T);
            T component = Entity.Create(type, isFromPool) as T;
            component.Id = id;
            component.Parent = this;
            EntitySystemSingleton.Instance.Awake(component);
            return component;
        }

        public T AddChildWithId<T, A>(long id, A a, bool isFromPool = false) where T : Entity, IAwake<A>
        {
            this.CheckThread();
            
            Type type = typeof (T);
            T component = (T) Entity.Create(type, isFromPool);
            component.Id = id;
            component.Parent = this;

            EntitySystemSingleton.Instance.Awake(component, a);
            return component;
        }

        public T AddChildWithId<T, A, B>(long id, A a, B b, bool isFromPool = false) where T : Entity, IAwake<A, B>
        {
            this.CheckThread();
            
            Type type = typeof (T);
            T component = (T) Entity.Create(type, isFromPool);
            component.Id = id;
            component.Parent = this;

            EntitySystemSingleton.Instance.Awake(component, a, b);
            return component;
        }

        public T AddChildWithId<T, A, B, C>(long id, A a, B b, C c, bool isFromPool = false) where T : Entity, IAwake<A, B, C>
        {
            this.CheckThread();
            
            Type type = typeof (T);
            T component = (T) Entity.Create(type, isFromPool);
            component.Id = id;
            component.Parent = this;

            EntitySystemSingleton.Instance.Awake(component, a, b, c);
            return component;
        }

        public override void BeginInit()
        {
            // 如果没有挂到树上，不用执行SerializeSystem
            if (this.iScene == null)
            {
                return;
            }
            
            EntitySystemSingleton.Instance.Serialize(this);
            
            if (this.components is { Count: > 0 })
            {
                foreach ((long _, Entity entity) in this.components)
                {
                    if (entity.IsSerializeWithParent)
                    {
                        entity.BeginInit();
                    }
                }
            }

            if (this.children is { Count: > 0 })
            {
                foreach ((long _, Entity entity) in this.children)
                {
                    if (entity.IsSerializeWithParent)
                    {
                        entity.BeginInit();
                    }
                }
            }
        }
        
        private void Reset()
        {
            if (this.iScene == null)
            {
                return;
            }
            
            this.InstanceId = 0;
            this.iScene = null;
            this.IsNoDeserializeSystem = false;
            this.IsRegister = false;
            
            if (this.components is { Count: > 0 })
            {
                foreach ((long _, Entity entity) in this.components)
                {
                    entity.Reset();
                }
            }

            if (this.children is { Count: > 0 })
            {
                foreach ((long _, Entity entity) in this.children)
                {
                    entity.Reset();
                }
            }
        }
        
        // 移除不需要序列化的Entity
        public void ClearUnSerialized()
        {
            if (this.components is { Count: > 0 })
            {
                using ListComponent<Type> types = ListComponent<Type>.Create();
                foreach ((long _, Entity entity) in this.components)
                {
                    if (!entity.IsSerializeWithParent)
                    {
                        types.Add(entity.GetType());
                        continue;
                    }
                    entity.ClearUnSerialized();
                }

                foreach (Type removeType in types)
                {
                    this.RemoveComponent(removeType);
                }
            }

            if (this.children is { Count: > 0 })
            {
                using ListComponent<long> ids = ListComponent<long>.Create();
                
                foreach ((long id, Entity entity) in this.children)
                {
                    if (!entity.IsSerializeWithParent)
                    {
                        ids.Add(id);
                        continue;
                    }
                    
                    entity.ClearUnSerialized();
                }
                
                foreach (long id in ids)
                {
                    this.RemoveChild(id);
                }
            }
        }
    }
}
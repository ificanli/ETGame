using System;
using System.Collections.Generic;

namespace ET
{
    public class EffectHandlerAttribute: BaseAttribute
    {
        public int SceneType { get; }

        public EffectHandlerAttribute(int sceneType)
        {
            this.SceneType = sceneType;
        }
    }
    
    public interface IEffectHandler
    {
        void Handle(Effect effect, EffectConfig effectConfig);
        Type GetEffectType();
    }
    
    public abstract class AEffectHandler<C>: HandlerObject, IEffectHandler where C : EffectConfig
    {
        protected abstract void Run(Effect effect, C effectConfig);

        public void Handle(Effect effect, EffectConfig effectConfig)
        {
            if (effectConfig is not C c)
            {
                Log.Error($"AEffectHandler EffectConfig类型转换错误: {effectConfig.GetType().FullName} to {typeof (C).Name}");
                return;
            }

            this.Run(effect, c);
        }

        public Type GetEffectType()
        {
            return typeof (C);
        }
    }
    
    public struct EffectDispatcherInfo
    {
        public int SceneType { get; }
        
        public IEffectHandler IEffectHandler { get; }

        public EffectDispatcherInfo(int sceneType, IEffectHandler iEffectHandler)
        {
            this.SceneType = sceneType;
            this.IEffectHandler = iEffectHandler;
        }
    }
    
    /// <summary>
    /// Actor消息分发组件
    /// </summary>
    [CodeProcess]
    public class EffectDispatcher: Singleton<EffectDispatcher>, ISingletonAwake
    {
        private readonly Dictionary<Type, List<EffectDispatcherInfo>> effectHandlers = new();

        public void Awake()
        {
            HashSet<Type> types = CodeTypes.Instance.GetTypes(typeof (EffectHandlerAttribute));
            
            foreach (Type type in types)
            {
                this.Register(type);
            }
        }
        
        private void Register(Type type)
        {
            object obj = Activator.CreateInstance(type);

            IEffectHandler effectHandler = obj as IEffectHandler;
            if (effectHandler == null)
            {
                throw new Exception($"effect handler not inherit IMActorHandler abstract class: {obj.GetType().FullName}");
            }
                
            object[] attrs = type.GetCustomAttributes(typeof(EffectHandlerAttribute), true);

            foreach (object attr in attrs)
            {
                EffectHandlerAttribute effectHandlerAttribute = attr as EffectHandlerAttribute;

                Type effectType = effectHandler.GetEffectType();
                EffectDispatcherInfo effectDispatcherInfo = new(effectHandlerAttribute.SceneType, effectHandler);

                this.RegisterHandler(effectType, effectDispatcherInfo);
            }
        }
        
        private void RegisterHandler(Type type, EffectDispatcherInfo handler)
        {
            if (!this.effectHandlers.ContainsKey(type))
            {
                this.effectHandlers.Add(type, new List<EffectDispatcherInfo>());
            }

            this.effectHandlers[type].Add(handler);
        }
        
        public void Handle(Effect effect, EffectConfig effectConfig)
        {
            List<EffectDispatcherInfo> list;
            if (!this.effectHandlers.TryGetValue(effectConfig.GetType(), out list))
            {
                throw new Exception($"not found effect handler: {effectConfig.GetType().FullName}");
            }

            int sceneType = effect.Scene().SceneType;
            
            foreach (EffectDispatcherInfo effectDispatcherInfo in list)
            {
                if (!SceneTypeSingleton.IsSame(effectDispatcherInfo.SceneType, sceneType))
                {
                    continue;
                }
                effectDispatcherInfo.IEffectHandler.Handle(effect, effectConfig);
            }
        }
    }
}
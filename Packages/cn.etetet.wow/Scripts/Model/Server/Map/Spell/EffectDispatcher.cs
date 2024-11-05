using System;
using System.Collections.Generic;

namespace ET.Server
{
    [CodeProcess]
    public class EffectDispatcher: Singleton<EffectDispatcher>, ISingletonAwake
    {
        private readonly Dictionary<int, IEffectHandler> dispatcher = new();
        
        public void Awake()
        {
            HashSet<Type> types = CodeTypes.Instance.GetTypes(typeof (EffectHandlerAttribute));

            foreach (Type type in types)
            {
                object[] attrs = type.GetCustomAttributes(typeof(EffectHandlerAttribute), false);
                if (attrs.Length == 0)
                {
                    continue;
                }

                EffectHandlerAttribute effectHandlerAttribute = (EffectHandlerAttribute)attrs[0];
                
                object obj = Activator.CreateInstance(type);

                if (obj is not IEffectHandler iEffectHandler)
                {
                    throw new Exception($"EffectHandler handler not inherit IEffect class: {obj.GetType().FullName}");
                }
                
                dispatcher.Add(effectHandlerAttribute.Type, iEffectHandler);
            }
        }

        public void Run(Effect effect)
        {
            if (!this.dispatcher.TryGetValue(effect.Config.Type, out IEffectHandler effectHandler))
            {
                throw new Exception($"not found effectHandler: {effect.Config.Type}");
            }
            effectHandler.Run(effect);
        }
    }
}
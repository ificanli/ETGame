using System;
using System.Collections.Generic;

namespace ET.Server
{
    [CodeProcess]
    public class QuestObjectiveDispatcher: Singleton<QuestObjectiveDispatcher>, ISingletonAwake
    {
        private readonly Dictionary<QuestObjectiveType, IQuestObjectiveHandler> dispatcher = new();
        
        public void Awake()
        {
            HashSet<Type> types = CodeTypes.Instance.GetTypes(typeof (QuestObjectiveAttribute));

            foreach (Type type in types)
            {
                object[] attrs = type.GetCustomAttributes(typeof(QuestObjectiveAttribute), false);
                if (attrs.Length == 0)
                {
                    continue;
                }

                QuestObjectiveAttribute questObjectiveAttribute = (QuestObjectiveAttribute)attrs[0];
                
                object obj = Activator.CreateInstance(type);

                IQuestObjectiveHandler questObjectiveHandler = obj as IQuestObjectiveHandler;
                if (questObjectiveHandler == null)
                {
                    throw new Exception($"QuestObjectiveHandler handler not inherit IQuestObjectiveHandler class: {obj.GetType().FullName}");
                }
                
                this.dispatcher.Add(questObjectiveAttribute.QuestObjectiveType, questObjectiveHandler);
            }
        }

        public IQuestObjectiveHandler Get(QuestObjectiveType questObjectiveType)
        {
            return this.dispatcher[questObjectiveType];
        }
    }
}
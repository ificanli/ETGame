using System.Collections.Generic;

namespace ET.Server
{
    [ChildOf(typeof(Quest))]
    public class QuestObjective: Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 完成数量
        /// </summary>
        public int Count;
    }
}
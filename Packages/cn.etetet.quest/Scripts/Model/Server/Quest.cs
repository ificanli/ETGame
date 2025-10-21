using System.Collections.Generic;

namespace ET.Server
{
    [ChildOf(typeof(QuestComponent))]
    public class Quest: Entity, IAwake
    {
        /// <summary>
        /// 任务当前状态
        /// </summary>
        public QuestStatus Status;
    }
}
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 客户端任务组件 - 管理玩家的任务数据和UI显示
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class QuestComponent : Entity, IAwake
    {
    }

    /// <summary>
    /// 客户端任务数据
    /// </summary>
    [ChildOf(typeof(QuestComponent))]
    public class Quest : Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 任务状态
        /// </summary>
        public QuestStatus Status;

        /// <summary>
        /// 任务目标数据列表
        /// </summary>
        public List<EntityRef<QuestObjective>> Objectives = new();

        /// <summary>
        /// 任务创建时间戳
        /// </summary>
        public long CreateTime;

        /// <summary>
        /// 任务完成时间戳
        /// </summary>
        public long CompleteTime;
    }

    /// <summary>
    /// 客户端任务目标数据
    /// </summary>
    [ChildOf(typeof(Quest))]
    public class QuestObjective : Entity, IAwake, IDestroy
    {
        public int Count;

        /// <summary>
        /// 需要完成的数量
        /// </summary>
        public int NeedCount;
    }

}
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 客户端任务组件 - 管理玩家的任务数据和UI显示
    /// </summary>
    [Module(ModuleName.Quest)]
    [ComponentOf(typeof(Scene))]
    public class ClientQuestComponent : Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 所有任务数据字典（任务配置ID -> 客户端任务数据）
        /// </summary>
        public Dictionary<int, EntityRef<ClientQuestData>> QuestDict = new Dictionary<int, EntityRef<ClientQuestData>>();

        /// <summary>
        /// 当前跟踪的任务ID
        /// </summary>
        public int TrackedQuestId;
    }

    /// <summary>
    /// 客户端任务数据
    /// </summary>
    [Module(ModuleName.Quest)]
    [ChildOf(typeof(ClientQuestComponent))]
    public class ClientQuestData : Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 任务配置ID
        /// </summary>
        public int QuestId;

        /// <summary>
        /// 任务状态
        /// </summary>
        public QuestStatus Status;

        /// <summary>
        /// 任务目标数据列表
        /// </summary>
        public List<EntityRef<ClientQuestObjectiveData>> Objectives = new List<EntityRef<ClientQuestObjectiveData>>();

        /// <summary>
        /// 是否是当前跟踪的任务
        /// </summary>
        public bool IsTracked;

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
    [Module(ModuleName.Quest)]
    [ChildOf(typeof(ClientQuestData))]
    public class ClientQuestObjectiveData : Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 目标配置ID
        /// </summary>
        public int ObjectiveId;

        /// <summary>
        /// 当前进度
        /// </summary>
        public int CurrentCount;

        /// <summary>
        /// 需要完成的数量
        /// </summary>
        public int RequiredCount;

        /// <summary>
        /// 是否已完成
        /// </summary>
        public bool IsCompleted;

        /// <summary>
        /// 目标描述文本
        /// </summary>
        public string Description;

    }

}
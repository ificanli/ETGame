using MemoryPack;
using System.Collections.Generic;

namespace ET
{
    // 接任务
    [MemoryPackable]
    [Message(Quest.C2M_AcceptQuest)]
    [ResponseType(nameof(M2C_AcceptQuest))]
    public partial class C2M_AcceptQuest : MessageObject, ILocationRequest
    {
        public static C2M_AcceptQuest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_AcceptQuest>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int QuestId { get; set; }

        [MemoryPackOrder(2)]
        public long NPCId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.QuestId = default;
            this.NPCId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Quest.M2C_AcceptQuest)]
    public partial class M2C_AcceptQuest : MessageObject, ILocationResponse
    {
        public static M2C_AcceptQuest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_AcceptQuest>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;

            ObjectPool.Recycle(this);
        }
    }

    // 交任务
    [MemoryPackable]
    [Message(Quest.C2M_SubmitQuest)]
    [ResponseType(nameof(M2C_SubmitQuest))]
    public partial class C2M_SubmitQuest : MessageObject, ILocationRequest
    {
        public static C2M_SubmitQuest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_SubmitQuest>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int QuestId { get; set; }

        [MemoryPackOrder(2)]
        public long NPCId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.QuestId = default;
            this.NPCId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Quest.M2C_SubmitQuest)]
    public partial class M2C_SubmitQuest : MessageObject, ILocationResponse
    {
        public static M2C_SubmitQuest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_SubmitQuest>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Quest.QuestObjectiveInfo)]
    public partial class QuestObjectiveInfo : MessageObject
    {
        public static QuestObjectiveInfo Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<QuestObjectiveInfo>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int QuestObjectiveId { get; set; }

        [MemoryPackOrder(1)]
        public int Count { get; set; }

        [MemoryPackOrder(2)]
        public int NeedCount { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.QuestObjectiveId = default;
            this.Count = default;
            this.NeedCount = default;

            ObjectPool.Recycle(this);
        }
    }

    // 更新任务信息
    [MemoryPackable]
    [Message(Quest.M2C_CreateQuest)]
    public partial class M2C_CreateQuest : MessageObject, IMessage
    {
        public static M2C_CreateQuest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_CreateQuest>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long QuestId { get; set; }

        [MemoryPackOrder(1)]
        public List<QuestObjectiveInfo> QuestObjective { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.QuestId = default;
            this.QuestObjective.Clear();

            ObjectPool.Recycle(this);
        }
    }

    // 更新任务目标
    [MemoryPackable]
    [Message(Quest.M2C_UpdateQuestObjective)]
    public partial class M2C_UpdateQuestObjective : MessageObject, IMessage
    {
        public static M2C_UpdateQuestObjective Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_UpdateQuestObjective>(isFromPool);
        }

        /// <summary>
        /// 任务Id
        /// </summary>
        [MemoryPackOrder(0)]
        public long QuestId { get; set; }

        [MemoryPackOrder(1)]
        public List<QuestObjectiveInfo> QuestObjective { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.QuestId = default;
            this.QuestObjective.Clear();

            ObjectPool.Recycle(this);
        }
    }

    // 更新任务信息
    [MemoryPackable]
    [Message(Quest.M2C_UpdateQuest)]
    public partial class M2C_UpdateQuest : MessageObject, IMessage
    {
        public static M2C_UpdateQuest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_UpdateQuest>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long QuestId { get; set; }

        /// <summary>
        /// 1:进行中, 2:已完成
        /// </summary>
        [MemoryPackOrder(1)]
        public int State { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.QuestId = default;
            this.State = default;

            ObjectPool.Recycle(this);
        }
    }

    // 同步任务数据请求
    [MemoryPackable]
    [Message(Quest.C2M_SyncQuestData)]
    [ResponseType(nameof(M2C_SyncQuestData))]
    public partial class C2M_SyncQuestData : MessageObject, ILocationRequest
    {
        public static C2M_SyncQuestData Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_SyncQuestData>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Quest.QuestInfo)]
    public partial class QuestInfo : MessageObject
    {
        public static QuestInfo Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<QuestInfo>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long QuestId { get; set; }

        /// <summary>
        /// 任务状态
        /// </summary>
        [MemoryPackOrder(1)]
        public int Status { get; set; }

        [MemoryPackOrder(2)]
        public List<QuestObjectiveInfo> Objectives { get; set; } = new();

        /// <summary>
        /// 接取时间
        /// </summary>
        [MemoryPackOrder(3)]
        public long AcceptTime { get; set; }

        /// <summary>
        /// 完成时间
        /// </summary>
        [MemoryPackOrder(4)]
        public long CompleteTime { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.QuestId = default;
            this.Status = default;
            this.Objectives.Clear();
            this.AcceptTime = default;
            this.CompleteTime = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Quest.M2C_SyncQuestData)]
    public partial class M2C_SyncQuestData : MessageObject, ILocationResponse
    {
        public static M2C_SyncQuestData Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_SyncQuestData>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        [MemoryPackOrder(3)]
        public List<QuestInfo> QuestList { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.QuestList.Clear();

            ObjectPool.Recycle(this);
        }
    }

    // 放弃任务
    [MemoryPackable]
    [Message(Quest.C2M_AbandonQuest)]
    [ResponseType(nameof(M2C_AbandonQuest))]
    public partial class C2M_AbandonQuest : MessageObject, ILocationRequest
    {
        public static C2M_AbandonQuest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_AbandonQuest>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int QuestId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.QuestId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Quest.M2C_AbandonQuest)]
    public partial class M2C_AbandonQuest : MessageObject, ILocationResponse
    {
        public static M2C_AbandonQuest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_AbandonQuest>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;

            ObjectPool.Recycle(this);
        }
    }

    // 查询可接取任务
    [MemoryPackable]
    [Message(Quest.C2M_QueryAvailableQuests)]
    [ResponseType(nameof(M2C_QueryAvailableQuests))]
    public partial class C2M_QueryAvailableQuests : MessageObject, ILocationRequest
    {
        public static C2M_QueryAvailableQuests Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_QueryAvailableQuests>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        /// <summary>
        /// NPC ID，为0时查询所有可接取任务
        /// </summary>
        [MemoryPackOrder(1)]
        public long NPCId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.NPCId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Quest.AvailableQuestInfo)]
    public partial class AvailableQuestInfo : MessageObject
    {
        public static AvailableQuestInfo Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<AvailableQuestInfo>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int QuestId { get; set; }

        [MemoryPackOrder(1)]
        public string QuestName { get; set; }

        [MemoryPackOrder(2)]
        public string QuestDesc { get; set; }

        [MemoryPackOrder(3)]
        public int QuestType { get; set; }

        [MemoryPackOrder(4)]
        public int RewardExp { get; set; }

        [MemoryPackOrder(5)]
        public int RewardGold { get; set; }

        /// <summary>
        /// 奖励道具ID列表
        /// </summary>
        [MemoryPackOrder(6)]
        public List<int> RewardItems { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.QuestId = default;
            this.QuestName = default;
            this.QuestDesc = default;
            this.QuestType = default;
            this.RewardExp = default;
            this.RewardGold = default;
            this.RewardItems.Clear();

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Quest.M2C_QueryAvailableQuests)]
    public partial class M2C_QueryAvailableQuests : MessageObject, ILocationResponse
    {
        public static M2C_QueryAvailableQuests Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_QueryAvailableQuests>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        [MemoryPackOrder(3)]
        public List<AvailableQuestInfo> AvailableQuests { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.AvailableQuests.Clear();

            ObjectPool.Recycle(this);
        }
    }

    // 任务完成通知
    [MemoryPackable]
    [Message(Quest.M2C_QuestComplete)]
    public partial class M2C_QuestComplete : MessageObject, IMessage
    {
        public static M2C_QuestComplete Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_QuestComplete>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int QuestId { get; set; }

        [MemoryPackOrder(1)]
        public int RewardExp { get; set; }

        [MemoryPackOrder(2)]
        public int RewardGold { get; set; }

        [MemoryPackOrder(3)]
        public List<int> RewardItems { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.QuestId = default;
            this.RewardExp = default;
            this.RewardGold = default;
            this.RewardItems.Clear();

            ObjectPool.Recycle(this);
        }
    }

    // 任务失败通知
    [MemoryPackable]
    [Message(Quest.M2C_QuestFailed)]
    public partial class M2C_QuestFailed : MessageObject, IMessage
    {
        public static M2C_QuestFailed Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_QuestFailed>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int QuestId { get; set; }

        [MemoryPackOrder(1)]
        public string Reason { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.QuestId = default;
            this.Reason = default;

            ObjectPool.Recycle(this);
        }
    }

    // 任务进度提示
    [MemoryPackable]
    [Message(Quest.M2C_QuestProgress)]
    public partial class M2C_QuestProgress : MessageObject, IMessage
    {
        public static M2C_QuestProgress Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_QuestProgress>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int QuestId { get; set; }

        /// <summary>
        /// 进度描述文本
        /// </summary>
        [MemoryPackOrder(1)]
        public string ProgressText { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.QuestId = default;
            this.ProgressText = default;

            ObjectPool.Recycle(this);
        }
    }

    // 获取任务详情
    [MemoryPackable]
    [Message(Quest.C2M_GetQuestDetail)]
    [ResponseType(nameof(M2C_GetQuestDetail))]
    public partial class C2M_GetQuestDetail : MessageObject, ILocationRequest
    {
        public static C2M_GetQuestDetail Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_GetQuestDetail>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int QuestId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.QuestId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Quest.QuestDetailInfo)]
    public partial class QuestDetailInfo : MessageObject
    {
        public static QuestDetailInfo Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<QuestDetailInfo>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int QuestId { get; set; }

        [MemoryPackOrder(1)]
        public string QuestName { get; set; }

        [MemoryPackOrder(2)]
        public string QuestDesc { get; set; }

        /// <summary>
        /// 任务背景故事
        /// </summary>
        [MemoryPackOrder(3)]
        public string QuestStory { get; set; }

        [MemoryPackOrder(4)]
        public int QuestType { get; set; }

        [MemoryPackOrder(5)]
        public int AcceptNPC { get; set; }

        [MemoryPackOrder(6)]
        public int SubmitNPC { get; set; }

        [MemoryPackOrder(7)]
        public int RewardExp { get; set; }

        [MemoryPackOrder(8)]
        public int RewardGold { get; set; }

        [MemoryPackOrder(9)]
        public List<int> RewardItems { get; set; } = new();

        /// <summary>
        /// 前置任务列表
        /// </summary>
        [MemoryPackOrder(10)]
        public List<int> PreQuests { get; set; } = new();

        /// <summary>
        /// 最低等级要求
        /// </summary>
        [MemoryPackOrder(11)]
        public int MinLevel { get; set; }

        /// <summary>
        /// 最高等级限制
        /// </summary>
        [MemoryPackOrder(12)]
        public int MaxLevel { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.QuestId = default;
            this.QuestName = default;
            this.QuestDesc = default;
            this.QuestStory = default;
            this.QuestType = default;
            this.AcceptNPC = default;
            this.SubmitNPC = default;
            this.RewardExp = default;
            this.RewardGold = default;
            this.RewardItems.Clear();
            this.PreQuests.Clear();
            this.MinLevel = default;
            this.MaxLevel = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Quest.M2C_GetQuestDetail)]
    public partial class M2C_GetQuestDetail : MessageObject, ILocationResponse
    {
        public static M2C_GetQuestDetail Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_GetQuestDetail>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        [MemoryPackOrder(3)]
        public QuestDetailInfo QuestDetail { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.QuestDetail = default;

            ObjectPool.Recycle(this);
        }
    }

    public static class Quest
    {
        public const ushort C2M_AcceptQuest = 6001;
        public const ushort M2C_AcceptQuest = 6002;
        public const ushort C2M_SubmitQuest = 6003;
        public const ushort M2C_SubmitQuest = 6004;
        public const ushort QuestObjectiveInfo = 6005;
        public const ushort M2C_CreateQuest = 6006;
        public const ushort M2C_UpdateQuestObjective = 6007;
        public const ushort M2C_UpdateQuest = 6008;
        public const ushort C2M_SyncQuestData = 6009;
        public const ushort QuestInfo = 6010;
        public const ushort M2C_SyncQuestData = 6011;
        public const ushort C2M_AbandonQuest = 6012;
        public const ushort M2C_AbandonQuest = 6013;
        public const ushort C2M_QueryAvailableQuests = 6014;
        public const ushort AvailableQuestInfo = 6015;
        public const ushort M2C_QueryAvailableQuests = 6016;
        public const ushort M2C_QuestComplete = 6017;
        public const ushort M2C_QuestFailed = 6018;
        public const ushort M2C_QuestProgress = 6019;
        public const ushort C2M_GetQuestDetail = 6020;
        public const ushort QuestDetailInfo = 6021;
        public const ushort M2C_GetQuestDetail = 6022;
    }
}
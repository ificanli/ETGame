using MemoryPack;
using System.Collections.Generic;

namespace ET
{
    // 获取成就列表
    [MemoryPackable]
    [Message(Opcode.C2M_GetAchievements)]
    [ResponseType(nameof(M2C_GetAchievements))]
    public partial class C2M_GetAchievements : MessageObject, ILocationRequest
    {
        public static C2M_GetAchievements Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_GetAchievements>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        /// <summary>
        /// 分类ID，0表示获取所有
        /// </summary>
        [MemoryPackOrder(1)]
        public int CategoryId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.CategoryId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.AchievementInfo)]
    public partial class AchievementInfo : MessageObject
    {
        public static AchievementInfo Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<AchievementInfo>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int AchievementId { get; set; }

        /// <summary>
        /// 成就状态：0=未开始，1=进行中，2=已完成，3=已领取
        /// </summary>
        [MemoryPackOrder(1)]
        public int Status { get; set; }

        /// <summary>
        /// 当前进度
        /// </summary>
        [MemoryPackOrder(2)]
        public int Progress { get; set; }

        /// <summary>
        /// 最大进度
        /// </summary>
        [MemoryPackOrder(3)]
        public int MaxProgress { get; set; }

        /// <summary>
        /// 完成时间
        /// </summary>
        [MemoryPackOrder(4)]
        public long CompleteTime { get; set; }

        /// <summary>
        /// 领取时间
        /// </summary>
        [MemoryPackOrder(5)]
        public long ClaimTime { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.AchievementId = default;
            this.Status = default;
            this.Progress = default;
            this.MaxProgress = default;
            this.CompleteTime = default;
            this.ClaimTime = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.M2C_GetAchievements)]
    public partial class M2C_GetAchievements : MessageObject, ILocationResponse
    {
        public static M2C_GetAchievements Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_GetAchievements>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        [MemoryPackOrder(3)]
        public List<AchievementInfo> Achievements { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.Achievements.Clear();

            ObjectPool.Recycle(this);
        }
    }

    // 领取成就奖励
    [MemoryPackable]
    [Message(Opcode.C2M_ClaimAchievement)]
    [ResponseType(nameof(M2C_ClaimAchievement))]
    public partial class C2M_ClaimAchievement : MessageObject, ILocationRequest
    {
        public static C2M_ClaimAchievement Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_ClaimAchievement>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int AchievementId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.AchievementId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.AchievementReward)]
    public partial class AchievementReward : MessageObject
    {
        public static AchievementReward Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<AchievementReward>(isFromPool);
        }

        /// <summary>
        /// 奖励类型：1=经验，2=金币，3=道具
        /// </summary>
        [MemoryPackOrder(0)]
        public int Type { get; set; }

        /// <summary>
        /// 道具ID（奖励类型为道具时使用）
        /// </summary>
        [MemoryPackOrder(1)]
        public int ItemId { get; set; }

        /// <summary>
        /// 数量
        /// </summary>
        [MemoryPackOrder(2)]
        public int Count { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Type = default;
            this.ItemId = default;
            this.Count = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.M2C_ClaimAchievement)]
    public partial class M2C_ClaimAchievement : MessageObject, ILocationResponse
    {
        public static M2C_ClaimAchievement Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_ClaimAchievement>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        [MemoryPackOrder(3)]
        public List<AchievementReward> Rewards { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.Rewards.Clear();

            ObjectPool.Recycle(this);
        }
    }

    // 成就进度更新通知
    [MemoryPackable]
    [Message(Opcode.M2C_AchievementProgress)]
    public partial class M2C_AchievementProgress : MessageObject, IMessage
    {
        public static M2C_AchievementProgress Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_AchievementProgress>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int AchievementId { get; set; }

        [MemoryPackOrder(1)]
        public int Progress { get; set; }

        [MemoryPackOrder(2)]
        public int MaxProgress { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.AchievementId = default;
            this.Progress = default;
            this.MaxProgress = default;

            ObjectPool.Recycle(this);
        }
    }

    // 成就完成通知
    [MemoryPackable]
    [Message(Opcode.M2C_AchievementComplete)]
    public partial class M2C_AchievementComplete : MessageObject, IMessage
    {
        public static M2C_AchievementComplete Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_AchievementComplete>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int AchievementId { get; set; }

        [MemoryPackOrder(1)]
        public string AchievementName { get; set; }

        [MemoryPackOrder(2)]
        public long CompleteTime { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.AchievementId = default;
            this.AchievementName = default;
            this.CompleteTime = default;

            ObjectPool.Recycle(this);
        }
    }

    // 获取成就详情
    [MemoryPackable]
    [Message(Opcode.C2M_GetAchievementDetail)]
    [ResponseType(nameof(M2C_GetAchievementDetail))]
    public partial class C2M_GetAchievementDetail : MessageObject, ILocationRequest
    {
        public static C2M_GetAchievementDetail Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_GetAchievementDetail>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int AchievementId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.AchievementId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.AchievementDetailInfo)]
    public partial class AchievementDetailInfo : MessageObject
    {
        public static AchievementDetailInfo Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<AchievementDetailInfo>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int AchievementId { get; set; }

        [MemoryPackOrder(1)]
        public string AchievementName { get; set; }

        [MemoryPackOrder(2)]
        public string Description { get; set; }

        [MemoryPackOrder(3)]
        public string Icon { get; set; }

        [MemoryPackOrder(4)]
        public int CategoryId { get; set; }

        /// <summary>
        /// 成就类型
        /// </summary>
        [MemoryPackOrder(5)]
        public int Type { get; set; }

        [MemoryPackOrder(6)]
        public int MaxProgress { get; set; }

        [MemoryPackOrder(7)]
        public List<AchievementReward> Rewards { get; set; } = new();

        /// <summary>
        /// 前置成就
        /// </summary>
        [MemoryPackOrder(8)]
        public List<int> PreAchievements { get; set; } = new();

        /// <summary>
        /// 成就点数
        /// </summary>
        [MemoryPackOrder(9)]
        public int Points { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.AchievementId = default;
            this.AchievementName = default;
            this.Description = default;
            this.Icon = default;
            this.CategoryId = default;
            this.Type = default;
            this.MaxProgress = default;
            this.Rewards.Clear();
            this.PreAchievements.Clear();
            this.Points = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.M2C_GetAchievementDetail)]
    public partial class M2C_GetAchievementDetail : MessageObject, ILocationResponse
    {
        public static M2C_GetAchievementDetail Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_GetAchievementDetail>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        [MemoryPackOrder(3)]
        public AchievementDetailInfo Detail { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.Detail = default;

            ObjectPool.Recycle(this);
        }
    }

    // 获取成就分类
    [MemoryPackable]
    [Message(Opcode.C2M_GetAchievementCategories)]
    [ResponseType(nameof(M2C_GetAchievementCategories))]
    public partial class C2M_GetAchievementCategories : MessageObject, ILocationRequest
    {
        public static C2M_GetAchievementCategories Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_GetAchievementCategories>(isFromPool);
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
    [Message(Opcode.AchievementCategoryInfo)]
    public partial class AchievementCategoryInfo : MessageObject
    {
        public static AchievementCategoryInfo Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<AchievementCategoryInfo>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int CategoryId { get; set; }

        [MemoryPackOrder(1)]
        public string CategoryName { get; set; }

        [MemoryPackOrder(2)]
        public string Icon { get; set; }

        /// <summary>
        /// 排序
        /// </summary>
        [MemoryPackOrder(3)]
        public int Order { get; set; }

        /// <summary>
        /// 该分类下总成就数
        /// </summary>
        [MemoryPackOrder(4)]
        public int TotalCount { get; set; }

        /// <summary>
        /// 已完成数量
        /// </summary>
        [MemoryPackOrder(5)]
        public int CompletedCount { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.CategoryId = default;
            this.CategoryName = default;
            this.Icon = default;
            this.Order = default;
            this.TotalCount = default;
            this.CompletedCount = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.M2C_GetAchievementCategories)]
    public partial class M2C_GetAchievementCategories : MessageObject, ILocationResponse
    {
        public static M2C_GetAchievementCategories Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_GetAchievementCategories>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        [MemoryPackOrder(3)]
        public List<AchievementCategoryInfo> Categories { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.Categories.Clear();

            ObjectPool.Recycle(this);
        }
    }

    // 成就统计信息
    [MemoryPackable]
    [Message(Opcode.C2M_GetAchievementStats)]
    [ResponseType(nameof(M2C_GetAchievementStats))]
    public partial class C2M_GetAchievementStats : MessageObject, ILocationRequest
    {
        public static C2M_GetAchievementStats Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_GetAchievementStats>(isFromPool);
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
    [Message(Opcode.AchievementStatsInfo)]
    public partial class AchievementStatsInfo : MessageObject
    {
        public static AchievementStatsInfo Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<AchievementStatsInfo>(isFromPool);
        }

        /// <summary>
        /// 总成就数
        /// </summary>
        [MemoryPackOrder(0)]
        public int TotalAchievements { get; set; }

        /// <summary>
        /// 已完成数
        /// </summary>
        [MemoryPackOrder(1)]
        public int CompletedAchievements { get; set; }

        /// <summary>
        /// 总成就点数
        /// </summary>
        [MemoryPackOrder(2)]
        public int TotalPoints { get; set; }

        /// <summary>
        /// 已获得点数
        /// </summary>
        [MemoryPackOrder(3)]
        public int EarnedPoints { get; set; }

        /// <summary>
        /// 最近完成的成就ID列表
        /// </summary>
        [MemoryPackOrder(4)]
        public List<int> RecentAchievements { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.TotalAchievements = default;
            this.CompletedAchievements = default;
            this.TotalPoints = default;
            this.EarnedPoints = default;
            this.RecentAchievements.Clear();

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.M2C_GetAchievementStats)]
    public partial class M2C_GetAchievementStats : MessageObject, ILocationResponse
    {
        public static M2C_GetAchievementStats Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_GetAchievementStats>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        [MemoryPackOrder(3)]
        public AchievementStatsInfo Stats { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.Stats = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.RobotCase_TriggerAchievementEvent_Request)]
    [ResponseType(nameof(RobotCase_TriggerAchievementEvent_Response))]
    public partial class RobotCase_TriggerAchievementEvent_Request : MessageObject, ILocationRequest, IRobotCaseMessage
    {
        public static RobotCase_TriggerAchievementEvent_Request Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<RobotCase_TriggerAchievementEvent_Request>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        /// <summary>
        /// 事件类型：1=击杀，2=升级，3=任务完成，4=道具收集，5=地图探索
        /// </summary>
        [MemoryPackOrder(1)]
        public int EventType { get; set; }

        /// <summary>
        /// 参数ID（怪物ID、等级、任务ID、道具ID、地图ID）
        /// </summary>
        [MemoryPackOrder(2)]
        public int ParamId { get; set; }

        /// <summary>
        /// 数量（击杀数量、道具数量等）
        /// </summary>
        [MemoryPackOrder(3)]
        public int Count { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.EventType = default;
            this.ParamId = default;
            this.Count = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.RobotCase_TriggerAchievementEvent_Response)]
    public partial class RobotCase_TriggerAchievementEvent_Response : MessageObject, ILocationResponse, IRobotCaseMessage
    {
        public static RobotCase_TriggerAchievementEvent_Response Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<RobotCase_TriggerAchievementEvent_Response>(isFromPool);
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

    public static partial class Opcode
    {
        public const ushort C2M_GetAchievements = 10601;
        public const ushort AchievementInfo = 10602;
        public const ushort M2C_GetAchievements = 10603;
        public const ushort C2M_ClaimAchievement = 10604;
        public const ushort AchievementReward = 10605;
        public const ushort M2C_ClaimAchievement = 10606;
        public const ushort M2C_AchievementProgress = 10607;
        public const ushort M2C_AchievementComplete = 10608;
        public const ushort C2M_GetAchievementDetail = 10609;
        public const ushort AchievementDetailInfo = 10610;
        public const ushort M2C_GetAchievementDetail = 10611;
        public const ushort C2M_GetAchievementCategories = 10612;
        public const ushort AchievementCategoryInfo = 10613;
        public const ushort M2C_GetAchievementCategories = 10614;
        public const ushort C2M_GetAchievementStats = 10615;
        public const ushort AchievementStatsInfo = 10616;
        public const ushort M2C_GetAchievementStats = 10617;
        public const ushort RobotCase_TriggerAchievementEvent_Request = 10618;
        public const ushort RobotCase_TriggerAchievementEvent_Response = 10619;
    }
}
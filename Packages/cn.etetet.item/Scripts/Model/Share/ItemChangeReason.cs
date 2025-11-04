namespace ET
{
    /// <summary>
    /// 道具变化原因枚举
    /// </summary>
    public enum ItemChangeReason
    {
        /// <summary>
        /// 未指定
        /// </summary>
        None = 0,

        /// <summary>
        /// GM命令
        /// </summary>
        GM = 1,

        /// <summary>
        /// 初始道具
        /// </summary>
        InitialItem = 2,

        /// <summary>
        /// 商店购买
        /// </summary>
        ShopBuy = 3,

        /// <summary>
        /// 任务奖励
        /// </summary>
        QuestReward = 4,

        /// <summary>
        /// 使用道具
        /// </summary>
        UseItem = 5,

        /// <summary>
        /// 丢弃道具
        /// </summary>
        DropItem = 6,

        /// <summary>
        /// 移动道具
        /// </summary>
        MoveItem = 7,

        /// <summary>
        /// 整理背包
        /// </summary>
        SortBag = 8,

        /// <summary>
        /// 怪物掉落
        /// </summary>
        MonsterDrop = 9,

        /// <summary>
        /// 邮件奖励
        /// </summary>
        MailReward = 10,

        /// <summary>
        /// 交易
        /// </summary>
        Trade = 11,

        /// <summary>
        /// 分解
        /// </summary>
        Decompose = 12,

        /// <summary>
        /// 合成
        /// </summary>
        Synthesis = 13,

        /// <summary>
        /// 升级
        /// </summary>
        Upgrade = 14,

        /// <summary>
        /// 活动奖励
        /// </summary>
        ActivityReward = 15,

        /// <summary>
        /// 系统奖励
        /// </summary>
        SystemReward = 16,
    }
}

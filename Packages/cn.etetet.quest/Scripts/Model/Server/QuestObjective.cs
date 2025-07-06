using System.Collections.Generic;

namespace ET.Server
{
    [Module(ModuleName.Quest)]
    [ChildOf(typeof(Quest))]
    public class QuestObjective: Entity, IAwake<int>, IDestroy
    {
        /// <summary>
        /// 配置表Id（唯一标识）
        /// </summary>
        public int ConfigId;

        /// <summary>
        /// 目标类型（如击杀、采集、对话等）
        /// </summary>
        public QuestObjectiveType ObjectiveType;

        /// <summary>
        /// 目标参数（如怪物Id、物品Id、NPC Id等，按配置表定义）
        /// </summary>
        public List<int> Params = new();

        /// <summary>
        /// 当前进度
        /// </summary>
        public int Progress;

        /// <summary>
        /// 目标所需数量
        /// </summary>
        public int TargetCount;

        /// <summary>
        /// 是否已完成
        /// </summary>
        public bool IsCompleted;
    }
}
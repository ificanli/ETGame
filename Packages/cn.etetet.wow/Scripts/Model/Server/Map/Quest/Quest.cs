using System.Collections.Generic;

namespace ET.Server
{
    
    [ChildOf(typeof(QuestComponent))]
    public class Quest: Entity, IAwake<int>
    {
        /// <summary>
        /// 配置表Id（唯一标识）
        /// </summary>
        public int ConfigId;
        
        /// <summary>
        /// 任务当前状态
        /// </summary>
        public QuestStatus Status;

        /// <summary>
        /// 任务进度（目标Id->当前进度）
        /// </summary>
        public Dictionary<int, int> Progress = new();

        /// <summary>
        /// 前置任务Id列表
        /// </summary>
        public List<int> PreQuestIds = new();

        /// <summary>
        /// 后续任务Id列表
        /// </summary>
        public List<int> NextQuestIds = new();

        /// <summary>
        /// 任务目标Id列表
        /// </summary>
        public List<int> ObjectiveIds = new();

        /// <summary>
        /// 任务奖励Id列表
        /// </summary>
        public List<int> RewardIds = new();

        /// <summary>
        /// 是否可重复接取
        /// </summary>
        public bool IsRepeatable;

        /// <summary>
        /// 是否自动接取
        /// </summary>
        public bool IsAutoAccept;

        /// <summary>
        /// 是否自动提交
        /// </summary>
        public bool IsAutoSubmit;

        /// <summary>
        /// 任务接取条件（可扩展为条件Id列表）
        /// </summary>
        public List<int> AcceptConditionIds = new();

        /// <summary>
        /// 任务提交条件（可扩展为条件Id列表）
        /// </summary>
        public List<int> SubmitConditionIds = new();
    }
}
using System.Collections.Generic;

namespace ET
{
    [System.Serializable]
    public partial class BuffConfig: ProtoObject
    {
        /// <summary>持续时间</summary>
        public int Duration;

        /// <summary>Tick间隔时间</summary>
        public int TickTime;

        /// <summary>最大层数</summary>
        public int MaxStack = 1;

        /// <summary>叠加规则类型</summary>
        public int OverLayRuleType;

        /// <summary>移除条件</summary>
        public List<BuffFlags> Flags;

        /// <summary>广播客户端类型</summary>
        public NoticeType NoticeType;
        
        /// <summary>效果</summary>
#if UNITY
        [UnityEngine.SerializeReference]
#endif
        public List<EffectConfig> Effects = new();
    }
}
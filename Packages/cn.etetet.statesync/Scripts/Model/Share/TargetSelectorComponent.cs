using System.Collections.Generic;
using Unity.Mathematics;

namespace ET
{
    /// <summary>
    /// 目标选择组件，挂载在 Unit 上，负责自动索敌和目标管理。
    /// 支持：自动选择最近的敌人、手动指定目标、目标有效性验证。
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class TargetSelectorComponent : Entity, IAwake, IDestroy
    {
        /// <summary>当前目标的Unit ID（0表示无目标）</summary>
        public long CurrentTargetId;

        /// <summary>手动指定的目标ID（0表示无手动目标）</summary>
        public long ManualTargetId;

        /// <summary>上次选择目标的时间（毫秒）</summary>
        public long LastSelectTime;

        /// <summary>目标选择间隔（毫秒），默认1秒</summary>
        public int SelectIntervalMs;

        /// <summary>最大索敌范围（米）</summary>
        public float MaxRange;
    }
}

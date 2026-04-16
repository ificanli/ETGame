using UnityEngine;

namespace ET.Client
{
    public enum MonsterOutlineTier
    {
        None = 0,
        Normal = 1,
        EliteOrBoss = 2,
    }

    /// <summary>
    /// 客户端怪物描边状态组件。
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class MonsterOutlineVisualComponent : Entity, IAwake, IUpdate, IDestroy
    {
        public MonsterOutlineTier Tier;
        public Color OutlineColor;
        public float OutlineWidth;
        public bool Applied;
    }
}

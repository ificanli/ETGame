using Sirenix.OdinInspector;

namespace ET
{
    public enum BuffFlags
    {
        [LabelText("无")]
        None = 0,

        [LabelText("超时")]
        TimeoutRemove = 1,

        [LabelText("死亡")]
        DeadRemove = 2,

        [LabelText("进入战斗")]
        InBattleRemove = 3,

        [LabelText("离开战斗")]
        OutBattleRemove = 4,

        [LabelText("移动")]
        MoveRemove = 5,

        [LabelText("伤害")]
        DamageRemove = 6,

        [LabelText("当前技能")]
        CurrentSpellRemoveRemove = 7,

        [LabelText("骑乘")]
        RideRemove = 8,

        [LabelText("叠加")]
        StackRemove = 9,

        [LabelText("相同配置ID替换")]
        SameConfigIdReplaceRemove = 10,

        [LabelText("新技能打断")]
        NewSpellInterrupt = 11,

        [LabelText("无持续时间")]
        NoDurationRemove = 12,

        [LabelText("父Buff")]
        ParentRemoveRemove = 13,

        [LabelText("目标不存在")]
        NotFoundTargetRemove = 14,
    }
}
using Sirenix.OdinInspector;

namespace ET
{
    [LabelText("Buff 移除条件")]
    public enum BuffFlags
    {
        [LabelText("超时移除")]
        TimeoutRemove = 1,

        [LabelText("死亡移除")]
        DeadRemove = 2,

        [LabelText("进入战斗移除")]
        InBattleRemove = 3,

        [LabelText("离开战斗移除")]
        OutBattleRemove = 4,

        [LabelText("移动移除")]
        MoveRemove = 5,

        [LabelText("伤害移除")]
        DamageRemove = 6,

        [LabelText("当前技能移除")]
        CurrentSpellRemoveRemove = 7,

        [LabelText("骑乘移除")]
        RideRemove = 8,

        [LabelText("叠加移除")]
        StackRemove = 9,

        [LabelText("相同配置ID替换移除")]
        SameConfigIdReplaceRemove = 10,

        [LabelText("通道移除")]
        Channeling = 100,
    }
}
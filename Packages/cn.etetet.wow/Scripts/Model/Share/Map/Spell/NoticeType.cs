using Sirenix.OdinInspector;

namespace ET
{
    [LabelText("广播客户端类型")]
    public enum NoticeType
    {
        [LabelText("无")]
        NoNotice,

        [LabelText("自己")]
        Self,

        [LabelText("全体")]
        Broadcast,

        [LabelText("除自己外")]
        BroadcastWithoutSelf
    }
}
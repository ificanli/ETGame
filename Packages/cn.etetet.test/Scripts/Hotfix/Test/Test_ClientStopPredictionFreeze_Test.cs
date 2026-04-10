namespace ET.Test
{
    /// <summary>
    /// 旧的 PendingStop/Freeze 测试已随 Replay 架构一起移除。
    /// 停步行为现在由 Test_ClientInputReplayAckAdvance_Test（零输入守卫）覆盖。
    /// 保留此文件和类名以避免测试注册表报错。
    /// </summary>
    public class Test_ClientStopPredictionFreeze_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            Log.Console("stop prediction freeze test superseded by simplified move system");
            return ErrorCode.ERR_Success;
        }
    }
}

using System;

namespace ET.Server
{
    [Event(SceneType.Map)]
    public class UnitBeforeTransfer_RogueCleanup : AEvent<Scene, UnitBeforeTransfer>
    {
        protected override async ETTask Run(Scene scene, UnitBeforeTransfer args)
        {
            Unit unit = args.Unit;
            if (unit == null || unit.IsDisposed || !args.ChangeScene)
            {
                await ETTask.CompletedTask;
                return;
            }

            if (!string.Equals(args.TargetMapName, "Home", StringComparison.OrdinalIgnoreCase))
            {
                await ETTask.CompletedTask;
                return;
            }

            RogueProgressHelper.ClearRogueRuntime(unit, true);
            await ETTask.CompletedTask;
        }
    }
}

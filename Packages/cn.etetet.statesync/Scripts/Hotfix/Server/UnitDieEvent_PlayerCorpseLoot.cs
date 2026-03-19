namespace ET.Server
{
    [Event(SceneType.Map)]
    public class UnitDieEvent_PlayerCorpseLoot : AEvent<Scene, UnitDie>
    {
        protected override async ETTask Run(Scene scene, UnitDie args)
        {
            Unit target = args.Target;
            if (target != null && !target.IsDisposed)
            {
                PlayerCorpseLootHelper.TryCreateCorpse(target);
            }

            await ETTask.CompletedTask;
        }
    }

    [Event(SceneType.All)]
    public class ItemDiscardToGroundEvent_CreatePoint : AEvent<Scene, ItemDiscardToGroundEvent>
    {
        protected override async ETTask Run(Scene scene, ItemDiscardToGroundEvent args)
        {
            ItemDiscardToGroundContext context = args.Context;
            if (context == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            Unit player = context.Player;
            if (player == null || player.IsDisposed || player.Scene() != scene)
            {
                context.ResultError = ErrorCode.ERR_Cancel;
                await ETTask.CompletedTask;
                return;
            }

            if (!PlayerCorpseLootHelper.TryCreateGroundDrop(player, context.ItemConfigId, context.Count, out string pointId, out long pointUnitId))
            {
                context.ResultError = ErrorCode.ERR_Cancel;
                await ETTask.CompletedTask;
                return;
            }

            context.ResultPointId = pointId;
            context.ResultPointUnitId = pointUnitId;
            context.ResultError = ErrorCode.ERR_Success;
            await ETTask.CompletedTask;
        }
    }
}

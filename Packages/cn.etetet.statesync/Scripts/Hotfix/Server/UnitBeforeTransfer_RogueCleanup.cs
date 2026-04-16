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

    [Event(SceneType.Map)]
    public class PlayerEnterMap_RunTimeLimit : AEvent<Scene, PlayerEnterMap>
    {
        protected override async ETTask Run(Scene scene, PlayerEnterMap args)
        {
            Unit unit = args.Unit;
            if (unit == null || unit.IsDisposed || unit.UnitType != UnitType.Player)
            {
                await ETTask.CompletedTask;
                return;
            }

            string mapName = args.MapName ?? unit.Scene()?.Name.GetSceneConfigName() ?? string.Empty;
            if (string.Equals(mapName, "Home", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(mapName, "GateMap", StringComparison.OrdinalIgnoreCase))
            {
                RunTimeLimitComponent existing = unit.GetComponent<RunTimeLimitComponent>();
                if (existing != null)
                {
                    unit.RemoveComponent<RunTimeLimitComponent>();
                }

                await ETTask.CompletedTask;
                return;
            }

            RunTimeLimitComponent current = unit.GetComponent<RunTimeLimitComponent>();
            if (current != null)
            {
                unit.RemoveComponent<RunTimeLimitComponent>();
            }

            long durationMs = RogueRunTimeLimitHelper.GetEffectiveDurationMs(unit);
            unit.AddComponent<RunTimeLimitComponent, long, string>(durationMs, mapName);
            RunTimeLimitMessageHelper.SyncState(unit, unit.GetComponent<RunTimeLimitComponent>());
            Log.Info($"[RunTimeLimit] start countdown, unitId={unit.Id}, map={mapName}, durationMs={durationMs}");
            await ETTask.CompletedTask;
        }
    }
}

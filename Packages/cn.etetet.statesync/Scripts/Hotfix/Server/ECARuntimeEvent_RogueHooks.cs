using System.Collections.Generic;

namespace ET.Server
{
    [Event(SceneType.Map)]
    public class EvacuationDurationAdjustEvent_RogueExtendGameTime : AEvent<Scene, EvacuationDurationAdjustEvent>
    {
        protected override async ETTask Run(Scene scene, EvacuationDurationAdjustEvent args)
        {
            RogueEcaRuntimeHookHelper.ApplyExtendGameTime(args.Context);
            await ETTask.CompletedTask;
        }
    }

    [Event(SceneType.Map)]
    public class ContainerLootBuildEvent_RogueContainerResult : AEvent<Scene, ContainerLootBuildEvent>
    {
        protected override async ETTask Run(Scene scene, ContainerLootBuildEvent args)
        {
            RogueEcaRuntimeHookHelper.ApplyContainerResultProbability(args.Context);
            await ETTask.CompletedTask;
        }
    }

    [Event(SceneType.Map)]
    public class ContainerOpenedEvent_RogueContainerLowQualityGold : AEvent<Scene, ContainerOpenedEvent>
    {
        protected override async ETTask Run(Scene scene, ContainerOpenedEvent args)
        {
            if (args.IsFirstOpen)
            {
                RogueEcaRuntimeHookHelper.TryGrantContainerLowQualityGold(args.Point, args.Player);
            }

            await ETTask.CompletedTask;
        }
    }

    [Event(SceneType.Map)]
    public class SearchMonsterSpawnEvent_RogueSearchMonsterProbability : AEvent<Scene, SearchMonsterSpawnEvent>
    {
        protected override async ETTask Run(Scene scene, SearchMonsterSpawnEvent args)
        {
            RogueEcaRuntimeHookHelper.ApplySearchMonsterProbability(args.Context);
            await ETTask.CompletedTask;
        }
    }

    [Event(SceneType.Map)]
    public class DoorKeyConsumedEvent_RogueTemporaryItem : AEvent<Scene, DoorKeyConsumedEvent>
    {
        protected override async ETTask Run(Scene scene, DoorKeyConsumedEvent args)
        {
            RogueEcaRuntimeHookHelper.ConsumeTemporaryDoorKey(args.Player, args.ItemConfigId, args.Count);
            await ETTask.CompletedTask;
        }
    }

    public static class RogueEcaRuntimeHookHelper
    {
        public static void ApplyExtendGameTime(EvacuationDurationAdjustContext context)
        {
            Unit player = context?.Player;
            if (context == null || player == null || player.IsDisposed || player.UnitType != UnitType.Player)
            {
                return;
            }

            long extendTimeMs = RogueEffectQueryHelper.GetExtendGameTimeMs(player);
            if (extendTimeMs > 0)
            {
                context.DurationMs += extendTimeMs;
            }
        }

        public static void ApplyContainerResultProbability(ContainerLootBuildContext context)
        {
            Unit player = context?.Player;
            if (context == null || player == null || player.IsDisposed || player.UnitType != UnitType.Player)
            {
                return;
            }

            int multiplierPermille = RogueEffectQueryHelper.GetContainerResultProbabilityMultiplierPermille(player);
            if (multiplierPermille > 0)
            {
                context.ResultRollMultiplierPermille = context.ResultRollMultiplierPermille * multiplierPermille / 1000;
            }
        }

        public static int TryGrantContainerLowQualityGold(ECAPointComponent point, Unit player)
        {
            RogueProgressComponent progress = player?.GetComponent<RogueProgressComponent>();
            ContainerComponent container = point?.GetParent<Unit>()?.GetComponent<ContainerComponent>();
            if (player == null || player.IsDisposed || player.UnitType != UnitType.Player || progress == null || container == null)
            {
                return 0;
            }

            RogueEffectQueryHelper.GetContainerLowQualityGold(player, out int totalGold, out int highQualityThreshold);
            if (totalGold <= 0)
            {
                return 0;
            }

            foreach (KeyValuePair<int, ContainerItemEntry> kv in container.ItemEntries)
            {
                ItemConfig itemConfig = ItemConfigCategory.Instance.Get(kv.Value.ConfigId);
                if (itemConfig != null && itemConfig.Quality >= highQualityThreshold)
                {
                    return 0;
                }
            }

            int finalDelta = RogueGoldHelper.GetGoldDeltaBySource(progress, totalGold, RogueGoldSourceType.RogueCard);
            if (finalDelta <= 0)
            {
                return 0;
            }

            RogueGoldHelper.AddGold(progress, finalDelta);
            RogueProgressHelper.SyncProgress(player, progress);
            return finalDelta;
        }

        public static void ApplySearchMonsterProbability(SearchMonsterSpawnContext context)
        {
            Unit player = context?.Player;
            if (context == null || player == null || player.IsDisposed || player.UnitType != UnitType.Player)
            {
                return;
            }

            int multiplierPermille = RogueEffectQueryHelper.GetSearchMonsterProbabilityMultiplierPermille(player);
            if (multiplierPermille > 0)
            {
                context.ChancePermille = context.ChancePermille * multiplierPermille / 1000;
            }
        }

        public static void ConsumeTemporaryDoorKey(Unit player, int itemConfigId, int count)
        {
            RogueTemporaryItemStateComponent stateComponent = player?.GetComponent<RogueTemporaryItemStateComponent>();
            if (player == null || player.IsDisposed || stateComponent == null || itemConfigId <= 0 || count <= 0)
            {
                return;
            }

            stateComponent.ConsumeItem(itemConfigId, count);
            if (stateComponent.IsEmpty())
            {
                player.RemoveComponent<RogueTemporaryItemStateComponent>();
            }
        }
    }
}

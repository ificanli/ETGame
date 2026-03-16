using System;
using System.Collections.Generic;
using ET;

namespace ET.Server
{
    [Invoke]
    public class ECAFlowActionInvokeHandler : AInvokeHandler<ECAFlowActionInvoke, ETTask>
    {
        private const string ParamState = "state";
        private const string ParamActive = "active";
        private const string ParamSeconds = "seconds";
        private const string ParamTimerId = "timer_id";
        private const string ParamLootTable = "loot_table";
        private const string ParamCount = "count";
        private const string ParamRadius = "radius";
        private const string ParamAllowRepeat = "allow_repeat";
        private const string ParamOutputMode = "output_mode";
        private const string ParamUiKey = "ui_key";
        private const string ParamButtonTextId = "button_text_id";
        private const string ParamButtonId = "button_id";
        private const string ParamCanInteract = "can_interact";
        private const string ParamGroupId = "group_id";
        private const string ParamMapName = "map_name";

        public override ETTask Handle(ECAFlowActionInvoke args)
        {
            if (string.IsNullOrWhiteSpace(args.Key))
            {
                return ETTask.CompletedTask;
            }

            RegisterHandlers();
            if (!ECAFlowActionRegistry.TryGet(args.Key, out Func<ECAFlowActionInvoke, ETTask> handler))
            {
                Log.Warning($"[ECAFlow] unknown action key: {args.Key}");
                return ETTask.CompletedTask;
            }

            return handler(args);
        }

        private static void RegisterHandlers()
        {
            if (ECAFlowActionRegistry.Count() > 0)
            {
                return;
            }

            ECAFlowActionRegistry.Register(ECAFlowActionKey.SetPointActive, HandleSetPointActiveAsync);
            ECAFlowActionRegistry.Register(ECAFlowActionKey.SetPointState, HandleSetPointStateAsync);
            ECAFlowActionRegistry.Register(ECAFlowActionKey.StartSearchTimer, HandleStartSearchTimerAsync);
            ECAFlowActionRegistry.Register(ECAFlowActionKey.ShowInteractButton, HandleShowInteractButtonAsync);
            ECAFlowActionRegistry.Register(ECAFlowActionKey.HideInteractButton, HandleHideInteractButtonAsync);
            ECAFlowActionRegistry.Register(ECAFlowActionKey.RefreshDoorInteractHint, HandleRefreshDoorInteractHintAsync);
            ECAFlowActionRegistry.Register(ECAFlowActionKey.ToggleDoor, HandleToggleDoorAsync);
            ECAFlowActionRegistry.Register(ECAFlowActionKey.ShowSearchUI, HandleShowSearchUiAsync);
            ECAFlowActionRegistry.Register(ECAFlowActionKey.OpenContainerUI, HandleOpenContainerUiAsync);
            ECAFlowActionRegistry.Register(ECAFlowActionKey.GenerateContainerLoot, HandleGenerateContainerLootAsync);
            ECAFlowActionRegistry.Register(ECAFlowActionKey.SpawnItemsToGround, HandleSpawnItemsToGroundAsync);
            ECAFlowActionRegistry.Register(ECAFlowActionKey.SpawnMonsters, HandleSpawnMonstersAsync);
            ECAFlowActionRegistry.Register(ECAFlowActionKey.StartEvacCountdown, HandleStartEvacCountdownAsync);
            ECAFlowActionRegistry.Register(ECAFlowActionKey.AllowEvacPlayers, HandleAllowEvacPlayersAsync);
            ECAFlowActionRegistry.Register(ECAFlowActionKey.TransferToLobby, HandleTransferToLobbyAsync);
            ECAFlowActionRegistry.Register(ECAFlowActionKey.ApplyStealth, HandleApplyStealthAsync);
            ECAFlowActionRegistry.Register(ECAFlowActionKey.RemoveStealth, HandleRemoveStealthAsync);
        }

        private static ETTask HandleSetPointActiveAsync(ECAFlowActionInvoke args)
        {
            ECAPointComponent point = args.Point;
            if (point == null)
            {
                return ETTask.CompletedTask;
            }

            if (!FlowParamHelper.TryGetBoolParam(args.Node, ParamActive, out bool active))
            {
                return ETTask.CompletedTask;
            }

            point.IsActive = active;
            Log.Info($"[ECAFlow] Point {point.PointId} active set to {active}");
            return ETTask.CompletedTask;
        }

        private static ETTask HandleSetPointStateAsync(ECAFlowActionInvoke args)
        {
            ECAPointComponent point = args.Point;
            if (point == null)
            {
                return ETTask.CompletedTask;
            }

            if (!FlowParamHelper.TryGetIntParam(args.Node, ParamState, out int state))
            {
                return ETTask.CompletedTask;
            }

            if (point.PointType == ECAPointType.Container)
            {
                ContainerComponent container = ContainerComponentSystem.GetOrAdd(point);
                if (container != null)
                {
                    container.State = state;
                }
            }

            ECAPointStateHelper.SetState(point, state);
            ContainerRuntimeHelper.NotifyPointStateToPlayers(point);
            if (point.PointType == ECAPointType.Door || point.PointType == ECAPointType.KeyDoor)
            {
                RefreshDoorInteractHintsForPlayersInRange(point);
            }

            Log.Info($"[ECAFlow] Point {point.PointId} state set to {state}");
            return ETTask.CompletedTask;
        }

        private static ETTask HandleStartSearchTimerAsync(ECAFlowActionInvoke args)
        {
            ECAPointComponent point = args.Point;
            Unit player = args.Player;
            if (point == null || player == null)
            {
                return ETTask.CompletedTask;
            }

            if (!FlowParamHelper.TryGetFloatParam(args.Node, ParamSeconds, out float searchSeconds) ||
                !FlowParamHelper.TryGetStringParam(args.Node, ParamTimerId, out string searchTimerId))
            {
                return ETTask.CompletedTask;
            }

            long durationMs = (long)(searchSeconds * 1000);
            if (!ECAFlowTimerHelper.StartTimer(point, player, searchTimerId, durationMs))
            {
                return ETTask.CompletedTask;
            }

            ContainerRuntimeHelper.MarkSearchStarted(point, player, searchTimerId, durationMs);
            return ETTask.CompletedTask;
        }

        private static ETTask HandleShowInteractButtonAsync(ECAFlowActionInvoke args)
        {
            ECAPointComponent point = args.Point;
            Unit player = args.Player;
            if (point == null || player == null)
            {
                return ETTask.CompletedTask;
            }

            int buttonTextId = 0;
            if (!FlowParamHelper.TryGetIntParam(args.Node, ParamButtonTextId, out buttonTextId))
            {
                FlowParamHelper.TryGetIntParam(args.Node, ParamButtonId, out buttonTextId);
            }

            if (!FlowParamHelper.TryGetBoolParam(args.Node, ParamCanInteract, out bool canInteract))
            {
                canInteract = true;
            }

            ContainerRuntimeHelper.SendInteractHint(point, player, true, buttonTextId, canInteract);
            return ETTask.CompletedTask;
        }

        private static ETTask HandleHideInteractButtonAsync(ECAFlowActionInvoke args)
        {
            ECAPointComponent point = args.Point;
            Unit player = args.Player;
            if (point == null || player == null)
            {
                return ETTask.CompletedTask;
            }

            ContainerRuntimeHelper.SendInteractHint(point, player, false);
            if (point.PointType == ECAPointType.Container)
            {
                ContainerRuntimeHelper.CancelSearch(point, player, notify: true);
            }

            return ETTask.CompletedTask;
        }

        private static ETTask HandleRefreshDoorInteractHintAsync(ECAFlowActionInvoke args)
        {
            ECAPointComponent point = args.Point;
            Unit player = args.Player;
            if (point == null || player == null)
            {
                return ETTask.CompletedTask;
            }

            RefreshDoorInteractHint(point, player);
            return ETTask.CompletedTask;
        }

        private static ETTask HandleToggleDoorAsync(ECAFlowActionInvoke args)
        {
            ECAPointComponent point = args.Point;
            Unit player = args.Player;
            if (point == null || player == null)
            {
                return ETTask.CompletedTask;
            }

            ToggleDoor(point, player);
            return ETTask.CompletedTask;
        }

        private static ETTask HandleShowSearchUiAsync(ECAFlowActionInvoke args)
        {
            ECAPointComponent point = args.Point;
            Unit player = args.Player;
            if (point == null || player == null)
            {
                return ETTask.CompletedTask;
            }

            ContainerRuntimeHelper.SendSearchState(point, player, ContainerSearchState.Searching, 0);
            return ETTask.CompletedTask;
        }

        private static ETTask HandleOpenContainerUiAsync(ECAFlowActionInvoke args)
        {
            ECAPointComponent point = args.Point;
            Unit player = args.Player;
            if (point == null || player == null)
            {
                return ETTask.CompletedTask;
            }

            FlowParamHelper.TryGetStringParam(args.Node, ParamUiKey, out string uiKey);
            ContainerRuntimeHelper.OpenContainerUI(point, player, uiKey);
            return ETTask.CompletedTask;
        }

        private static ETTask HandleGenerateContainerLootAsync(ECAFlowActionInvoke args)
        {
            ECAPointComponent point = args.Point;
            Unit player = args.Player;
            if (point == null)
            {
                return ETTask.CompletedTask;
            }

            if (!FlowParamHelper.TryGetStringParam(args.Node, ParamLootTable, out string lootTable) ||
                !TryResolveRandomCount(args.Node, out int count) ||
                !FlowParamHelper.TryGetFloatParam(args.Node, ParamRadius, out float radius))
            {
                return ETTask.CompletedTask;
            }

            FlowParamHelper.TryGetStringParam(args.Node, ParamOutputMode, out string outputMode);
            bool allowRepeat = GetAllowRepeat(args.Node);
            ContainerRuntimeHelper.GenerateLoot(point, player, outputMode, lootTable, count, radius, allowRepeat);
            return ETTask.CompletedTask;
        }

        private static ETTask HandleSpawnItemsToGroundAsync(ECAFlowActionInvoke args)
        {
            ECAPointComponent point = args.Point;
            Unit player = args.Player;
            if (point == null)
            {
                return ETTask.CompletedTask;
            }

            if (!FlowParamHelper.TryGetStringParam(args.Node, ParamLootTable, out string lootTable) ||
                !TryResolveRandomCount(args.Node, out int count) ||
                !FlowParamHelper.TryGetFloatParam(args.Node, ParamRadius, out float radius))
            {
                return ETTask.CompletedTask;
            }

            bool allowRepeat = GetAllowRepeat(args.Node);
            ContainerRuntimeHelper.GenerateLoot(point, player, ContainerOutputMode.GroundDrop.ToString(), lootTable, count, radius, allowRepeat);
            Log.Info($"[ECAFlow] Point {point.PointId} spawn items request (compat): {lootTable}, count={count}, radius={radius}");
            return ETTask.CompletedTask;
        }

        private static bool TryResolveRandomCount(FlowNodeData node, out int count)
        {
            count = 0;
            if (!FlowParamHelper.TryGetStringParam(node, ParamCount, out string rawCount))
            {
                return false;
            }

            if (!TryParseRandomCountRange(rawCount, out int minCount, out int maxCount))
            {
                return false;
            }

            count = minCount == maxCount ? minCount : RandomGenerator.RandomNumber(minCount, maxCount + 1);
            return count > 0;
        }

        private static bool GetAllowRepeat(FlowNodeData node)
        {
            if (!FlowParamHelper.TryGetBoolParam(node, ParamAllowRepeat, out bool allowRepeat))
            {
                allowRepeat = true;
            }

            return allowRepeat;
        }

        private static bool TryParseRandomCountRange(string rawCount, out int minCount, out int maxCount)
        {
            minCount = 0;
            maxCount = 0;
            if (string.IsNullOrWhiteSpace(rawCount))
            {
                return false;
            }

            string trimmed = rawCount.Trim();
            if (int.TryParse(trimmed, out int fixedCount))
            {
                if (fixedCount <= 0)
                {
                    return false;
                }

                minCount = fixedCount;
                maxCount = fixedCount;
                return true;
            }

            string[] parts = trimmed.Split(new[] { '-', '~' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                return false;
            }

            if (!int.TryParse(parts[0].Trim(), out minCount) || minCount <= 0)
            {
                return false;
            }

            if (!int.TryParse(parts[1].Trim(), out maxCount) || maxCount <= 0)
            {
                return false;
            }

            if (minCount > maxCount)
            {
                (minCount, maxCount) = (maxCount, minCount);
            }

            return true;
        }

        private static ETTask HandleSpawnMonstersAsync(ECAFlowActionInvoke args)
        {
            ECAPointComponent point = args.Point;
            Unit player = args.Player;
            if (!FlowParamHelper.TryGetStringParam(args.Node, ParamGroupId, out string groupId))
            {
                Log.Warning("[ECAFlow] SpawnMonsters: missing group_id parameter");
                return ETTask.CompletedTask;
            }

            if (!FlowParamHelper.TryGetIntParam(args.Node, ParamCount, out int spawnCount))
            {
                Log.Warning("[ECAFlow] SpawnMonsters: missing count parameter");
                return ETTask.CompletedTask;
            }

            if (point == null)
            {
                Log.Warning("[ECAFlow] SpawnMonsters: point is null");
                return ETTask.CompletedTask;
            }

            if (player != null &&
                point.PointType == ECAPointType.Container &&
                ECAInteractionModifierHelper.HasSilentSearch(player))
            {
                Log.Info($"[ECAFlow] SpawnMonsters skipped by silent search: player={player.Id}, point={point.PointId}, group={groupId}");
                return ETTask.CompletedTask;
            }

            Unit pointUnit = point.GetParent<Unit>();
            if (pointUnit == null)
            {
                Log.Warning($"[ECAFlow] SpawnMonsters: pointUnit is null for point {point.PointId}");
                return ETTask.CompletedTask;
            }

            Scene mapScene = pointUnit.Scene();
            int spawned = SpawnMonstersHelper.SpawnAtPoint(mapScene, pointUnit, groupId, spawnCount);
            Log.Info($"[ECAFlow] Point {point.PointId} spawned monsters: groupId={groupId}, spawned={spawned}/{spawnCount}");
            return ETTask.CompletedTask;
        }

        private static ETTask HandleStartEvacCountdownAsync(ECAFlowActionInvoke args)
        {
            ECAPointComponent point = args.Point;
            Unit player = args.Player;
            if (point == null || player == null)
            {
                return ETTask.CompletedTask;
            }

            long evacuationDurationMs = ECAConfig.DefaultEvacuationDurationMs;
            if (FlowParamHelper.TryGetFloatParam(args.Node, ParamSeconds, out float evacSeconds) && evacSeconds > 0f)
            {
                evacuationDurationMs = (long)(evacSeconds * 1000);
            }
            else if (FlowParamHelper.TryGetIntParam(point.Params, ECAPointParamKey.EvacuationDurationMs, out int configuredDurationMs) &&
                     configuredDurationMs > 0)
            {
                evacuationDurationMs = configuredDurationMs;
            }

            string lobbyMapName = FlowParamHelper.GetStringParamOrDefault(point.Params, ECAPointParamKey.LobbyMapName, ECAConfig.DefaultLobbyMapName);
            point.StartEvacuation(player, evacuationDurationMs, lobbyMapName);
            Log.Info($"[ECAFlow] Point {point.PointId} start evacuation countdown: player={player.Id}, durationMs={evacuationDurationMs}, lobbyMap={lobbyMapName}");
            return ETTask.CompletedTask;
        }

        private static ETTask HandleAllowEvacPlayersAsync(ECAFlowActionInvoke args)
        {
            ECAPointComponent point = args.Point;
            if (point == null)
            {
                return ETTask.CompletedTask;
            }

            point.IsActive = true;
            Log.Info($"[ECAFlow] Point {point.PointId} allow evacuation");
            return ETTask.CompletedTask;
        }

        private static async ETTask HandleTransferToLobbyAsync(ECAFlowActionInvoke args)
        {
            Unit player = args.Player;
            if (player == null)
            {
                return;
            }

            if (!FlowParamHelper.TryGetStringParam(args.Node, ParamMapName, out string mapName))
            {
                return;
            }

            await TransferHelper.TransferAtFrameFinish(player, mapName, 0);
        }

        private static ETTask HandleApplyStealthAsync(ECAFlowActionInvoke args)
        {
            ECAPointComponent point = args.Point;
            Unit player = args.Player;
            if (point == null || player == null)
            {
                return ETTask.CompletedTask;
            }

            point.Scene()?.GetComponent<ExtraUnitVisibilityComponent>()?.SetPlayerConcealmentState(point.PointId, player, true);
            return ETTask.CompletedTask;
        }

        private static ETTask HandleRemoveStealthAsync(ECAFlowActionInvoke args)
        {
            ECAPointComponent point = args.Point;
            Unit player = args.Player;
            if (point == null || player == null)
            {
                return ETTask.CompletedTask;
            }

            point.Scene()?.GetComponent<ExtraUnitVisibilityComponent>()?.SetPlayerConcealmentState(point.PointId, player, false);
            return ETTask.CompletedTask;
        }

        private static void RefreshDoorInteractHint(ECAPointComponent point, Unit player)
        {
            if (point == null || player == null || player.IsDisposed)
            {
                return;
            }

            bool canInteract = CanInteractDoor(point, player);
            int buttonTextId = ResolveDoorButtonTextId(point, canInteract);
            ContainerRuntimeHelper.SendInteractHint(point, player, true, buttonTextId, canInteract);
            ContainerRuntimeHelper.SendPointState(point, player);
        }

        private static void RefreshDoorInteractHintsForPlayersInRange(ECAPointComponent point)
        {
            if (point == null || point.IsDisposed)
            {
                return;
            }

            Scene scene = point.Scene();
            UnitComponent unitComponent = scene?.GetComponent<UnitComponent>();
            if (unitComponent == null)
            {
                return;
            }

            point.CleanupInvalidPlayersInRange(unitComponent);
            foreach (long playerId in point.PlayersInRange)
            {
                Unit player = unitComponent.Get(playerId);
                if (player == null || player.IsDisposed)
                {
                    continue;
                }

                RefreshDoorInteractHint(point, player);
            }
        }

        private static void ToggleDoor(ECAPointComponent point, Unit player)
        {
            if (point == null || point.IsDisposed || player == null || player.IsDisposed)
            {
                return;
            }

            int currentState = point.CurrentState;
            int nextState;
            switch (point.CurrentState)
            {
                case ECADoorState.Locked:
                    if (!TryConsumeDoorKey(point, player))
                    {
                        Log.Info($"[ECAServer][Door] toggle blocked by key: point={point.PointId}, player={player.Id}, state={point.CurrentState}");
                        RefreshDoorInteractHint(point, player);
                        return;
                    }

                    nextState = ECADoorState.Opened;
                    break;
                case ECADoorState.Opened:
                    nextState = ECADoorState.Closed;
                    break;
                default:
                    nextState = ECADoorState.Opened;
                    break;
            }

            Log.Info($"[ECAServer][Door] toggle request: point={point.PointId}, player={player.Id}, from={currentState}, to={nextState}, type={point.PointType}");
            ECAPointStateHelper.SetState(point, nextState);
            ContainerRuntimeHelper.NotifyPointStateToPlayers(point);
            RefreshDoorInteractHintsForPlayersInRange(point);
        }

        private static bool CanInteractDoor(ECAPointComponent point, Unit player)
        {
            if (point == null || player == null || player.IsDisposed || !point.IsActive)
            {
                return false;
            }

            if (point.PointType != ECAPointType.KeyDoor || point.CurrentState != ECADoorState.Locked)
            {
                return true;
            }

            if (!TryGetDoorKeyRequirement(point, out int itemConfigId, out int needCount))
            {
                return false;
            }

            ItemComponent itemComponent = player.GetComponent<ItemComponent>();
            return itemComponent != null && itemComponent.GetItemCount(itemConfigId) >= needCount;
        }

        private static bool TryConsumeDoorKey(ECAPointComponent point, Unit player)
        {
            if (!TryGetDoorKeyRequirement(point, out int itemConfigId, out int needCount))
            {
                return false;
            }

            ItemComponent itemComponent = player.GetComponent<ItemComponent>();
            if (itemComponent == null)
            {
                return false;
            }

            return ItemHelper.RemoveItem(itemComponent, itemConfigId, needCount, ItemChangeReason.UseItem);
        }

        private static bool TryGetDoorKeyRequirement(ECAPointComponent point, out int itemConfigId, out int needCount)
        {
            itemConfigId = 0;
            needCount = 1;
            if (!FlowParamHelper.TryGetIntParam(point?.Params, ECADoorParamKey.RequiredKeyItemId, out itemConfigId) || itemConfigId <= 0)
            {
                return false;
            }

            if (FlowParamHelper.TryGetIntParam(point?.Params, ECADoorParamKey.ConsumeKeyCount, out int configuredCount) && configuredCount > 0)
            {
                needCount = configuredCount;
            }

            return true;
        }

        private static int ResolveDoorButtonTextId(ECAPointComponent point, bool canInteract)
        {
            if (point == null)
            {
                return 0;
            }

            if (point.CurrentState == ECADoorState.Opened &&
                FlowParamHelper.TryGetIntParam(point.Params, ECADoorParamKey.OpenedButtonTextId, out int openedButtonTextId))
            {
                return openedButtonTextId;
            }

            if (point.CurrentState == ECADoorState.Locked && !canInteract &&
                FlowParamHelper.TryGetIntParam(point.Params, ECADoorParamKey.LockedButtonTextId, out int lockedButtonTextId))
            {
                return lockedButtonTextId;
            }

            if (FlowParamHelper.TryGetIntParam(point.Params, ECADoorParamKey.ClosedButtonTextId, out int closedButtonTextId))
            {
                return closedButtonTextId;
            }

            return 0;
        }
    }
}

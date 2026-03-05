using ET;

namespace ET.Server
{
    [Invoke]
    public class ECAFlowActionInvokeHandler : AInvokeHandler<ECAFlowActionInvoke>
    {
        private const string ParamState = "state";
        private const string ParamActive = "active";
        private const string ParamSeconds = "seconds";
        private const string ParamTimerId = "timer_id";
        private const string ParamLootTable = "loot_table";
        private const string ParamCount = "count";
        private const string ParamRadius = "radius";
        private const string ParamOutputMode = "output_mode";
        private const string ParamButtonTextId = "button_text_id";
        private const string ParamButtonId = "button_id";
        private const string ParamGroupId = "group_id";
        private const string ParamMapName = "map_name";

        public override void Handle(ECAFlowActionInvoke args)
        {
            ECAPointComponent point = args.Point;
            Unit player = args.Player;
            switch (args.Key)
            {
                case ECAFlowActionKey.SetPointActive:
                    if (point == null)
                    {
                        return;
                    }
                    if (!TryGetBoolParam(args.Node, ParamActive, out bool active))
                    {
                        return;
                    }
                    point.IsActive = active;
                    Log.Info($"[ECAFlow] Point {point.PointId} active set to {active}");
                    break;
                case ECAFlowActionKey.SetPointState:
                    if (point == null)
                    {
                        return;
                    }
                    if (!TryGetIntParam(args.Node, ParamState, out int state))
                    {
                        return;
                    }
                    point.CurrentState = state;
                    if (point.PointType == ECAPointType.Container)
                    {
                        ContainerComponent container = ContainerComponentSystem.GetOrAdd(point);
                        if (container != null)
                        {
                            container.State = state;
                        }
                    }
                    Log.Info($"[ECAFlow] Point {point.PointId} state set to {state}");
                    break;
                case ECAFlowActionKey.StartSearchTimer:
                    if (point == null || player == null)
                    {
                        return;
                    }
                    if (!TryGetFloatParam(args.Node, ParamSeconds, out float searchSeconds))
                    {
                        return;
                    }
                    if (!TryGetStringParam(args.Node, ParamTimerId, out string searchTimerId))
                    {
                        return;
                    }
                    long durationMs = (long)(searchSeconds * 1000);
                    if (!ECAFlowTimerHelper.StartTimer(point, player, searchTimerId, durationMs))
                    {
                        return;
                    }

                    ContainerRuntimeHelper.MarkSearchStarted(point, player, searchTimerId, durationMs);
                    break;
                case ECAFlowActionKey.ShowInteractButton:
                    if (point == null || player == null)
                    {
                        return;
                    }

                    int buttonTextId = 0;
                    if (!TryGetIntParam(args.Node, ParamButtonTextId, out buttonTextId))
                    {
                        TryGetIntParam(args.Node, ParamButtonId, out buttonTextId);
                    }
                    ContainerRuntimeHelper.SendInteractHint(point, player, true, buttonTextId);
                    break;
                case ECAFlowActionKey.HideInteractButton:
                    if (point == null || player == null)
                    {
                        return;
                    }

                    ContainerRuntimeHelper.SendInteractHint(point, player, false);
                    if (point.PointType == ECAPointType.Container)
                    {
                        ContainerRuntimeHelper.CancelSearch(point, player, notify: true);
                    }
                    break;
                case ECAFlowActionKey.ShowSearchUI:
                    if (point == null || player == null)
                    {
                        return;
                    }

                    ContainerRuntimeHelper.SendSearchState(point, player, ContainerSearchState.Searching, 0);
                    break;
                case ECAFlowActionKey.OpenContainerUI:
                    if (point == null || player == null)
                    {
                        return;
                    }

                    ContainerRuntimeHelper.OpenContainerUI(point, player);
                    break;
                case ECAFlowActionKey.GenerateContainerLoot:
                    if (point == null)
                    {
                        return;
                    }
                    if (!TryGetStringParam(args.Node, ParamLootTable, out string generateLootTable))
                    {
                        return;
                    }
                    if (!TryGetIntParam(args.Node, ParamCount, out int generateCount))
                    {
                        return;
                    }
                    if (!TryGetFloatParam(args.Node, ParamRadius, out float generateRadius))
                    {
                        return;
                    }

                    string outputMode = null;
                    TryGetStringParam(args.Node, ParamOutputMode, out outputMode);
                    ContainerRuntimeHelper.GenerateLoot(point, player, outputMode, generateLootTable, generateCount, generateRadius);
                    break;
                case ECAFlowActionKey.SpawnItemsToGround:
                    if (!TryGetStringParam(args.Node, ParamLootTable, out string lootTable))
                    {
                        return;
                    }
                    if (!TryGetIntParam(args.Node, ParamCount, out int dropCount))
                    {
                        return;
                    }
                    if (!TryGetFloatParam(args.Node, ParamRadius, out float dropRadius))
                    {
                        return;
                    }
                    if (point == null)
                    {
                        return;
                    }
                    // 兼容旧动作：默认走地面掉落模式。
                    ContainerRuntimeHelper.GenerateLoot(point, player, ContainerOutputMode.GroundDrop.ToString(), lootTable, dropCount, dropRadius);
                    Log.Info($"[ECAFlow] Point {point.PointId} spawn items request (compat): {lootTable}, count={dropCount}, radius={dropRadius}");
                    break;
                case ECAFlowActionKey.SpawnMonsters:
                    if (!TryGetStringParam(args.Node, ParamGroupId, out string groupId))
                    {
                        return;
                    }
                    if (!TryGetIntParam(args.Node, ParamCount, out int spawnCount))
                    {
                        return;
                    }
                    if (point == null)
                    {
                        return;
                    }
                    SpawnPointComponent spawnPoint = SpawnPointComponentSystem.GetOrAdd(point);
                    spawnPoint?.RecordSpawnRequest(groupId, spawnCount);
                    Log.Info($"[ECAFlow] Point {point.PointId} spawn monsters request: {groupId}, count={spawnCount}");
                    break;
                case ECAFlowActionKey.StartEvacCountdown:
                    if (!TryGetFloatParam(args.Node, ParamSeconds, out float evacSeconds))
                    {
                        return;
                    }
                    if (!TryGetStringParam(args.Node, ParamTimerId, out string evacTimerId))
                    {
                        return;
                    }
                    ECAFlowTimerHelper.StartTimer(point, player, evacTimerId, (long)(evacSeconds * 1000));
                    break;
                case ECAFlowActionKey.AllowEvacPlayers:
                    if (point == null)
                    {
                        return;
                    }
                    point.IsActive = true;
                    Log.Info($"[ECAFlow] Point {point.PointId} allow evacuation");
                    break;
                case ECAFlowActionKey.TransferToLobby:
                    if (player == null)
                    {
                        return;
                    }
                    if (!TryGetStringParam(args.Node, ParamMapName, out string mapName))
                    {
                        return;
                    }
                    TransferHelper.TransferAtFrameFinish(player, mapName, 0).Coroutine();
                    break;
            }
        }

        private static bool TryGetIntParam(FlowNodeData node, string key, out int value)
        {
            value = 0;
            if (node == null || node.Params == null || node.Params.Count == 0)
            {
                return false;
            }

            foreach (FlowParam param in node.Params)
            {
                if (param == null || param.Key != key)
                {
                    continue;
                }

                return int.TryParse(param.Value, out value);
            }

            return false;
        }

        private static bool TryGetBoolParam(FlowNodeData node, string key, out bool value)
        {
            value = false;
            if (node == null || node.Params == null || node.Params.Count == 0)
            {
                return false;
            }

            foreach (FlowParam param in node.Params)
            {
                if (param == null || param.Key != key)
                {
                    continue;
                }

                if (int.TryParse(param.Value, out int intValue))
                {
                    value = intValue != 0;
                    return true;
                }

                return bool.TryParse(param.Value, out value);
            }

            return false;
        }

        private static bool TryGetFloatParam(FlowNodeData node, string key, out float value)
        {
            value = 0f;
            if (node == null || node.Params == null || node.Params.Count == 0)
            {
                return false;
            }

            foreach (FlowParam param in node.Params)
            {
                if (param == null || param.Key != key)
                {
                    continue;
                }

                return float.TryParse(param.Value, out value);
            }

            return false;
        }

        private static bool TryGetStringParam(FlowNodeData node, string key, out string value)
        {
            value = null;
            if (node == null || node.Params == null || node.Params.Count == 0)
            {
                return false;
            }

            foreach (FlowParam param in node.Params)
            {
                if (param == null || param.Key != key)
                {
                    continue;
                }

                value = param.Value;
                return !string.IsNullOrWhiteSpace(value);
            }

            return false;
        }
    }
}

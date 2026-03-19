using System.Collections.Generic;
using ET.Server;
using Unity.Mathematics;

namespace ET.Test
{
    /// <summary>
    /// TDD: 验证玩家尸体流图会通过 SearchPanelComponent 打开搜索界面。
    /// </summary>
    public class Statesync_PlayerCorpseLoot_OpenUiKey_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.CreateOneFiber(
                context.Fiber, SceneType.Test, nameof(Statesync_PlayerCorpseLoot_OpenUiKey_Test));

            Fiber testFiber = scope.TestFiber;
            Scene scene = testFiber.Root;
            UnitComponent unitComponent = scene.GetComponent<UnitComponent>() ?? scene.AddComponent<UnitComponent>();
            TimerComponent timerComponent = scene.TimerComponent ?? scene.AddComponent<TimerComponent>();
            ECAManagerComponent ecaManager = scene.GetComponent<ECAManagerComponent>() ?? scene.AddComponent<ECAManagerComponent>();
            if (ecaManager == null)
            {
                Log.Console("eca manager is null");
                return 1;
            }

            Unit target = unitComponent.AddChild<Unit, int>(0);
            target.UnitType = UnitType.Player;
            target.Position = new float3(1f, 0f, 2f);

            ItemComponent itemComponent = target.AddComponent<ItemComponent>();
            itemComponent.SetCapacity(8);

            Item normalItem = itemComponent.AddChild<Item>();
            normalItem.ConfigId = 20001;
            normalItem.Count = 1;
            itemComponent.SetSlotItem(1, normalItem);

            bool created = PlayerCorpseLootHelper.TryCreateCorpse(target);
            if (!created)
            {
                Log.Console("corpse loot should be created");
                return 2;
            }

            ECAPointComponent point = target.GetComponent<ECAPointComponent>();
            if (point == null)
            {
                Log.Console("corpse point is null");
                return 3;
            }

            if (point.FlowGraph?.Nodes == null || point.FlowGraph.Nodes.Count == 0)
            {
                Log.Console("corpse flow graph is empty");
                return 4;
            }

            FlowNodeData openContainerNode = null;
            foreach (FlowNodeData node in point.FlowGraph.Nodes)
            {
                if (node != null && node.NodeKey == ECAFlowActionKey.OpenContainerUI)
                {
                    openContainerNode = node;
                    break;
                }
            }

            if (openContainerNode == null)
            {
                Log.Console("OpenContainerUI node is missing");
                return 5;
            }

            if (openContainerNode.Params == null || openContainerNode.Params.Count == 0)
            {
                Log.Console("OpenContainerUI params are missing");
                return 6;
            }

            Dictionary<string, string> paramMap = new();
            foreach (FlowParam param in openContainerNode.Params)
            {
                if (param == null || string.IsNullOrWhiteSpace(param.Key))
                {
                    continue;
                }

                paramMap[param.Key] = param.Value;
            }

            if (!paramMap.TryGetValue(global::ET.SearchPanelOpenConst.OpenContainerUiKeyParam, out string uiKey))
            {
                Log.Console("OpenContainerUI ui_key is missing");
                return 7;
            }

            if (!string.Equals(uiKey, global::ET.SearchPanelOpenConst.SearchPanelUiKey, System.StringComparison.Ordinal))
            {
                Log.Console($"unexpected ui_key: {uiKey}");
                return 8;
            }

            return ErrorCode.ERR_Success;
        }
    }
}

using System;
using System.Collections.Generic;
using ET.Server;
using Unity.Mathematics;

namespace ET.Test
{
    /// <summary>
    /// TDD: 验证怪物死亡尸体盒会生成共享容器，且空盒仍可打开。
    /// </summary>
    public class Statesync_MonsterCorpseLoot_CreateBox_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Statesync_MonsterCorpseLoot_CreateBox_Test));
            Scene scene = scope.TestFiber.Root;

            UnitComponent unitComponent = scene.GetComponent<UnitComponent>();
            if (unitComponent == null)
            {
                unitComponent = scene.AddComponent<UnitComponent>();
            }

            if (scene.TimerComponent == null)
            {
                scene.AddComponent<TimerComponent>();
            }

            ECAManagerComponent ecaManager = scene.GetComponent<ECAManagerComponent>();
            if (ecaManager == null)
            {
                ecaManager = scene.AddComponent<ECAManagerComponent>();
            }

            if (ecaManager == null)
            {
                Log.Console("eca manager is null");
                return 1;
            }

            Unit emptyMonster = TestHelper.CreateServerUnit(scene, UnitType.Monster, addBuffComponent: false, campId: 2);
            if (emptyMonster == null)
            {
                Log.Console("empty monster is null");
                return 2;
            }

            emptyMonster.Position = new float3(6f, 0f, 8f);
            emptyMonster.Rotation = quaternion.identity;

            if (!MonsterCorpseLootHelper.TryCreateCorpse(
                    emptyMonster,
                    ExtractionInventoryConfig.GetDefaultMonsterCorpseLootBoxUnitConfigId(),
                    0,
                    string.Empty,
                    0,
                    0,
                    false,
                    out string emptyPointId,
                    out long emptyPointUnitId))
            {
                Log.Console("empty corpse loot box should be created");
                return 3;
            }

            if (!emptyPointId.StartsWith(global::ET.SearchPanelOpenConst.MonsterCorpsePointPrefix, StringComparison.Ordinal))
            {
                Log.Console($"unexpected empty corpse point id: {emptyPointId}");
                return 4;
            }

            Unit emptyPointUnit = unitComponent.Get(emptyPointUnitId);
            if (emptyPointUnit == null || emptyPointUnit.IsDisposed)
            {
                Log.Console("empty corpse point unit is null");
                return 5;
            }

            ECAPointComponent emptyPoint = emptyPointUnit.GetComponent<ECAPointComponent>();
            ContainerComponent emptyContainer = emptyPointUnit.GetComponent<ContainerComponent>();
            if (emptyPoint == null || emptyContainer == null)
            {
                Log.Console("empty corpse point or container missing");
                return 6;
            }

            NumericComponent emptyNumeric = emptyPointUnit.NumericComponent;
            if (emptyNumeric == null ||
                emptyNumeric.GetAsLong(NumericType.HP) != 0 ||
                emptyNumeric.GetAsLong(NumericType.MaxHP) != 0)
            {
                Log.Console($"empty corpse combat numeric should be cleared: hp={emptyNumeric?.GetAsLong(NumericType.HP) ?? -1}, maxHp={emptyNumeric?.GetAsLong(NumericType.MaxHP) ?? -1}");
                return 7;
            }

            if (emptyContainer.State != ContainerState.Empty || emptyContainer.HasAnyItem())
            {
                Log.Console($"empty corpse container should remain empty: state={emptyContainer.State}, items={emptyContainer.ItemEntries.Count}");
                return 8;
            }

            if (!ContainerRuntimeHelper.TryGetPoint(scene, emptyPointId, out ECAPointComponent registeredEmptyPoint) || registeredEmptyPoint != emptyPoint)
            {
                Log.Console("empty corpse point was not registered in eca manager");
                return 9;
            }

            FlowNodeData openContainerNode = null;
            foreach (FlowNodeData node in emptyPoint.FlowGraph?.Nodes ?? new List<FlowNodeData>())
            {
                if (node != null && node.NodeKey == ECAFlowActionKey.OpenContainerUI)
                {
                    openContainerNode = node;
                    break;
                }
            }

            if (openContainerNode == null)
            {
                Log.Console("empty corpse open container node is missing");
                return 10;
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

            if (!paramMap.TryGetValue(global::ET.SearchPanelOpenConst.OpenContainerUiKeyParam, out string emptyUiKey) ||
                !string.Equals(emptyUiKey, global::ET.SearchPanelOpenConst.SearchPanelUiKey, StringComparison.Ordinal))
            {
                Log.Console($"empty corpse ui_key mismatch: {emptyUiKey ?? "null"}");
                return 11;
            }

            Unit lootMonster = TestHelper.CreateServerUnit(scene, UnitType.Monster, addBuffComponent: false, campId: 2);
            if (lootMonster == null)
            {
                Log.Console("loot monster is null");
                return 11;
            }

            lootMonster.Position = new float3(9f, 0f, 12f);
            lootMonster.Rotation = quaternion.identity;

            if (!MonsterCorpseLootHelper.TryCreateCorpse(
                    lootMonster,
                    ExtractionInventoryConfig.GetDefaultMonsterCorpseLootBoxUnitConfigId(),
                    0,
                    "22001*1|20001*1",
                    1,
                    1,
                    false,
                    out string lootPointId,
                    out long lootPointUnitId))
            {
                Log.Console("loot corpse loot box should be created");
                return 12;
            }

            if (!lootPointId.StartsWith(global::ET.SearchPanelOpenConst.MonsterCorpsePointPrefix, StringComparison.Ordinal))
            {
                Log.Console($"unexpected loot corpse point id: {lootPointId}");
                return 13;
            }

            Unit lootPointUnit = unitComponent.Get(lootPointUnitId);
            ContainerComponent lootContainer = lootPointUnit?.GetComponent<ContainerComponent>();
            if (lootPointUnit == null || lootContainer == null)
            {
                Log.Console("loot corpse point unit or container missing");
                return 15;
            }

            NumericComponent lootNumeric = lootPointUnit.NumericComponent;
            if (lootNumeric == null ||
                lootNumeric.GetAsLong(NumericType.HP) != 0 ||
                lootNumeric.GetAsLong(NumericType.MaxHP) != 0)
            {
                Log.Console($"loot corpse combat numeric should be cleared: hp={lootNumeric?.GetAsLong(NumericType.HP) ?? -1}, maxHp={lootNumeric?.GetAsLong(NumericType.MaxHP) ?? -1}");
                return 16;
            }

            if (lootContainer.State != ContainerState.Closed)
            {
                Log.Console($"loot corpse container state mismatch: {lootContainer.State}");
                return 17;
            }

            if (lootContainer.ItemEntries.Count != 1)
            {
                Log.Console($"loot corpse item count mismatch: {lootContainer.ItemEntries.Count}");
                return 18;
            }

            foreach (ContainerItemEntry entry in lootContainer.ItemEntries.Values)
            {
                if ((entry.ConfigId != 22001 && entry.ConfigId != 20001) || entry.Count != 1)
                {
                    Log.Console($"loot corpse entry mismatch: config={entry.ConfigId}, count={entry.Count}");
                    return 19;
                }
            }

            if (!ContainerRuntimeHelper.TryGetPoint(scene, lootPointId, out ECAPointComponent registeredLootPoint) ||
                registeredLootPoint == null ||
                registeredLootPoint.IsDisposed)
            {
                Log.Console("loot corpse point was not registered in eca manager");
                return 20;
            }

            return ErrorCode.ERR_Success;
        }
    }
}

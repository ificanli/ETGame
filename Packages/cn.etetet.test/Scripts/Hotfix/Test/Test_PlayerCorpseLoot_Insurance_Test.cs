using System;
using System.Collections.Generic;
using ET.Server;
using Unity.Mathematics;

namespace ET.Test
{
    public class Test_PlayerCorpseLoot_Insurance_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_PlayerCorpseLoot_Insurance_Test));
            Fiber testFiber = scope.TestFiber;
            Scene scene = testFiber.Root;

            int safeSlotStart = ExtractionInventoryConfig.GetSafeSlotStart();
            int safeSlotCount = ExtractionInventoryConfig.GetSafeSlotCount();
            if (safeSlotCount <= 0)
            {
                Log.Console("safe slot count must be greater than zero");
                return 1;
            }

            UnitComponent unitComponent = scene.AddComponent<UnitComponent>();
            scene.AddComponent<TimerComponent>();
            ECAManagerComponent ecaManager = scene.AddComponent<ECAManagerComponent>();
            if (ecaManager == null)
            {
                Log.Console("eca manager is null");
                return 2;
            }

            Unit target = unitComponent.AddChild<Unit, int>(0);
            target.UnitType = UnitType.Player;
            target.Position = new float3(12f, 0f, 8f);

            ItemComponent itemComponent = target.AddComponent<ItemComponent>();
            itemComponent.SetCapacity(12);

            Item safeItem = itemComponent.AddChild<Item>();
            safeItem.ConfigId = 10001;
            safeItem.Count = 2;
            itemComponent.SetSlotItem(safeSlotStart, safeItem);

            int normalSlot1 = safeSlotStart + safeSlotCount;
            if (ExtractionInventoryConfig.IsSafeSlot(normalSlot1))
            {
                ++normalSlot1;
            }

            int normalSlot2 = normalSlot1 + 1;
            if (normalSlot2 >= itemComponent.Capacity)
            {
                Log.Console("not enough normal bag slots for corpse loot test");
                return 3;
            }

            Item normalItem1 = itemComponent.AddChild<Item>();
            normalItem1.ConfigId = 20001;
            normalItem1.Count = 3;
            itemComponent.SetSlotItem(normalSlot1, normalItem1);

            Item normalItem2 = itemComponent.AddChild<Item>();
            normalItem2.ConfigId = 20002;
            normalItem2.Count = 5;
            itemComponent.SetSlotItem(normalSlot2, normalItem2);

            bool created = PlayerCorpseLootHelper.TryCreateCorpse(target);
            if (!created)
            {
                Log.Console("corpse loot should be created");
                return 4;
            }

            if (target.GetComponent<PlayerCorpseLootComponent>() == null)
            {
                Log.Console("corpse loot component missing");
                return 5;
            }

            ECAPointComponent point = target.GetComponent<ECAPointComponent>();
            if (point == null)
            {
                Log.Console("eca point missing on corpse");
                return 6;
            }

            if (!string.Equals(point.PointId, $"corpse_{target.Id}", StringComparison.Ordinal))
            {
                Log.Console($"unexpected corpse point id: {point.PointId}");
                return 7;
            }

            if (Math.Abs(point.InteractRange - ExtractionInventoryConfig.GetCorpseInteractRange()) > 0.001f)
            {
                Log.Console($"corpse interact range mismatch: {point.InteractRange}");
                return 8;
            }

            if (point.FlowGraph == null || point.FlowGraph.Nodes == null || point.FlowGraph.Nodes.Count != 6)
            {
                Log.Console("corpse flow graph is invalid");
                return 9;
            }

            if (ecaManager.GetECAPoint(point.PointId) != point)
            {
                Log.Console("eca manager did not register corpse point");
                return 10;
            }

            if (itemComponent.GetItemBySlot(safeSlotStart)?.ConfigId != safeItem.ConfigId)
            {
                Log.Console("safe slot item should remain in player bag");
                return 11;
            }

            if (itemComponent.GetItemBySlot(normalSlot1) != null)
            {
                Log.Console("normal slot 1 should be emptied after corpse creation");
                return 12;
            }

            if (itemComponent.GetItemBySlot(normalSlot2) != null)
            {
                Log.Console("normal slot 2 should be emptied after corpse creation");
                return 13;
            }

            ContainerComponent container = target.GetComponent<ContainerComponent>();
            if (container == null)
            {
                Log.Console("container component missing on corpse");
                return 14;
            }

            if (container.State != ContainerState.Closed)
            {
                Log.Console($"corpse container state mismatch: {container.State}");
                return 15;
            }

            if (container.ItemEntries.Count != 2)
            {
                Log.Console($"corpse container item count mismatch: {container.ItemEntries.Count}");
                return 16;
            }

            HashSet<int> containerConfigIds = new();
            foreach (ContainerItemEntry entry in container.ItemEntries.Values)
            {
                containerConfigIds.Add(entry.ConfigId);
            }

            if (!containerConfigIds.Contains(normalItem1.ConfigId) || !containerConfigIds.Contains(normalItem2.ConfigId))
            {
                Log.Console("corpse container missing normal bag items");
                return 17;
            }

            if (containerConfigIds.Contains(safeItem.ConfigId))
            {
                Log.Console("corpse container should not contain safe slot item");
                return 18;
            }

            scene.RemoveComponent<ECAManagerComponent>();
            return ErrorCode.ERR_Success;
        }
    }
}

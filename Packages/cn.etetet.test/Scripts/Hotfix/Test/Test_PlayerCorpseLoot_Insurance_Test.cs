using System;
using System.Collections.Generic;
using System.Reflection;
using ET.Server;
using Luban;
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

            UnitComponent unitComponent = scene.AddComponent<UnitComponent>();
            scene.AddComponent<TimerComponent>();
            ECAManagerComponent ecaManager = scene.AddComponent<ECAManagerComponent>();
            if (ecaManager == null)
            {
                Log.Console("eca manager is null");
                return 1;
            }

            Unit target = unitComponent.AddChild<Unit, int>(0);
            target.UnitType = UnitType.Player;
            target.Position = new float3(12f, 0f, 8f);

            ItemComponent itemComponent = target.AddComponent<ItemComponent>();
            itemComponent.SetCapacity(12);

            RuntimeSecureInventoryHelper.Apply(
                target,
                2,
                2,
                new List<LoadoutGridItemInfo>
                {
                    new LoadoutGridItemInfo
                    {
                        ConfigId = 10001,
                        Count = 2,
                        AnchorSlotIndex = 0,
                        GridWidth = 1,
                        GridHeight = 1,
                    },
                });

            int normalSlot1 = 0;
            int normalSlot2 = 1;
            if (normalSlot2 >= itemComponent.Capacity)
            {
                Log.Console("not enough normal bag slots for corpse loot test");
                return 2;
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
                return 3;
            }

            if (target.GetComponent<PlayerCorpseLootComponent>() == null)
            {
                Log.Console("corpse loot component missing");
                return 4;
            }

            ECAPointComponent point = target.GetComponent<ECAPointComponent>();
            if (point == null)
            {
                Log.Console("eca point missing on corpse");
                return 5;
            }

            if (!string.Equals(point.PointId, $"corpse_{target.Id}", StringComparison.Ordinal))
            {
                Log.Console($"unexpected corpse point id: {point.PointId}");
                return 6;
            }

            if (Math.Abs(point.InteractRange - ExtractionInventoryConfig.GetCorpseInteractRange()) > 0.001f)
            {
                Log.Console($"corpse interact range mismatch: {point.InteractRange}");
                return 7;
            }

            if (point.FlowGraph == null || point.FlowGraph.Nodes == null || point.FlowGraph.Nodes.Count != 6)
            {
                Log.Console("corpse flow graph is invalid");
                return 8;
            }

            if (ecaManager.GetECAPoint(point.PointId) != point)
            {
                Log.Console("eca manager did not register corpse point");
                return 9;
            }

            RuntimeSecureInventoryComponent secureInventory = target.GetComponent<RuntimeSecureInventoryComponent>();
            if (secureInventory == null || secureInventory.Items.Count != 1 || secureInventory.Items[0].ConfigId != 10001 || secureInventory.Items[0].Count != 2)
            {
                Log.Console("runtime secure item should remain on player after corpse creation");
                return 10;
            }

            if (itemComponent.GetItemBySlot(normalSlot1) != null)
            {
                Log.Console("normal slot 1 should be emptied after corpse creation");
                return 11;
            }

            if (itemComponent.GetItemBySlot(normalSlot2) != null)
            {
                Log.Console("normal slot 2 should be emptied after corpse creation");
                return 12;
            }

            ContainerComponent container = target.GetComponent<ContainerComponent>();
            if (container == null)
            {
                Log.Console("container component missing on corpse");
                return 13;
            }

            if (container.State != ContainerState.Closed)
            {
                Log.Console($"corpse container state mismatch: {container.State}");
                return 14;
            }

            if (container.ItemEntries.Count != 2)
            {
                Log.Console($"corpse container item count mismatch: {container.ItemEntries.Count}");
                return 15;
            }

            HashSet<int> containerConfigIds = new();
            foreach (ContainerItemEntry entry in container.ItemEntries.Values)
            {
                containerConfigIds.Add(entry.ConfigId);
            }

            if (!containerConfigIds.Contains(normalItem1.ConfigId) || !containerConfigIds.Contains(normalItem2.ConfigId))
            {
                Log.Console("corpse container missing normal bag items");
                return 16;
            }

            if (containerConfigIds.Contains(10001))
            {
                Log.Console("corpse container should not contain runtime secure item");
                return 17;
            }

            scene.RemoveComponent<ECAManagerComponent>();
            return ErrorCode.ERR_Success;
        }
    }

    public class Test_Item_DiscardGroundDrop_Test : ATestHandler
    {
        private const int DiscardTestItemConfigId = 10001;

        public override async ETTask<int> Handle(TestContext context)
        {
            ItemConfigCategory previousItemConfigCategory = InstallDiscardTestItemConfig();
            try
            {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Item_DiscardGroundDrop_Test));
            Scene scene = scope.TestFiber.Root;

            UnitComponent unitComponent = scene.GetComponent<UnitComponent>() ?? scene.AddComponent<UnitComponent>();
            scene.AddComponent<TimerComponent>();
            ECAManagerComponent ecaManager = scene.GetComponent<ECAManagerComponent>() ?? scene.AddComponent<ECAManagerComponent>();
            scene.AddComponent<CoroutineLockComponent>();

            ItemConfig itemConfig = ItemConfigCategory.Instance.GetOrDefault(DiscardTestItemConfigId);
            if (itemConfig == null)
            {
                Log.Console($"discard test item config is null: {DiscardTestItemConfigId}");
                return 1;
            }

            Unit player = TestHelper.CreateServerUnit(scene, UnitType.Player, addItemComponent: true, campId: 1);
            if (player == null)
            {
                Log.Console("player is null");
                return 2;
            }

            player.Position = new float3(15f, 0f, 9f);
            ItemComponent itemComponent = player.GetComponent<ItemComponent>();
            if (itemComponent == null)
            {
                Log.Console("item component is null");
                return 3;
            }

            itemComponent.SetCapacity(8);
            Item bagItem = itemComponent.AddChild<Item>();
            bagItem.ConfigId = itemConfig.Id;
            bagItem.Count = 5;
            bagItem.GridWidth = itemConfig.GridWidth > 0 ? itemConfig.GridWidth : 1;
            bagItem.GridHeight = itemConfig.GridHeight > 0 ? itemConfig.GridHeight : 1;
            itemComponent.SetSlotItem(0, bagItem);

            EntityRef<Unit> playerRef = player;
            EntityRef<ItemComponent> itemComponentRef = itemComponent;
            EntityRef<ECAManagerComponent> ecaManagerRef = ecaManager;
            int error = await ItemHelper.TryDiscardItemToGroundAsync(player, bagItem.Id, 2);
            if (error != ErrorCode.ERR_Success)
            {
                Log.Console($"discard to ground failed: {error}");
                return 4;
            }

            player = playerRef;
            itemComponent = itemComponentRef;
            ecaManager = ecaManagerRef;
            if (player == null || itemComponent == null || ecaManager == null)
            {
                Log.Console("entity ref lost after discard await");
                return 5;
            }

            Item remainItem = itemComponent.GetItemBySlot(0);
            if (remainItem == null || remainItem.Count != 3)
            {
                Log.Console($"bag item count mismatch after discard: {remainItem?.Count ?? 0}");
                return 6;
            }

            List<ECAPointComponent> points = ecaManager.GetAllECAPoints();
            if (points.Count != 1)
            {
                Log.Console($"ground drop point count mismatch: {points.Count}");
                return 7;
            }

            ECAPointComponent point = points[0];
            if (point == null || point.IsDisposed)
            {
                Log.Console("ground drop point is null");
                return 8;
            }

            Unit pointUnit = point.GetParent<Unit>();
            if (pointUnit == null || pointUnit.IsDisposed || pointUnit.Id == player.Id)
            {
                Log.Console("ground drop point unit is invalid");
                return 9;
            }

            ContainerComponent container = pointUnit.GetComponent<ContainerComponent>();
            if (container == null)
            {
                Log.Console("ground drop container is null");
                return 10;
            }

            if (container.OutputMode != ContainerOutputMode.GroundDrop)
            {
                Log.Console($"ground drop output mode mismatch: {container.OutputMode}");
                return 11;
            }

            if (container.CreatorPlayerId != player.Id)
            {
                Log.Console($"ground drop creator mismatch: {container.CreatorPlayerId}");
                return 12;
            }

            if (container.CreateTime <= 0)
            {
                Log.Console($"ground drop create time invalid: {container.CreateTime}");
                return 13;
            }

            if (!container.TryGetItem(0, out ContainerItemEntry entry))
            {
                Log.Console("ground drop container slot 0 missing");
                return 14;
            }

            if (entry.ConfigId != itemConfig.Id || entry.Count != 2)
            {
                Log.Console($"ground drop container entry mismatch: config={entry.ConfigId}, count={entry.Count}");
                return 15;
            }

            if (point.FlowGraph == null || point.FlowGraph.Nodes == null || point.FlowGraph.Nodes.Count == 0)
            {
                Log.Console("ground drop flow graph is invalid");
                return 16;
            }

            FlowNodeData openNode = null;
            foreach (FlowNodeData node in point.FlowGraph.Nodes)
            {
                if (node != null && node.NodeKey == ECAFlowActionKey.OpenContainerUI)
                {
                    openNode = node;
                    break;
                }
            }

            if (openNode == null)
            {
                Log.Console("ground drop open container node missing");
                return 17;
            }

            Dictionary<string, string> paramMap = new();
            foreach (FlowParam param in openNode.Params)
            {
                if (param == null || string.IsNullOrWhiteSpace(param.Key))
                {
                    continue;
                }

                paramMap[param.Key] = param.Value;
            }

            if (!paramMap.TryGetValue("ui_key", out string uiKey) || !string.Equals(uiKey, "SearchPanelComponent", StringComparison.Ordinal))
            {
                Log.Console($"ground drop ui_key mismatch: {uiKey ?? "null"}");
                return 18;
            }

            ContainerRuntimeHelper.OpenContainerUI(point, player, uiKey);
            EntityRef<ECAPointComponent> pointRef = point;
            error = await ContainerRuntimeHelper.TakeItem(player, point, 0);
            if (error != ErrorCode.ERR_Success)
            {
                Log.Console($"take ground drop failed: {error}");
                return 19;
            }

            player = playerRef;
            itemComponent = itemComponentRef;
            ecaManager = ecaManagerRef;
            point = pointRef;
            if (player == null || itemComponent == null || ecaManager == null)
            {
                Log.Console("entity ref lost after take await");
                return 20;
            }

            remainItem = itemComponent.GetItemBySlot(0);
            if (remainItem == null || remainItem.Count != 5)
            {
                Log.Console($"bag item count mismatch after take: {remainItem?.Count ?? 0}");
                return 21;
            }

            if (point != null && !point.IsDisposed)
            {
                Log.Console("ground drop point should be disposed after take");
                return 22;
            }

            points = ecaManager.GetAllECAPoints();
            if (points.Count != 0)
            {
                Log.Console($"ground drop points should be cleaned up after take: {points.Count}");
                return 23;
            }

            return ErrorCode.ERR_Success;
            }
            finally
            {
                RestoreItemConfigCategory(previousItemConfigCategory);
            }
        }

        private static ItemConfigCategory InstallDiscardTestItemConfig()
        {
            ItemConfigCategory current = ItemConfigCategory.Instance;
            if (current != null && current.GetOrDefault(DiscardTestItemConfigId) != null)
            {
                return current;
            }

            ByteBuf buffer = new ByteBuf();
            buffer.WriteSize(1);
            buffer.WriteInt(DiscardTestItemConfigId);
            buffer.WriteString("DiscardTestItem");
            buffer.WriteString("DiscardTestItemDesc");
            buffer.WriteInt(1);
            buffer.WriteInt(99);
            buffer.WriteString("discard_test_icon");
            buffer.WriteInt(1);
            buffer.WriteInt(0);
            buffer.WriteInt(1);
            buffer.WriteInt(1);
            buffer.WriteInt(1);
            buffer.WriteBool(false);
            buffer.WriteInt(0);
            buffer.WriteInt(0);
            buffer.WriteBool(false);
            buffer.WriteBool(false);
            buffer.WriteBool(false);
            buffer.WriteBool(false);
            buffer.WriteBool(false);
            buffer.WriteInt(0);
            buffer.WriteInt(0);
            buffer.ReaderIndex = 0;

            ItemConfigCategory discardCategory = new ItemConfigCategory(buffer);
            SetItemConfigCategory(discardCategory);
            return current;
        }

        private static void RestoreItemConfigCategory(ItemConfigCategory previous)
        {
            SetItemConfigCategory(previous);
        }

        private static void SetItemConfigCategory(ItemConfigCategory category)
        {
            FieldInfo instanceField = typeof(Singleton<ItemConfigCategory>).GetField("instance", BindingFlags.NonPublic | BindingFlags.Static);
            instanceField?.SetValue(null, category);
        }
    }
}

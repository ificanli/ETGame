using System.Collections.Generic;
using ET.Server;

namespace ET.Test
{
    public class Test_Rogue_BuffTreeNodeId_Assigned_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Rogue_BuffTreeNodeId_Assigned_Test));

            RogueBuffConfigLoader.EnsureRegistered();

            int[] buffConfigIds = { 1022, 1036, 1064 };
            foreach (int buffConfigId in buffConfigIds)
            {
                BuffConfig buffConfig = BuffConfigCategory.Instance.Get(buffConfigId);
                if (buffConfig == null)
                {
                    Log.Console($"rogue buff config is null, buffConfigId={buffConfigId}");
                    return 1;
                }

                foreach (EffectNode effectNode in buffConfig.Effects)
                {
                    if (effectNode == null)
                    {
                        Log.Console($"rogue buff effect is null, buffConfigId={buffConfigId}");
                        return 2;
                    }

                    if (!ValidateTree(effectNode))
                    {
                        Log.Console($"rogue buff tree contains invalid node id, buffConfigId={buffConfigId}, effectType={effectNode.GetType().Name}");
                        return 3;
                    }
                }
            }

            const int effectGroupId = 990001;
            RogueEffectGroupConfig groupConfig = new RogueEffectGroupConfig
            {
                Id = effectGroupId,
                Entries =
                {
                    new RogueEffectEntryConfig
                    {
                        ExecuteType = RogueEffectExecuteType.AddGold,
                        Value1 = 100,
                    },
                    new RogueEffectEntryConfig
                    {
                        ExecuteType = RogueEffectExecuteType.DamageReduction,
                        Value1 = 300,
                        Value2 = 1,
                    },
                    new RogueEffectEntryConfig
                    {
                        ExecuteType = RogueEffectExecuteType.PeriodicHpScale,
                        Value1 = 1000,
                        Value2 = 30,
                    },
                },
            };

            if (!RogueEffectGroupSyntheticBuffHelper.TryEnsureSyntheticBuffConfig(effectGroupId, groupConfig, out int syntheticBuffConfigId))
            {
                Log.Console($"failed to create synthetic rogue buff, effectGroupId={effectGroupId}");
                return 4;
            }

            BuffConfig syntheticBuffConfig = BuffConfigCategory.Instance.Get(syntheticBuffConfigId);
            if (syntheticBuffConfig == null)
            {
                Log.Console($"synthetic rogue buff config is null, buffConfigId={syntheticBuffConfigId}");
                return 5;
            }

            foreach (EffectNode effectNode in syntheticBuffConfig.Effects)
            {
                if (effectNode == null)
                {
                    Log.Console($"synthetic rogue buff effect is null, buffConfigId={syntheticBuffConfigId}");
                    return 6;
                }

                if (!ValidateTree(effectNode))
                {
                    Log.Console($"synthetic rogue buff tree contains invalid node id, buffConfigId={syntheticBuffConfigId}, effectType={effectNode.GetType().Name}");
                    return 7;
                }
            }

            return ErrorCode.ERR_Success;
        }

        private static bool ValidateTree(BTNode root)
        {
            HashSet<int> usedIds = new();
            return ValidateNode(root, usedIds);
        }

        private static bool ValidateNode(BTNode node, HashSet<int> usedIds)
        {
            if (node == null)
            {
                return false;
            }

            if (node.Id <= 0 || !usedIds.Add(node.Id))
            {
                return false;
            }

            if (node.Children == null)
            {
                return true;
            }

            foreach (BTNode child in node.Children)
            {
                if (!ValidateNode(child, usedIds))
                {
                    return false;
                }
            }

            return true;
        }
    }
}

using System.Collections.Generic;
using ET.Server;

namespace ET.Test
{
    public class Test_Rogue_EcaInteractGoldReward_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Rogue_EcaInteractGoldReward_Test));
            Scene scene = scope.TestFiber.Root;

            UnitComponent unitComponent = scene.GetComponent<UnitComponent>() ?? scene.AddComponent<UnitComponent>();

            UnitConfig playerConfig = null;
            foreach (UnitConfig config in UnitConfigCategory.Instance.DataList)
            {
                if (config.UnitType == UnitType.Player)
                {
                    playerConfig = config;
                    break;
                }
            }

            if (playerConfig == null)
            {
                Log.Console("player unit config is null");
                return 1;
            }

            Unit player = unitComponent.AddChildWithId<Unit, int>(IdGenerater.Instance.GenerateId(), playerConfig.Id);
            player.UnitType = UnitType.Player;

            NumericComponent numericComponent = player.AddComponent<NumericComponent>();
            foreach ((int numericType, long numericValue) in playerConfig.KV)
            {
                numericComponent.SetNoEvent(numericType, numericValue);
            }

            RogueProgressComponent progress = RogueProgressHelper.EnsureProgress(player, false);
            if (progress == null)
            {
                Log.Console("rogue progress is null");
                return 2;
            }

            Unit pointUnit = unitComponent.AddChildWithId<Unit, int>(IdGenerater.Instance.GenerateId(), 0);
            ECAPointComponent point = pointUnit.AddComponent<ECAPointComponent, string, int, float>("rogue_merchant_001", ECAPointType.RangeTrigger, 3f);
            point.Params = new List<FlowParam>
            {
                new() { Key = RogueECAPointParamKey.InteractGold, Value = "30000" },
                new() { Key = RogueECAPointParamKey.RewardOnce, Value = "1" },
            };

            int firstReward = RoguePointRewardHelper.TryGrantInteractGold(point, player);
            if (firstReward != 30000 || progress.CurrentGold != 30000)
            {
                Log.Console($"first interact gold mismatch, reward={firstReward}, gold={progress.CurrentGold}");
                return 3;
            }

            if (progress.ClaimedPointRewardIds.Count != 1 || progress.ClaimedPointRewardIds[0] != "rogue_merchant_001")
            {
                Log.Console("claimed point reward ids mismatch after first interact");
                return 4;
            }

            int secondReward = RoguePointRewardHelper.TryGrantInteractGold(point, player);
            if (secondReward != 0 || progress.CurrentGold != 30000)
            {
                Log.Console($"second interact should not grant gold, reward={secondReward}, gold={progress.CurrentGold}");
                return 5;
            }

            return ErrorCode.ERR_Success;
        }
    }
}

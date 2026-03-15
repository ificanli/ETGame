using ET.Client;

namespace ET.Test
{
    /// <summary>
    /// 测试悬赏合同功能
    /// </summary>
    public class Test_Home_Contract_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Home_Contract_Test));
            Fiber testFiber = scope.TestFiber;

            Fiber robot = await TestHelper.CreateRobot(testFiber, nameof(Test_Home_Contract_Test));
            Unit serverUnit = TestHelper.GetServerUnit(testFiber, robot);
            if (serverUnit == null)
            {
                Log.Console("server unit is null");
                return 1;
            }

            // 确保有 HomeContractComponent
            Server.HomeContractComponent contractComp = serverUnit.GetComponent<Server.HomeContractComponent>();
            if (contractComp == null)
            {
                contractComp = serverUnit.AddComponent<Server.HomeContractComponent>();
            }

            // 刷新合同列表
            Server.HomeContractHelper.RefreshContracts(serverUnit);

            // 验证有可用合同
            if (contractComp.AvailableContracts.Count == 0)
            {
                Log.Console("no available contracts after refresh");
                return 2;
            }

            // 接取第一个合同
            long contractId = contractComp.AvailableContracts[0].ContractId;
            int acceptResult = Server.HomeContractHelper.AcceptContract(serverUnit, contractId);
            if (acceptResult != ErrorCode.ERR_Success)
            {
                Log.Console($"accept contract failed, error={acceptResult}");
                return 3;
            }

            // 验证合同移入ActiveContracts
            if (contractComp.ActiveContracts.Count != 1)
            {
                Log.Console($"expected 1 active contract, got {contractComp.ActiveContracts.Count}");
                return 4;
            }

            // 验证合同状态为Active
            if (contractComp.ActiveContracts[0].State != HomeContractState.Active)
            {
                Log.Console($"expected Active state, got {contractComp.ActiveContracts[0].State}");
                return 5;
            }

            // 验证AvailableContracts中该合同已移除
            bool foundInAvailable = false;
            foreach (var c in contractComp.AvailableContracts)
            {
                if (c.ContractId == contractId)
                {
                    foundInAvailable = true;
                    break;
                }
            }

            if (foundInAvailable)
            {
                Log.Console("accepted contract still in AvailableContracts");
                return 6;
            }

            // 模拟合同完成
            contractComp.ActiveContracts[0].Progress = contractComp.ActiveContracts[0].Target;
            contractComp.ActiveContracts[0].State = HomeContractState.Completed;

            // 领取奖励
            var rewardResult = Server.HomeContractHelper.CollectContractReward(serverUnit, contractId);
            if (rewardResult.ErrorCode != ErrorCode.ERR_Success)
            {
                Log.Console($"collect reward failed, error={rewardResult.ErrorCode}");
                return 7;
            }

            // 验证获得奖励物品
            if (rewardResult.ItemConfigIds == null || rewardResult.ItemConfigIds.Count == 0)
            {
                Log.Console("no reward items");
                return 8;
            }

            // 验证合同状态已变为Claimed
            if (contractComp.ActiveContracts[0].State != HomeContractState.Claimed)
            {
                Log.Console($"expected Claimed state, got {contractComp.ActiveContracts[0].State}");
                return 9;
            }

            // 验证接取不存在的合同返回错误
            int notFoundResult = Server.HomeContractHelper.AcceptContract(serverUnit, 999999);
            if (notFoundResult != ErrorCode.ERR_HomeContractNotFound)
            {
                Log.Console($"expected ERR_HomeContractNotFound, got {notFoundResult}");
                return 10;
            }

            return ErrorCode.ERR_Success;
        }
    }
}

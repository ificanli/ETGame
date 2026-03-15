using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 悬赏合同逻辑
    /// </summary>
    public static class HomeContractHelper
    {
        // 默认生成合同数量
        public const int DefaultContractCount = 3;
        // 默认合同有效期 24小时(ms)
        public const long DefaultContractDuration = 24 * 3600 * 1000;
        // 默认奖励
        public const int DefaultRewardItemId = 1003;
        public const int DefaultRewardCount = 5;
        // 默认合同目标
        public const int DefaultTarget = 10;

        /// <summary>
        /// 刷新可接取合同列表
        /// </summary>
        public static void RefreshContracts(Unit unit)
        {
            HomeContractComponent contractComp = unit.GetComponent<HomeContractComponent>();
            if (contractComp == null)
            {
                return;
            }

            long now = TimeInfo.Instance.ServerNow();
            contractComp.AvailableContracts.Clear();

            for (int i = 0; i < DefaultContractCount; i++)
            {
                var contract = new HomeContractData
                {
                    ContractId = IdGenerater.Instance.GenerateId(),
                    ConfigId = i + 1,
                    State = HomeContractState.Available,
                    AcceptTime = 0,
                    ExpireTime = now + DefaultContractDuration,
                    Progress = 0,
                    Target = DefaultTarget
                };
                contractComp.AvailableContracts.Add(contract);
            }

            contractComp.LastRefreshTime = now;
            Log.Debug($"HomeContractHelper: refreshed {DefaultContractCount} contracts for unit {unit.Id}");
        }

        /// <summary>
        /// 接取合同
        /// </summary>
        public static int AcceptContract(Unit unit, long contractId)
        {
            HomeContractComponent contractComp = unit.GetComponent<HomeContractComponent>();
            if (contractComp == null)
            {
                return ErrorCode.ERR_HomeNotInHomeScene;
            }

            // 从 AvailableContracts 找到合同
            HomeContractData contract = null;
            int contractIndex = -1;
            for (int i = 0; i < contractComp.AvailableContracts.Count; i++)
            {
                if (contractComp.AvailableContracts[i].ContractId == contractId)
                {
                    contract = contractComp.AvailableContracts[i];
                    contractIndex = i;
                    break;
                }
            }

            if (contract == null)
            {
                return ErrorCode.ERR_HomeContractNotFound;
            }

            // 检查是否过期
            if (TimeInfo.Instance.ServerNow() > contract.ExpireTime)
            {
                return ErrorCode.ERR_HomeContractExpired;
            }

            // 移入 ActiveContracts
            contract.State = HomeContractState.Active;
            contract.AcceptTime = TimeInfo.Instance.ServerNow();
            contractComp.AvailableContracts.RemoveAt(contractIndex);
            contractComp.ActiveContracts.Add(contract);

            Log.Debug($"HomeContractHelper: accepted contract {contractId} for unit {unit.Id}");
            return ErrorCode.ERR_Success;
        }

        /// <summary>
        /// 领取合同奖励
        /// </summary>
        public static HomeCollectResult CollectContractReward(Unit unit, long contractId)
        {
            HomeContractComponent contractComp = unit.GetComponent<HomeContractComponent>();
            if (contractComp == null)
            {
                return new HomeCollectResult { ErrorCode = ErrorCode.ERR_HomeNotInHomeScene };
            }

            // 从 ActiveContracts 找到合同
            HomeContractData contract = null;
            foreach (var c in contractComp.ActiveContracts)
            {
                if (c.ContractId == contractId)
                {
                    contract = c;
                    break;
                }
            }

            if (contract == null)
            {
                return new HomeCollectResult { ErrorCode = ErrorCode.ERR_HomeContractNotFound };
            }

            if (contract.State != HomeContractState.Completed)
            {
                return new HomeCollectResult { ErrorCode = ErrorCode.ERR_HomeContractNotCompleted };
            }

            contract.State = HomeContractState.Claimed;

            Log.Debug($"HomeContractHelper: collected reward for contract {contractId}");
            return new HomeCollectResult
            {
                ErrorCode = ErrorCode.ERR_Success,
                ItemConfigIds = new List<int> { DefaultRewardItemId },
                ItemCounts = new List<int> { DefaultRewardCount }
            };
        }
    }
}

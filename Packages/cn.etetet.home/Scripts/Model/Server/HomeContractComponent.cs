using System.Collections.Generic;

namespace ET.Server
{
    [ComponentOf(typeof(Unit))]
    public class HomeContractComponent : Entity, IAwake, IDestroy, IDeserialize
    {
        public long LastRefreshTime;
        public List<HomeContractData> AvailableContracts = new();
        public List<HomeContractData> ActiveContracts = new();
    }
}

using System.Collections.Generic;

namespace ET.Server
{
    [ComponentOf(typeof(Unit))]
    public class HomeProductionComponent : Entity, IAwake, IDestroy, IDeserialize
    {
        public Dictionary<long, HomeProductionOrderData> ProductionOrders = new();
    }
}

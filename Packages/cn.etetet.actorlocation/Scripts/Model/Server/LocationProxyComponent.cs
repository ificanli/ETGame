using System;

namespace ET.Server
{
    [ComponentOf(typeof(Scene))]
    public class LocationProxyComponent: Entity, IAwake
    {
        private EntityRef<ServiceDiscoveryProxyComponent> serviceDiscoveryProxyComponent;

        public ServiceDiscoveryProxyComponent ServiceDiscoveryProxyComponent
        {
            get
            {
                return this.serviceDiscoveryProxyComponent;
            }
            set
            {
                this.serviceDiscoveryProxyComponent = value;
            }
        }
    }
}
using System;

namespace ET.Server
{
    [ComponentOf(typeof(Scene))]
    public class LocationProxyComponent: Entity, IAwake
    {
        private EntityRef<ServiceMessageSender> serviceMessageSender;

        public ServiceMessageSender ServiceMessageSender
        {
            get
            {
                return this.serviceMessageSender;
            }
            set
            {
                this.serviceMessageSender = value;
            }
        }
    }
}
using System.Net;

namespace ET.Server
{
    public partial class StartProcessConfig
    {
        public string InnerIP => this.StartMachineConfig.InnerIP;

        public string OuterIP => this.StartMachineConfig.OuterIP;

        public IPEndPoint InnerIPInnerPort
        {
            get
            {
                return NetworkHelper.ToIPEndPoint($"{this.InnerIP}:{this.Port}");
            }
        }

        public Address Address
        {
            get
            {
                return new Address(this.InnerIP, this.Port);
            }
        }

        public StartMachineConfig StartMachineConfig => StartMachineConfigCategory.Instance.Get(this.MachineId);

        public override void EndInit()
        {
        }
    }
}
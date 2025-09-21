using System.Net;

namespace ET.Server
{
    public partial class StartProcessConfig
    {
        public string InnerIP => this.StartMachineConfig.InnerIP;

        public string OuterIP => this.StartMachineConfig.OuterIP;

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
using System;

namespace ET.Server
{
    public class AddressSingleton: Singleton<AddressSingleton>, ISingletonAwake
    {
        public string InnerIP { get; set; }
        public int InnerPort { get; set; }
        public string OuterIP { get; set; }
        public int OuterPort { get; set; }
        
        public void Awake()
        {
            this.InnerIP = Environment.GetEnvironmentVariable("InnerIP");
            this.InnerPort = int.Parse(Environment.GetEnvironmentVariable("InnerPort") ?? "0");
            
            this.OuterIP = Environment.GetEnvironmentVariable("OuterIP");
            this.OuterPort = int.Parse(Environment.GetEnvironmentVariable("OuterPort") ?? "0");
        }
        
        public Address InnerAddress
        {
            get
            {
                return new Address(this.InnerIP, this.InnerPort);
            }
            set
            {
                this.InnerIP = value.IP;
                this.InnerPort = value.Port;
            }
        }
        
        public Address OuterAddress
        {
            get
            {
                return new Address(this.OuterIP, this.OuterPort);
            }
        }
    }
}
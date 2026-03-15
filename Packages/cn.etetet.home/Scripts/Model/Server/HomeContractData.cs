namespace ET.Server
{
    [EnableClass]
    public class HomeContractData
    {
        public long ContractId;
        public int ConfigId;
        public int State;
        public long AcceptTime;
        public long ExpireTime;
        public int Progress;
        public int Target;
    }
}

namespace ET.Client
{
    public struct Wait_M2C_UpdateItem: IWaitType
    {
        public M2C_UpdateItem M2C_UpdateItem;
        
        public int Error { get; set; }
    }
}
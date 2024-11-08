namespace ET.Client
{
    public struct Wait_M2C_SpellAdd: IWaitType
    {
        public M2C_SpellAdd Message { get; set; }
        public int Error { get; set; }
    }
    
    public struct Wait_M2C_SpellHit: IWaitType
    {
        public M2C_SpellHit Message { get; set; }
        public int Error { get; set; }
    }
    
    public struct Wait_M2C_SpellRemove: IWaitType
    {
        public M2C_SpellRemove Message { get; set; }
        public int Error { get; set; }
    }
}
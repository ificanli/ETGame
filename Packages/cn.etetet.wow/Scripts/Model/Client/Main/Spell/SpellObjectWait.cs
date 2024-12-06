namespace ET.Client
{
    public struct Wait_M2C_SpellHit: IWaitType
    {
        public M2C_SpellHit Message { get; set; }
        public int Error { get; set; }
    }
}
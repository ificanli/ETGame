namespace ET
{
    [EntitySystemOf(typeof(Buff))]
    public static partial class BuffSystem
    {
        [EntitySystem]
        private static void Awake(this Buff self, int configId)
        {
            self.ConfigId = configId;
        }
        
        public static BuffConfig GetConfig(this Buff self)
        {
            return BuffConfigCategory.Instance.Get(self.ConfigId);
        } 
        
        public static Unit GetOwner(this Buff self)
        {
            return self.Parent.GetParent<Unit>();
        } 
    }
}
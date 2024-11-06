namespace ET
{
    public static class BuffComponentSystem
    {
        public static Buff CreateBuff(this BuffComponent self, int configId)
        {
            Buff buff = self.AddChild<Buff, int>(configId);
            return buff;
        }
    }
}
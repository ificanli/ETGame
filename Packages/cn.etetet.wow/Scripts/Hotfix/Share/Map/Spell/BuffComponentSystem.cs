namespace ET
{
    public static class BuffComponentSystem
    {
        public static Buff CreateBuff(this BuffComponent self, BuffConfig buffConfig)
        {
            Buff buff = self.AddChild<Buff, BuffConfig>(buffConfig);
            return buff;
        }
    }
}
namespace ET.Server
{
    [Module(ModuleName.Spell)]
    public class BTBuffAddExpireTimeHandler: ABTHandler<BTBuffAddExpireTime>
    {
        protected override int Run(BTBuffAddExpireTime node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            long leftTime = buff.ExpireTime - buff.CreateTime;
            leftTime = (int)((leftTime + node.Value) * (100 + node.Pct) / 100f);
            long expireTime = buff.CreateTime + leftTime;
            BuffHelper.UpdateExpireTime(buff, expireTime);
            return 0;
        }
    }
}
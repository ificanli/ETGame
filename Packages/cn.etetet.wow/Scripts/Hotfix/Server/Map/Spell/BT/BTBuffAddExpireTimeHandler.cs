namespace ET.Server
{
    public class BTBuffAddExpireTimeHandler: ABTHandler<BTBuffAddExpireTime>
    {
        protected override int Run(BTBuffAddExpireTime node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            long expireTime = buff.ExpireTime + node.Value; 
            BuffHelper.UpdateExpireTime(buff, expireTime);
            return 0;
        }
    }
}
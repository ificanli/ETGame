namespace ET.Server
{
    public static class BuffHelper
    {
        public static Buff CreateBuff(Unit unit, int buffConfigId)
        {
            Buff buff = unit.GetComponent<BuffComponent>().CreateBuff(buffConfigId);
            M2C_BuffAdd m2CBuffAdd = M2C_BuffAdd.Create();
            m2CBuffAdd.BuffId = buff.Id;
            m2CBuffAdd.BuffConfigId = buffConfigId;
            m2CBuffAdd.UnitId = unit.Id;

            BuffConfig buffConfig = buff.GetConfig();
            MapMessageHelper.NoticeClient(unit, m2CBuffAdd, buffConfig.NoticeType);

            EffectHelper.RunBT<EffectServerBuffAdd>(buff);
            
            if (buffConfig.Duration > 0)
            {
                RunBuff(buff).NoContext();
            }
            
            return buff;
        }

        public static async ETTask RunBuff(Buff buff)
        {
            BuffConfig buffConfig = buff.GetConfig();
            TimerComponent timerComponent = buff.Root().GetComponent<TimerComponent>();

            long timeoutTime = buff.CreateTime + buffConfig.Duration;
            if (buffConfig.TickTime > 0)
            {
                long nextTick = buff.CreateTime + buffConfig.TickTime;
                while (true)
                {
                    await timerComponent.WaitTillAsync(nextTick);
                    if (buff.IsDisposed)
                    {
                        return;
                    }
                    if (nextTick > timeoutTime)
                    {
                        break;
                    }
                    EffectHelper.RunBT<EffectServerBuffTick>(buff);
                }
            }
            
            await timerComponent.WaitTillAsync(timeoutTime);
            if (buff.IsDisposed)
            {
                return;
            }
            RemoveBuff(buff);
        }

        public static void RemoveBuff(Buff buff)
        {
            Unit unit = buff.Parent.GetParent<Unit>();
            
            RemoveBuff(unit, buff.Id);
        }
        
        public static void RemoveBuff(Unit unit, long id)
        {
            BuffComponent buffComponent = unit.GetComponent<BuffComponent>();
            Buff buff = buffComponent.GetChild<Buff>(id);
            
            M2C_BuffRemove m2CBuffRemove = M2C_BuffRemove.Create();
            m2CBuffRemove.UnitId = unit.Id;
            m2CBuffRemove.BuffId = id;
            
            
            EffectHelper.RunBT<EffectServerBuffRemove>(buff);
            
            buffComponent.RemoveBuff(id);
            
            MapMessageHelper.NoticeClient(unit, m2CBuffRemove, buff.GetConfig().NoticeType);
        }
    }
}
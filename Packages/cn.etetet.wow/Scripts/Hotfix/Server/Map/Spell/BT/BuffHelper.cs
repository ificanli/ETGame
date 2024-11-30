namespace ET.Server
{
    public static class BuffHelper
    {
        public static Buff CreateBuff(Unit unit, Spell spell, int buffConfigId)
        {
            Buff buff = unit.GetComponent<BuffComponent>().CreateBuff(buffConfigId);
            BuffConfig buffConfig = buff.GetConfig();
            buff.AddComponent<SpellMainComponent>().Spell = spell;
            
            M2C_BuffAdd m2CBuffAdd = M2C_BuffAdd.Create();
            m2CBuffAdd.BuffId = buff.Id;
            m2CBuffAdd.BuffConfigId = buffConfigId;
            m2CBuffAdd.UnitId = unit.Id;
            m2CBuffAdd.CreateTime = buff.CreateTime;
            m2CBuffAdd.TickTime = buff.TickTime;
            m2CBuffAdd.ExpireTime = buff.ExpireTime;
            m2CBuffAdd.Stack = buff.Stack;
            
            MapMessageHelper.NoticeClient(unit, m2CBuffAdd, buffConfig.NoticeType);

            EffectHelper.RunBT<EffectServerBuffAdd>(buff);
            
            TimerComponent timerComponent = buff.Root().GetComponent<TimerComponent>();
            
            if (buffConfig.TickTime > 0)
            {
                WaitTick(buff, timerComponent).NoContext();
            }
            
            if (buffConfig.Duration > 0)
            {
                WaitTimeout(buff, timerComponent).NoContext();
            }
            
            return buff;
        }

        private static async ETTask WaitTick(Buff buff, TimerComponent timerComponent)
        {
            BuffConfig buffConfig = buff.GetConfig();

            int i = 0;
            while (true)
            {
                ++i;
                long nextTick = buff.CreateTime + buffConfig.TickTime * i;
                
                await timerComponent.WaitTillAsync(nextTick);
                if (buff.IsDisposed)
                {
                    return;
                }

                EffectHelper.RunBT<EffectServerBuffTick>(buff);
            }
        }

        private static async ETTask WaitTimeout(Buff buff, TimerComponent timerComponent)
        {
            await timerComponent.WaitTillAsync(buff.CreateTime + buff.GetConfig().Duration);
            if (buff.IsDisposed)
            {
                return;
            }
            RemoveBuff(buff, BuffRemoveType.Time);
        }

        public static void RemoveBuff(Buff buff, BuffRemoveType removeType)
        {
            Unit unit = buff.Parent.GetParent<Unit>();
            BuffComponent buffComponent = unit.GetComponent<BuffComponent>();
            
            M2C_BuffRemove m2CBuffRemove = M2C_BuffRemove.Create();
            m2CBuffRemove.UnitId = unit.Id;
            m2CBuffRemove.BuffId = buff.Id;


            buff.AddComponent<BuffRemoveTypeComponent>().BuffRemoveType = removeType;
            EffectHelper.RunBT<EffectServerBuffRemove>(buff);
            
            buffComponent.RemoveBuff(buff);
            
            MapMessageHelper.NoticeClient(unit, m2CBuffRemove, buff.GetConfig().NoticeType);
        }
        
        public static void RemoveBuff(Unit unit, long id, BuffRemoveType removeType)
        {
            BuffComponent buffComponent = unit.GetComponent<BuffComponent>();
            Buff buff = buffComponent.GetChild<Buff>(id);
            
            if (buff == null)
            {
                return;
            }
            
            RemoveBuff(buff, removeType);
        }
    }
}
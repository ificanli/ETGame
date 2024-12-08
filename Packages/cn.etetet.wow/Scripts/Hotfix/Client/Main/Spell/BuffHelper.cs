namespace ET.Client
{
    public static class BuffHelper
    {
        public static Buff CreateBuff(Unit unit, M2C_BuffAdd buffAdd)
        {
            Buff buff = unit.GetComponent<BuffComponent>().CreateBuff(buffAdd.BuffId, buffAdd.BuffConfigId, buffAdd.CasterId);
            buff.ExpireTime = buffAdd.ExpireTime;
            buff.TickTime = buffAdd.TickTime;
            buff.CreateTime = buffAdd.CreateTime;
            buff.Stack = buffAdd.Stack;
            if (buffAdd.SpellTarget != null)
            {
                SpellTargetComponent spellTargetComponent = buff.AddComponent<SpellTargetComponent>();
                spellTargetComponent.Units.AddRange(buffAdd.SpellTarget.TargetUnitId);
                spellTargetComponent.Position = buffAdd.SpellTarget.TargetPosition;
            }
            
            EffectHelper.RunBT<EffectClientBuffAdd>(buff);

            return buff;
        }
        
        public static void UpdateExpireTime(Buff buff, long expireTime)
        {
        }
        
        public static void UpdateTickTime(Buff buff, int tickTime)
        {
        }
        
        public static void UpdateStack(Buff buff, int stack)
        {
        }
        
        public static void RemoveBuff(Unit unit, long id, BuffFlags removeType)
        {
            BuffComponent buffComponent = unit.GetComponent<BuffComponent>();
            Buff buff = buffComponent.GetChild<Buff>(id);
            if (buff == null)
            {
                Log.Error($"not found buff: {id}");
                return;
            }
            buff.AddComponent<BuffRemoveTypeComponent>().BuffRemoveType = removeType;
            EffectHelper.RunBT<EffectClientBuffRemove>(buff);
            
            buffComponent.RemoveBuff(buff);
        }
    }
}
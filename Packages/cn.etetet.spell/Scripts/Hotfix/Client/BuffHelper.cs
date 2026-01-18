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
                SpellTargetComponent spellTargetComponent = buff.GetBuffData().AddComponent<SpellTargetComponent>();
                spellTargetComponent.Units.AddRange(buffAdd.SpellTarget.TargetUnitId);
                spellTargetComponent.Position = buffAdd.SpellTarget.TargetPosition;
            }

            EffectClientBuffAdd effect = buff.GetConfig().GetEffect<EffectClientBuffAdd>();
            if (effect != null)
            {
                using BTEnv env = BTEnv.Create(buff.Scene(), unit.Id);
                env.AddEntity(effect.Buff, buff);
                env.AddEntity(effect.Unit, unit);
                env.AddEntity(effect.Caster, buff.GetCaster());
                BTHelper.RunTree(effect, env);
            }

            Log.Debug($"Buff Create: {buffAdd.BuffConfigId}");
            return buff;
        }
        
        public static void UpdateBuff(Buff buff, long expireTime, int tickTime, int stack)
        {
            if (stack > 0)
            {
                buff.Stack = stack;
            }

            if (tickTime > 0)
            {
                buff.TickTime = tickTime;
            }

            if (expireTime > 0)
            {
                buff.ExpireTime = expireTime;
            }
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
            
            EffectClientBuffRemove effect = buff.GetConfig().GetEffect<EffectClientBuffRemove>();
            if (effect != null)
            {
                using BTEnv env = BTEnv.Create(buff.Scene(), unit.Id);
                env.AddEntity(effect.Buff, buff);
                env.AddEntity(effect.Unit, unit);
                env.AddEntity(effect.Caster, buff.GetCaster());
                BTHelper.RunTree(effect, env);
            }

            buffComponent.RemoveBuff(buff);
        }
    }
}
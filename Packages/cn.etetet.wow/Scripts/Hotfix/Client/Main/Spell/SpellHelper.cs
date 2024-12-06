using System;
using ET.Server;

namespace ET.Client
{
    public static class SpellHelper
    {
        public static void Cast(Unit unit, C2M_SpellCast c2MSpellCast)
        {
            unit.Root().GetComponent<ClientSenderComponent>().Send(c2MSpellCast);
        }

        public static async ETTask Start(Unit unit, long spellId, int spellConfigId)
        {
            ETCancellationToken cancellationToken = await ETTaskHelper.GetContextAsync<ETCancellationToken>();
            SpellComponent spellComponent = unit.GetComponent<SpellComponent>();
            try
            {
                Spell spell = spellComponent.CreateSpell(spellId, spellConfigId);
                spell.CancellationToken = cancellationToken;
                
                EffectHelper.RunBT<EffectClientSpellAdd>(spell);

                ObjectWait objectWait = spell.AddComponent<ObjectWait>();
                // 等待spell hit消息
                Wait_M2C_SpellHit waitM2CSpellHit = await objectWait.Wait<Wait_M2C_SpellHit>();
                if (cancellationToken.IsCancel())
                {
                    return;
                }
            
                UnitComponent unitComponent = unit.GetParent<UnitComponent>();
                SpellTargetComponent spellTargetComponent = spell.AddComponent<SpellTargetComponent>();
                foreach (long targetId in waitM2CSpellHit.Message.TargetUnitId)
                {
                    spellTargetComponent.Units.Add(unitComponent.Get(targetId));
                }
                spellTargetComponent.Position = waitM2CSpellHit.Message.TargetPosition;
            
                EffectHelper.RunBT<EffectClientSpellHit>(spell);
            }
            finally
            {
                // 客户端技能hit完就可以直接删除了
                spellComponent.RemoveSpell(spellId);
            }
        }
    }
}
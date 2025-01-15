using System;

namespace ET.Client
{
    public static class SpellHelper
    {
        public static void Cast(Unit unit, C2M_SpellCast c2MSpellCast)
        {
            unit.Root().GetComponent<ClientSenderComponent>().Send(c2MSpellCast);
        }
    }
}
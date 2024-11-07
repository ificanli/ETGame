namespace ET.Client
{
    public static class SpellHelper
    {
        public static void Cast(Scene current, int spellConfig)
        {
            C2M_SpellCast c2MSpellCast = C2M_SpellCast.Create();
            c2MSpellCast.SpellConfigId = spellConfig;
            current.Root().GetComponent<ClientSenderComponent>().Send(c2MSpellCast);
        }
    }
}
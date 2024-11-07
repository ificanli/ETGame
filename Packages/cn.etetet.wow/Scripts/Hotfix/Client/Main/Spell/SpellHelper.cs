namespace ET.Client
{
    public static class SpellHelper
    {
        public static async ETTask Cast(Scene current, C2M_SpellCast c2MSpellCast)
        {
            current.Root().GetComponent<ClientSenderComponent>().Send(c2MSpellCast);
            await ETTask.CompletedTask;
        }
    }
}
namespace ET.Server
{
    public class C2M_SpellCastHandler: MessageHandler<Unit, C2M_SpellCast>
    {
        protected override async ETTask Run(Unit unit, C2M_SpellCast message)
        {
            await SpellHelper.Cast(unit, message.SpellConfigId);
        }
    }
}
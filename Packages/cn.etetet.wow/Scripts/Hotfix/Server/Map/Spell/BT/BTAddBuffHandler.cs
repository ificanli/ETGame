namespace ET.Server
{
    public class BTAddBuffHandler: ABTHandler<BTAddBuff>
    {
        protected override int Run(BTAddBuff node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            Spell spell = env.GetEntity<Spell>(node.Spell);
            Buff buff = BuffHelper.CreateBuff(unit, spell.Main.Entity, node.ConfigId);
            return 0;
        }
    }
}
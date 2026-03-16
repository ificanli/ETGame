namespace ET.Server
{
    /// <summary>
    /// 肉鸽技能禁用效果的施法前拦截。
    /// </summary>
    [Invoke]
    public class SpellCastCheck_RogueSkillDisable : AInvokeHandler<SpellCastCheck, int>
    {
        public override int Handle(SpellCastCheck args)
        {
            Unit unit = args.Unit;
            if (unit == null || unit.IsDisposed)
            {
                return 0;
            }

            return RogueEffectQueryHelper.IsSkillDisabled(unit) ? TextConstDefine.SpellCast_SpellInCD : 0;
        }
    }
}

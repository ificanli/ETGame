namespace ET.Server
{
    /// <summary>
    /// 技能施放前的扩展拦截点。返回 0 表示允许施放，其它值为错误码。
    /// </summary>
    public struct SpellCastCheck
    {
        public EntityRef<Unit> Unit;
        public int SpellConfigId;
    }
}

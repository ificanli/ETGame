namespace ET.Server
{
    [ChildOf(typeof(SpellComponent))]
    public class Spell: Entity, IAwake<int>
    {
        public int ConfigId { get; set; } //配置表id
    }
}
namespace ET.Client
{
    [ComponentOf(typeof(Unit))]
    public class TargetComponent: Entity, IAwake
    {
        public EntityRef<Unit> Target { get; set; }
    }
}
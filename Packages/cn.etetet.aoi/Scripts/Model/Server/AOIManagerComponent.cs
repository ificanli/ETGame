namespace ET.Server
{
    [ComponentOf(typeof(Scene))]
    public class AOIManagerComponent: Entity, IAwake
    {
        public const int CellSize = 50 * 1000;
    }
}
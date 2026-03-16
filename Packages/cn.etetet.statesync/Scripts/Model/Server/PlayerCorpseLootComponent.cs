namespace ET.Server
{
    /// <summary>
    /// 玩家尸体盒运行时标记，防止重复创建。
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class PlayerCorpseLootComponent : Entity, IAwake
    {
        public string PointId;
        public long CreatedTime;
    }
}

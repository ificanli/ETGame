namespace ET.Server
{
    /// <summary>
    /// 怪物死亡并即将被地图层移除前抛出的事件。
    /// 高层玩法可在此时机创建尸体盒等运行时对象。
    /// </summary>
    public struct MonsterDeadBeforeRemove
    {
        public EntityRef<Unit> Unit;
    }
}

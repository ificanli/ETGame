namespace ET.Server
{
    [EntitySystemOf(typeof(GateMapComponent))]
    public static partial class GateMapComponentSystem
    {
        [EntitySystem]
        private static void Destroy(this GateMapComponent self)
        {

        }
        
        [EntitySystem]
        private static void Awake(this GateMapComponent self)
        {

        }

        public static async ETTask Create(this GateMapComponent self, long id)
        {
            self.Fiber = await self.Fiber().CreateFiber(id, self.Zone(), SceneType.Map, $"GateMap@{id}");
        }
    }
}
namespace ET.Server
{
    public static class ActorHelper
    {
        public static ActorId GetActorId(this Entity self)
        {
            Fiber root = self.Fiber();
            return new ActorId(AddressSingleton.Instance.InnerAddress, new FiberInstanceId(root.Id, self.InstanceId));
        }
    }
}
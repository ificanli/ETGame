namespace ET
{
    [EntitySystemOf(typeof(MailBoxComponent))]
    public static partial class MailBoxComponentSystem
    {
        [EntitySystem]       
        private static void Awake(this MailBoxComponent self, int mailBoxType)
        {
            Fiber fiber = self.Fiber();
            self.MailBoxType = mailBoxType;
            self.ParentInstanceId = self.Parent.InstanceId;
            fiber.Mailboxes.Add(self);
        }
        
        [EntitySystem]
        private static void Destroy(this MailBoxComponent self)
        {
            self.Fiber().Mailboxes.Remove(self.ParentInstanceId);
        }

        // 加到mailbox
        public static void Add(this MailBoxComponent self, int fromFiber, MessageObject messageObject)
        {
            // 根据mailboxType进行分发处理
            EventSystem.Instance.Invoke(self.MailBoxType, new MailBoxInvoker() {MailBoxComponent = self, MessageObject = messageObject, FromFiber = fromFiber});
        }
    }

    public struct MailBoxInvoker
    {
        public int FromFiber { get; set; }
        public MessageObject MessageObject { get; set; }
        public EntityRef<MailBoxComponent> MailBoxComponent { get; set; }
    }
    
    /// <summary>
    /// 挂上这个组件表示该Entity是一个Actor,接收的消息将会队列处理
    /// </summary>
    [ComponentOf]
    public class MailBoxComponent: Entity, IAwake<int>, IDestroy
    {
        public long ParentInstanceId { get; set; }
        // Mailbox的类型
        public int MailBoxType { get; set; }
    }
}
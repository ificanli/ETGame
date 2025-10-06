namespace ET.Server
{
    [Invoke(MailBoxType.OrderedMessage)]
    public class MailBoxType_OrderedMessageHandler: AInvokeHandler<MailBoxInvoker>
    {
        public override void Handle(MailBoxInvoker args)
        {
            HandleInner(args).NoContext();
        }

        private static async ETTask HandleInner(MailBoxInvoker args)
        {
            MailBoxComponent mailBoxComponent = args.MailBoxComponent;
            
            MessageObject messageObject = args.MessageObject;

            Fiber fiber = mailBoxComponent.Fiber();
            if (fiber.IsDisposed)
            {
                return;
            }

            EntityRef<MailBoxComponent> mailBoxComponentRef = mailBoxComponent;
            using EntityRef<CoroutineLock> coroutineLockRef = await fiber.Root.GetComponent<CoroutineLockComponent>().Wait(CoroutineLockType.Mailbox, mailBoxComponent.ParentInstanceId);

            // 占用锁只能占用5秒,5秒之后解锁
            CoroutineLockHelper.LockTime(fiber, coroutineLockRef, 5000).NoContext();

            mailBoxComponent = mailBoxComponentRef;
            if (mailBoxComponent == null)
            {
                if (messageObject is IRequest request)
                {
                    IResponse resp = MessageHelper.CreateResponse(request.GetType(), request.RpcId, ErrorCode.ERR_NotFoundActor);
                    fiber.Root.GetComponent<ProcessInnerSender>().Reply(args.FromFiber, resp);
                }
                return;
            }
            await MessageDispatcher.Instance.HandleAsync(mailBoxComponent.Parent, args.FromFiber, messageObject);
        }
    }
}
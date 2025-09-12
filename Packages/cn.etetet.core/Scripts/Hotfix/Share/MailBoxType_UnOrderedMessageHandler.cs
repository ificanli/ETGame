namespace ET
{
    [Invoke(MailBoxType.UnOrderedMessage)]
    public class MailBoxType_UnOrderedMessageHandler: AInvokeHandler<MailBoxInvoker>
    {
        public override void Handle(MailBoxInvoker args)
        {
            MailBoxComponent mailBoxComponent = args.MailBoxComponent;
            
            MessageObject messageObject = args.MessageObject;
            
            MessageDispatcher.Instance.Handle(mailBoxComponent.Parent, args.FromFiber, messageObject);
        }
    }
}
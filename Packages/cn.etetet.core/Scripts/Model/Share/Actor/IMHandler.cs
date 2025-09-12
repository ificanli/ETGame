using System;

namespace ET
{
    public interface IMHandler
    {
        ETTask Handle(Entity entity, int fromFiber, MessageObject actorMessage);
        Type GetRequestType();
        Type GetResponseType();
    }
}
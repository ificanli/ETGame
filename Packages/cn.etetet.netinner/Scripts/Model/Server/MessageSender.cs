using System;
using System.Collections.Generic;

namespace ET.Server
{
    [ComponentOf(typeof(Scene))]
    public class MessageSender: Entity, IAwake, IDestroy
    {
        public const long TIMEOUT_TIME = 40 * 1000;

        public readonly Dictionary<int, MessageSenderStruct> requestCallback = new();
    }
}
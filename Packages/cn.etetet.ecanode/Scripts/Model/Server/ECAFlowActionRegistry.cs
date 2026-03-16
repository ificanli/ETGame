using System;
using System.Collections.Generic;

namespace ET.Server
{
    public static class ECAFlowActionRegistry
    {
        [StaticField]
        private static readonly Dictionary<string, Func<ECAFlowActionInvoke, ETTask>> Handlers = new();

        public static void Register(string actionKey, Func<ECAFlowActionInvoke, ETTask> handler)
        {
            if (string.IsNullOrWhiteSpace(actionKey) || handler == null)
            {
                return;
            }

            Handlers[actionKey] = handler;
        }

        public static bool TryGet(string actionKey, out Func<ECAFlowActionInvoke, ETTask> handler)
        {
            return Handlers.TryGetValue(actionKey, out handler);
        }

        public static int Count()
        {
            return Handlers.Count;
        }
    }
}

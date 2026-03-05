namespace ET.Client
{
    public static class ECAInteractHelper
    {
        public static ECAInteractClientComponent GetOrAddRuntime(Scene root)
        {
            if (root == null)
            {
                return null;
            }

            ECAInteractClientComponent runtime = root.GetComponent<ECAInteractClientComponent>();
            if (runtime == null)
            {
                runtime = root.AddComponent<ECAInteractClientComponent>();
            }

            return runtime;
        }

        public static async ETTask TryInteractFocus(Scene root)
        {
            ECAInteractClientComponent runtime = GetOrAddRuntime(root);
            if (runtime == null || string.IsNullOrWhiteSpace(runtime.FocusPointId))
            {
                return;
            }

            string pointId = runtime.FocusPointId;
            C2M_ECAInteract request = C2M_ECAInteract.Create();
            request.PointId = pointId;
            M2C_ECAInteract response = await root.GetComponent<ClientSenderComponent>().Call(request) as M2C_ECAInteract;
            if (response == null)
            {
                return;
            }

            if (response.Error != ErrorCode.ERR_Success)
            {
                Log.Warning($"[ECAClient] interact failed: point={pointId}, error={response.Error}, msg={response.Message}");
            }
        }

        public static void CancelSearch(Scene root)
        {
            ECAInteractClientComponent runtime = GetOrAddRuntime(root);
            if (runtime == null || string.IsNullOrWhiteSpace(runtime.SearchingPointId))
            {
                return;
            }

            C2M_ECASearchCancel msg = C2M_ECASearchCancel.Create();
            msg.PointId = runtime.SearchingPointId;
            root.GetComponent<ClientSenderComponent>().Send(msg);

            runtime.SearchState = ContainerSearchState.Interrupted;
            runtime.SearchRemainMs = 0;
            runtime.SearchingPointId = null;
        }

        public static async ETTask TakeAll(Scene root)
        {
            ECAInteractClientComponent runtime = GetOrAddRuntime(root);
            if (runtime == null || string.IsNullOrWhiteSpace(runtime.OpenContainerPointId))
            {
                return;
            }

            string pointId = runtime.OpenContainerPointId;
            C2M_ContainerTakeAll request = C2M_ContainerTakeAll.Create();
            request.PointId = pointId;
            M2C_ContainerTakeAll response = await root.GetComponent<ClientSenderComponent>().Call(request) as M2C_ContainerTakeAll;
            if (response != null && response.Error != ErrorCode.ERR_Success)
            {
                Log.Warning($"[ECAClient] take all failed: point={pointId}, error={response.Error}, msg={response.Message}");
            }
        }

        public static async ETTask TakeItem(Scene root, int slotIndex)
        {
            ECAInteractClientComponent runtime = GetOrAddRuntime(root);
            if (runtime == null || string.IsNullOrWhiteSpace(runtime.OpenContainerPointId))
            {
                return;
            }

            string pointId = runtime.OpenContainerPointId;
            C2M_ContainerTakeItem request = C2M_ContainerTakeItem.Create();
            request.PointId = pointId;
            request.SlotIndex = slotIndex;
            M2C_ContainerTakeItem response = await root.GetComponent<ClientSenderComponent>().Call(request) as M2C_ContainerTakeItem;
            if (response != null && response.Error != ErrorCode.ERR_Success)
            {
                Log.Warning($"[ECAClient] take item failed: point={pointId}, slot={slotIndex}, error={response.Error}, msg={response.Message}");
            }
        }

        public static void CloseContainer(Scene root)
        {
            ECAInteractClientComponent runtime = GetOrAddRuntime(root);
            if (runtime == null || string.IsNullOrWhiteSpace(runtime.OpenContainerPointId))
            {
                return;
            }

            C2M_ContainerClose msg = C2M_ContainerClose.Create();
            msg.PointId = runtime.OpenContainerPointId;
            root.GetComponent<ClientSenderComponent>().Send(msg);

            runtime.OpenContainerPointId = null;
            runtime.ContainerItems.Clear();
        }
    }
}

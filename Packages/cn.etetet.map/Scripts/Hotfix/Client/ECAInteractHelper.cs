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

            runtime.EnsureLocalConcealmentConfigLoaded(root);

            return runtime;
        }

        public static async ETTask TryInteractFocus(Scene root)
        {
            ECAInteractClientComponent runtime = GetOrAddRuntime(root);
            if (runtime == null)
            {
                Log.Warning("[ECAClient] TryInteractFocus skipped: runtime missing");
                return;
            }

            if (string.IsNullOrWhiteSpace(runtime.FocusPointId) && runtime.InRangePointIds.Count > 0)
            {
                foreach (string candidatePointId in runtime.InRangePointIds)
                {
                    runtime.FocusPointId = candidatePointId;
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(runtime.FocusPointId))
            {
                Log.Warning($"[ECAClient] TryInteractFocus skipped: focus missing, inRangeCount={runtime.InRangePointIds.Count}");
                return;
            }

            string pointId = runtime.FocusPointId;
            Log.Warning($"[ECAClient] TryInteractFocus send request: point={pointId}");
            C2M_ECAInteract request = C2M_ECAInteract.Create();
            request.PointId = pointId;
            M2C_ECAInteract response = await root.GetComponent<ClientSenderComponent>().Call(request) as M2C_ECAInteract;
            if (response == null)
            {
                Log.Warning($"[ECAClient] TryInteractFocus no response: point={pointId}");
                return;
            }

            if (response.Error != ErrorCode.ERR_Success)
            {
                Log.Warning($"[ECAClient] interact failed: point={pointId}, error={response.Error}, msg={response.Message}");
                return;
            }

            Log.Warning($"[ECAClient] TryInteractFocus success: point={pointId}");
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

        public static async ETTask MoveContainerItem(
            Scene root,
            int sourceAreaType,
            int sourceSlot,
            long sourceItemId,
            int targetAreaType,
            int targetSlot)
        {
            ECAInteractClientComponent runtime = GetOrAddRuntime(root);
            bool needPoint = sourceAreaType == (int)ContainerItemAreaType.Container || targetAreaType == (int)ContainerItemAreaType.Container;
            if (runtime == null || (needPoint && string.IsNullOrWhiteSpace(runtime.OpenContainerPointId)))
            {
                return;
            }

            string pointId = runtime.OpenContainerPointId;
            C2M_ContainerMoveItem request = C2M_ContainerMoveItem.Create();
            request.PointId = pointId;
            request.SourceAreaType = sourceAreaType;
            request.SourceSlot = sourceSlot;
            request.SourceItemId = sourceItemId;
            request.TargetAreaType = targetAreaType;
            request.TargetSlot = targetSlot;
            M2C_ContainerMoveItem response = await root.GetComponent<ClientSenderComponent>().Call(request) as M2C_ContainerMoveItem;
            if (response != null && response.Error != ErrorCode.ERR_Success)
            {
                Log.Warning($"[ECAClient] move container item failed: point={pointId}, sourceArea={sourceAreaType}, sourceSlot={sourceSlot}, sourceItemId={sourceItemId}, targetArea={targetAreaType}, targetSlot={targetSlot}, error={response.Error}, msg={response.Message}");
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

            EventSystem.Instance.Publish(root, new ECAContainerCloseUIEvent
            {
                PointId = runtime.OpenContainerPointId,
                UiKey = runtime.OpenContainerUiKey
            });
            runtime.OpenContainerPointId = null;
            runtime.OpenContainerUiKey = null;
            runtime.ContainerItems.Clear();
        }
    }
}

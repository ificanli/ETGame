using System;

namespace ET.Client
{
    [Event(SceneType.Client)]
    public class ECAContainerOpenUIEvent_OpenPanel : AEvent<Scene, ECAContainerOpenUIEvent>
    {
        protected override async ETTask Run(Scene scene, ECAContainerOpenUIEvent args)
        {
            string panelName = ECAContainerUIPanelNameHelper.Normalize(args.UiKey);
            if (string.IsNullOrWhiteSpace(panelName))
            {
                Log.Warning($"[ECAClient][ContainerUI] open skipped, ui_key missing, point={args.PointId}");
                return;
            }

            YIUIRootComponent yiuiRoot = scene.YIUIRoot();
            if (yiuiRoot == null)
            {
                Log.Warning($"[ECAClient][ContainerUI] open skipped, YIUIRoot missing, point={args.PointId}, uiKey={args.UiKey ?? "null"}");
                return;
            }

            if (string.Equals(panelName, nameof(SearchPanelComponent), StringComparison.Ordinal))
            {
                SearchPanelOpenContextComponent openContext = SearchPanelOpenContextHelper.GetOrAdd(scene);
                openContext?.PrepareContainerOpen(args.PointId);

                // 打开容器面板前同步背包数据，确保客户端 ItemComponent 维度与服务端一致
                EntityRef<Scene> sceneRef = scene;
                await SyncBagDataBeforeOpen(scene);
                scene = sceneRef;
                if (scene == null || scene.IsDisposed)
                {
                    return;
                }
            }

            // await 后重新获取 yiuiRoot，遵循 EntityRef 规范
            YIUIRootComponent yiuiRootAfter = scene.YIUIRoot();
            if (yiuiRootAfter == null)
            {
                return;
            }

            Log.Info($"[ECAClient][ContainerUI] open panel={panelName}, point={args.PointId}, uiKey={args.UiKey ?? "null"}");
            await yiuiRootAfter.OpenPanelAsync(panelName);
        }

        private static async ETTask SyncBagDataBeforeOpen(Scene root)
        {
            if (root == null || root.IsDisposed)
            {
                return;
            }

            ClientSenderComponent sender = root.GetComponent<ClientSenderComponent>();
            if (sender == null)
            {
                return;
            }

            C2M_SyncBagData request = C2M_SyncBagData.Create();
            EntityRef<Scene> rootRef = root;
            M2C_SyncBagData response = await sender.Call(request) as M2C_SyncBagData;
            root = rootRef;
            if (root == null || root.IsDisposed)
            {
                return;
            }

            if (response == null || response.Error != ErrorCode.ERR_Success)
            {
                Log.Warning($"[ECAClient][ContainerUI] sync bag before container open failed: error={response?.Error ?? -1}");
                return;
            }

            ItemComponent itemComponent = root.GetComponent<ItemComponent>();
            if (itemComponent == null)
            {
                return;
            }

            itemComponent.Clear();
            if (response.Width > 0 && response.Height > 0)
            {
                itemComponent.SetSize(response.Width, response.Height);
            }
            else
            {
                itemComponent.SetCapacity(response.Capacity);
            }

            foreach (ItemData itemData in response.Items)
            {
                itemComponent.UpdateItem(itemData.ItemId, itemData.SlotIndex, itemData.ConfigId, itemData.Count, itemData.GridWidth, itemData.GridHeight);
            }

            Log.Info($"[ECAClient][ContainerUI] sync bag success before container open: size={itemComponent.Width}x{itemComponent.Height}, items={response.Items.Count}");
        }
    }

    [Event(SceneType.Client)]
    public class ECAContainerCloseUIEvent_ClosePanel : AEvent<Scene, ECAContainerCloseUIEvent>
    {
        protected override async ETTask Run(Scene scene, ECAContainerCloseUIEvent args)
        {
            string panelName = ECAContainerUIPanelNameHelper.Normalize(args.UiKey);
            if (string.IsNullOrWhiteSpace(panelName))
            {
                Log.Warning($"[ECAClient][ContainerUI] close skipped, ui_key missing, point={args.PointId}");
                await ETTask.CompletedTask;
                return;
            }

            YIUIMgrComponent yiuiMgr = scene.YIUIMgr();
            if (yiuiMgr == null)
            {
                Log.Warning($"[ECAClient][ContainerUI] close skipped, YIUIMgr missing, point={args.PointId}, uiKey={args.UiKey ?? "null"}");
                await ETTask.CompletedTask;
                return;
            }

            Log.Info($"[ECAClient][ContainerUI] close panel={panelName}, point={args.PointId}, uiKey={args.UiKey ?? "null"}");
            yiuiMgr.ClosePanel(panelName);
            await ETTask.CompletedTask;
        }
    }

    internal static class ECAContainerUIPanelNameHelper
    {
        public static string Normalize(string uiKey)
        {
            if (string.IsNullOrWhiteSpace(uiKey))
            {
                return null;
            }

            string panelName = uiKey.Trim();
            if (panelName.EndsWith("PanelComponent", StringComparison.Ordinal))
            {
                return panelName;
            }

            if (panelName.EndsWith("Panel", StringComparison.Ordinal))
            {
                return $"{panelName}Component";
            }

            return $"{panelName}PanelComponent";
        }
    }
}

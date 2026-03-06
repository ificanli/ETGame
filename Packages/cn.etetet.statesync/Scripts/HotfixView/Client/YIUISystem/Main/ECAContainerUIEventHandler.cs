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

            Log.Info($"[ECAClient][ContainerUI] open panel={panelName}, point={args.PointId}, uiKey={args.UiKey ?? "null"}");
            await yiuiRoot.OpenPanelAsync(panelName);
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

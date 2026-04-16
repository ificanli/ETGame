using UnityEngine;

namespace ET.Client
{
    [FriendOf(typeof(MainPanelComponent))]
    public static partial class MainPanelComponentSystem
    {
        private const int RunTimeLimitWarningSeconds = 60;

        private static void RefreshRunTimeLimit(this MainPanelComponent self, RunTimeLimitClientComponent runtime)
        {
            bool show = runtime != null && runtime.IsActive && runtime.EndTimeMs > 0;
            long remainSeconds = 0;
            if (show)
            {
                long remainMs = runtime.EndTimeMs - TimeInfo.Instance.ClientNow();
                if (remainMs < 0)
                {
                    remainMs = 0;
                }

                runtime.RemainMs = remainMs;
                remainSeconds = remainMs <= 0 ? 0 : (long)System.Math.Ceiling(remainMs / 1000d);
            }

            if (self.u_ComRunTimeLimitRoot != null && self.u_ComRunTimeLimitRoot.gameObject.activeSelf != show)
            {
                self.u_ComRunTimeLimitRoot.gameObject.SetActive(show);
            }

            if (!show)
            {
                self.LastRunTimeLimitVisible = false;
                self.LastRunTimeLimitRemainSeconds = long.MinValue;
                return;
            }

            if (self.u_ComRunTimeLimitText == null)
            {
                self.LastRunTimeLimitVisible = show;
                self.LastRunTimeLimitRemainSeconds = remainSeconds;
                return;
            }

            if (!self.LastRunTimeLimitVisible || self.LastRunTimeLimitRemainSeconds != remainSeconds)
            {
                self.u_ComRunTimeLimitText.text = FormatRunTimeLimit(remainSeconds);
                self.u_ComRunTimeLimitText.color = remainSeconds <= RunTimeLimitWarningSeconds
                    ? GetRunTimeLimitWarningTextColor()
                    : GetRunTimeLimitNormalTextColor();
            }

            self.LastRunTimeLimitVisible = true;
            self.LastRunTimeLimitRemainSeconds = remainSeconds;
        }

        private static void BindRunTimeLimitUi(this MainPanelComponent self, bool reset)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            if (self.u_ComRunTimeLimitRoot == null || self.u_ComRunTimeLimitText == null)
            {
                Log.Warning("[MainPanel] missing run time limit prefab nodes: RunTimeLimitRoot / RunTimeLimitText");
                return;
            }

            if (!reset)
            {
                return;
            }

            self.u_ComRunTimeLimitRoot.gameObject.SetActive(false);
            self.u_ComRunTimeLimitText.text = FormatRunTimeLimit(0);
            self.u_ComRunTimeLimitText.color = GetRunTimeLimitNormalTextColor();
        }

        private static string FormatRunTimeLimit(long remainSeconds)
        {
            long totalSeconds = remainSeconds > 0 ? remainSeconds : 0;
            long minutes = totalSeconds / 60;
            long seconds = totalSeconds % 60;
            return $"{minutes:00}:{seconds:00}";
        }

        private static Color32 GetRunTimeLimitNormalTextColor()
        {
            return new Color32(244, 238, 214, 255);
        }

        private static Color32 GetRunTimeLimitWarningTextColor()
        {
            return new Color32(255, 108, 108, 255);
        }
    }
}

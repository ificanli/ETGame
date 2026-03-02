using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    public static partial class MainPanelComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this MainPanelComponent self)
        {
            // 找到摇杆并注入Entity引用
            JoystickView joystickView = self.UIBase.OwnerGameObject.GetComponentInChildren<JoystickView>();
            if (joystickView != null)
            {
                joystickView.SetEntity(self);
            }
        }

        [EntitySystem]
        private static void Destroy(this MainPanelComponent self)
        {
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this MainPanelComponent self)
        {
            await ETTask.CompletedTask;
            return true;
        }

        #region YIUIEvent开始
        #endregion YIUIEvent结束
    }
}

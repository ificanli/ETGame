using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// Author  Lsy
    /// Date    2024.12.10
    /// Desc
    /// </summary>
    [FriendOf(typeof(HPView3DComponent))]
    public static partial class HPView3DComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this HPView3DComponent self)
        {
            self.m_OwnerUnit = self.Parent.GetParent<Unit>();
        }

        [EntitySystem]
        private static void Destroy(this HPView3DComponent self)
        {
        }

        [EntitySystem]
        private static void LateUpdate(this ET.Client.HPView3DComponent self)
        {
            if (self.Player == null)
            {
                var player = UnitHelper.GetMyUnitFromCurrentScene(self.Scene());
                if (player == null)
                {
                    self.m_Player = default;
                }
                else
                {
                    self.m_Player = player;
                }
            }

            if (self.Player == null)
            {
                self.u_DataShow.SetValue(false);
                return;
            }

            if (Vector3.Distance(self.Player.Position, self.OwnerUnit.Position) <= 10)
            {
                var screenPos = Camera.main.WorldToScreenPoint(self.OwnerUnit.Position);

                //如果不在屏幕内，则不显示
                //上下左右各留100像素的范围
                if (screenPos.x < 100 || screenPos.x > Screen.width - 100 || screenPos.y < 100 || screenPos.y > Screen.height - 100)
                {
                    self.u_DataShow.SetValue(false);
                    return;
                }

                self.u_DataShow.SetValue(true);
                self.UIBase.OwnerRectTransform.LookAt(Camera.main.transform.position);
            }
            else
            {
                self.u_DataShow.SetValue(false);
            }
        }

        #region YIUIEvent开始

        #endregion YIUIEvent结束
    }
}
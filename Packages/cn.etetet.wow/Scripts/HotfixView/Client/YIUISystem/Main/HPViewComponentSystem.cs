using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// Author  Lsy
    /// Date    2024.12.9
    /// Desc
    /// </summary>
    [FriendOf(typeof(HPViewComponent))]
    public static partial class HPViewComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this HPViewComponent self)
        {
            self.m_OwnerUnit = self.Parent.GetParent<Unit>();

            var bindPoint = self.OwnerUnit?.GetComponent<GameObjectComponent>()?.GameObject?.GetComponent<BindPointComponent>()?.BindPoints;
            if (bindPoint != null)
            {
                if (!bindPoint.TryGetValue(BindPoint.HP, out self.HPPoint))
                {
                    Log.Error($" {self.OwnerUnit?.Config().Name} 没有找到 HP 点位");
                }
            }
            else
            {
                Log.Error($" {self.OwnerUnit?.Config().Name} 没有找到 BindPointComponent");
            }

            self.m_Numeric = self.OwnerUnit.GetComponent<NumericComponent>();
        }

        [EntitySystem]
        private static void Destroy(this HPViewComponent self)
        {
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this HPViewComponent self)
        {
            await ETTask.CompletedTask;
            return true;
        }

        [EntitySystem]
        private static void LateUpdate(this HPViewComponent self)
        {
            if (self.UpdateHP() <= 0)
            {
                self.SetUICache();
                return;
            }

            if (self.HPPoint == null)
            {
                return;
            }

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
                self.SetUICache();
                return;
            }

            if (Vector3.Distance(self.Player.Position, self.OwnerUnit.Position) <= 10)
            {
                var screenPos = Camera.main.WorldToScreenPoint(self.HPPoint.position);

                //如果不在屏幕内，则不显示
                //上下左右各留100像素的范围
                if (screenPos.x < 100 || screenPos.x > Screen.width - 100 || screenPos.y < 100 || screenPos.y > Screen.height - 100)
                {
                    self.SetUICache();
                    return;
                }

                if (self.UIBase.OwnerRectTransform.parent == YIUIMgrComponent.Inst.UICache)
                {
                    self.DynamicEvent(new EventMain_ShowHPView
                    {
                        HPView = self
                    }).NoContext();
                }

                if (self.UIBase.OwnerRectTransform.parent != YIUIMgrComponent.Inst.UICache)
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)self.UIBase.OwnerRectTransform.parent, screenPos, YIUIMgrComponent.Inst.UICamera, out var targetLocalPosition);
                    self.UIBase.OwnerRectTransform.localPosition = targetLocalPosition;
                }
            }
            else
            {
                self.SetUICache();
            }
        }

        private static void SetUICache(this HPViewComponent self)
        {
            if (self.UIBase.OwnerRectTransform.parent != YIUIMgrComponent.Inst.UICache)
            {
                self.UIBase.OwnerRectTransform.SetParent(YIUIMgrComponent.Inst.UICache);
            }
        }

        private static float UpdateHP(this HPViewComponent self)
        {
            if (self.Numeric == null) return 0;
            var hp    = self.Numeric.GetAsInt(NumericType.HP);
            var maxHP = self.Numeric.GetAsInt(NumericType.MaxHP);
            if (hp == 0 && maxHP == 0) return 0;
            var ratio = maxHP <= 0 ? 0 : (float)hp / maxHP;
            self.u_DataHPRatio.SetValue(ratio);
            return ratio;
        }

        #region YIUIEvent开始

        #endregion YIUIEvent结束
    }
}
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

            self.m_Numeric = self.OwnerUnit.NumericComponent;
            self.ExpBarRoot = FindExpBarRoot(self);
            self.LastShowExpBar = self.ExpBarRoot != null && self.ExpBarRoot.gameObject.activeSelf;
            self.u_DataTxtLevel?.SetValue("1", true);
            self.u_DataCurExp?.SetValue(0f, true);
            self.SetExpBarVisible(false);
        }

        [EntitySystem]
        private static void Destroy(this HPViewComponent self)
        {
            self.HideImmediately();
        }

        public static void HideImmediately(this HPViewComponent self)
        {
            // Unit 销毁或移除前先把 2D 血条归位到缓存节点，避免残留在 HUD Canvas 上。
            if (self?.UIBase?.OwnerRectTransform == null)
            {
                return;
            }

            var mgr = self.YIUIMgr();
            if (mgr?.UICache == null)
            {
                return;
            }

            if (self.UIBase.OwnerRectTransform.parent != mgr.UICache)
            {
                self.UIBase.OwnerRectTransform.SetParent(mgr.UICache);
            }
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

            self.UpdateDisplayLevelAndExp();

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

                if (self.UIBase.OwnerRectTransform.parent == self.YIUIMgr().UICache)
                {
                    self.DynamicEvent(new EventMain_ShowHPView
                    {
                        HPView = self
                    }).Coroutine();
                }

                if (self.UIBase.OwnerRectTransform.parent != self.YIUIMgr().UICache)
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)self.UIBase.OwnerRectTransform.parent, screenPos, self.YIUIMgr().UICamera, out var targetLocalPosition);
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
            self.HideImmediately();
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

        private static void UpdateDisplayLevelAndExp(this HPViewComponent self)
        {
            if (self.OwnerUnit == null || self.Player == null)
            {
                return;
            }

            bool isMyUnit = self.OwnerUnit.Id == self.Player.Id;
            int displayLevel = self.OwnerUnit.GetComponent<UnitDisplayLevelComponent>()?.Level ?? 1;
            float expRatio = 0f;

            if (isMyUnit)
            {
                RogueClientComponent runtime = RogueClientHelper.GetOrAddRuntime(self.Root());
                if (runtime != null)
                {
                    displayLevel = runtime.Level > 0 ? runtime.Level : displayLevel;
                    int safeNeedExp = runtime.NeedExp > 0 ? runtime.NeedExp : 1;
                    expRatio = Mathf.Clamp01(runtime.CurrentExp / (float)safeNeedExp);
                }
            }

            if (displayLevel <= 0)
            {
                displayLevel = 1;
            }

            if (self.LastDisplayLevel != displayLevel)
            {
                self.LastDisplayLevel = displayLevel;
                self.u_DataTxtLevel?.SetValue(displayLevel.ToString(), true);
            }

            if (Mathf.Abs(self.LastDisplayExpRatio - expRatio) > 0.0001f)
            {
                self.LastDisplayExpRatio = expRatio;
                self.u_DataCurExp?.SetValue(expRatio, true);
            }

            self.SetExpBarVisible(isMyUnit);
        }

        private static void SetExpBarVisible(this HPViewComponent self, bool visible)
        {
            if (self.ExpBarRoot == null || self.LastShowExpBar == visible)
            {
                return;
            }

            self.LastShowExpBar = visible;
            self.ExpBarRoot.gameObject.SetActive(visible);
        }

        private static Transform FindExpBarRoot(HPViewComponent self)
        {
            Transform rootTransform = self.UIBase?.OwnerGameObject?.transform;
            if (rootTransform == null)
            {
                return null;
            }

            Transform expBar = rootTransform.Find("ExpBar");
            if (expBar != null)
            {
                return expBar;
            }

            foreach (Transform child in rootTransform.GetComponentsInChildren<Transform>(true))
            {
                if (child != null && child.name == "ExpBar")
                {
                    return child;
                }
            }

            return null;
        }

        #region YIUIEvent开始

        #endregion YIUIEvent结束
    }

    [Event(SceneType.Current)]
    public class BeforeUnitRemove_HideHPView : AEvent<Scene, BeforeUnitRemove>
    {
        protected override async ETTask Run(Scene scene, BeforeUnitRemove args)
        {
            Unit unit = args.Unit;
            if (unit?.Children == null)
            {
                return;
            }

            foreach (Entity child in unit.Children.Values)
            {
                YIUIChild uiChild = child as YIUIChild;
                if (uiChild == null || uiChild.IsDisposed)
                {
                    continue;
                }

                uiChild.GetComponent<HPViewComponent>()?.HideImmediately();
                uiChild.GetComponent<HPView3DComponent>()?.HideImmediately();
            }

            await ETTask.CompletedTask;
        }
    }
}

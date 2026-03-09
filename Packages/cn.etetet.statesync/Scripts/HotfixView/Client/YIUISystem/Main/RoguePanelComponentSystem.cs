using UnityEngine;
using YIUIFramework;

namespace ET.Client
{
    [FriendOf(typeof(RoguePanelComponent))]
    public static partial class RoguePanelComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this RoguePanelComponent self)
        {
            self.RefreshView();
        }

        [EntitySystem]
        private static void Destroy(this RoguePanelComponent self)
        {
            RogueOptionViewHelper.ResetOption(self.GetOptionComponent(0));
            RogueOptionViewHelper.ResetOption(self.GetOptionComponent(1));
            RogueOptionViewHelper.ResetOption(self.GetOptionComponent(2));
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this RoguePanelComponent self)
        {
            self.RefreshView();
            await ETTask.CompletedTask;
            return true;
        }

        public static void RefreshView(this RoguePanelComponent self)
        {
            Scene root = self.Root();
            RogueClientComponent runtime = RogueClientHelper.GetOrAddRuntime(root);

            for (int i = 0; i < 3; ++i)
            {
                RectTransform optionRect = self.GetOptionRectTransform(i);
                RogueOptionComponent optionComponent = self.GetOptionComponent(i);
                if (optionRect == null || optionComponent == null)
                {
                    continue;
                }

                bool hasData = runtime != null && i < runtime.ChoiceOptions.Count;
                optionRect.gameObject.SetActive(hasData);
                if (!hasData)
                {
                    RogueOptionViewHelper.ResetOption(optionComponent);
                    continue;
                }

                RogueOptionViewHelper.BindOption(optionComponent, self, i, runtime.ChoiceOptions[i]);
            }
        }

        private static RogueOptionComponent GetOptionComponent(this RoguePanelComponent self, int index)
        {
            return index switch
            {
                0 => self.UIRogueOption,
                1 => self.UIRogueOption1,
                2 => self.UIRogueOption2,
                _ => null,
            };
        }

        private static RectTransform GetOptionRectTransform(this RoguePanelComponent self, int index)
        {
            return index switch
            {
                0 => self.u_ComRogueOptionRectTransform,
                1 => self.u_ComRogueOption1RectTransform,
                2 => self.u_ComRogueOption2RectTransform,
                _ => null,
            };
        }
    }
}

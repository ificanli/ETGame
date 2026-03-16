using TMPro;
using UnityEngine;

namespace ET.Client
{
    [EntitySystemOf(typeof(CombatTextComponent))]
    [FriendOf(typeof(CombatTextComponent))]
    public static partial class CombatTextComponentSystem
    {
        private const float BaseLife = 0.72f;
        private const float CritLife = 0.85f;
        private const float BaseUpSpeed = 120.0f;
        private const float CritUpSpeed = 165.0f;

        [EntitySystem]
        private static void Awake(this CombatTextComponent self)
        {
            GameObject root = new("CombatTextRuntime");
            UnityEngine.Object.DontDestroyOnLoad(root);

            self.RootGameObject = root;
            self.Canvas = root.AddComponent<Canvas>();
            self.Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            self.Canvas.sortingOrder = 30000;
            self.CanvasRect = self.Canvas.transform as RectTransform;
        }

        [EntitySystem]
        private static void Destroy(this CombatTextComponent self)
        {
            if (self.RootGameObject == null)
            {
                return;
            }

            UnityEngine.Object.Destroy(self.RootGameObject);
            self.RootGameObject = null;
        }

        [EntitySystem]
        private static void LateUpdate(this CombatTextComponent self)
        {
            if (self.ActiveNodes.Count == 0)
            {
                return;
            }

            float dt = Time.deltaTime;
            for (int i = self.ActiveNodes.Count - 1; i >= 0; --i)
            {
                CombatTextNodeData node = self.ActiveNodes[i];
                node.Elapsed += dt;

                float t = Mathf.Clamp01(node.Elapsed / node.Lifetime);
                node.RectTransform.anchoredPosition += node.Velocity * dt;
                node.Velocity += new Vector2(0f, 35f) * dt;

                Color color = node.Text.color;
                color.a = 1.0f - t;
                node.Text.color = color;

                float scale = Mathf.Lerp(node.StartScale, node.EndScale, t);
                node.RectTransform.localScale = Vector3.one * scale;

                if (node.Elapsed < node.Lifetime)
                {
                    self.ActiveNodes[i] = node;
                    continue;
                }

                node.GameObject.SetActive(false);
                self.ActiveNodes.RemoveAt(i);
                self.CachedNodes.Enqueue(node);
            }
        }

        public static void Show(this CombatTextComponent self, Vector3 worldPos, string content, Color color, float fontSize, bool critical)
        {
            Camera cam = Camera.main;
            if (cam == null || string.IsNullOrEmpty(content))
            {
                return;
            }

            Vector3 screenPoint = cam.WorldToScreenPoint(worldPos);
            if (screenPoint.z <= 0)
            {
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(self.CanvasRect, screenPoint, null, out Vector2 localPoint))
            {
                return;
            }

            CombatTextNodeData node = self.GetNode();
            node.Text.text = content;
            node.Text.color = color;
            node.Text.fontSize = fontSize;
            node.Lifetime = critical ? CritLife : BaseLife;
            node.Elapsed = 0;
            node.StartScale = critical ? 1.35f : 1.0f;
            node.EndScale = 1.0f;

            float randomX = Random.Range(-48f, 48f);
            float randomY = Random.Range(-10f, 18f);
            node.RectTransform.anchoredPosition = localPoint + new Vector2(randomX, randomY);

            float speedX = Random.Range(-26f, 26f);
            float speedY = critical ? CritUpSpeed : BaseUpSpeed;
            node.Velocity = new Vector2(speedX, speedY);

            node.RectTransform.localScale = Vector3.one * node.StartScale;
            node.GameObject.SetActive(true);
            self.ActiveNodes.Add(node);
        }

        private static CombatTextNodeData GetNode(this CombatTextComponent self)
        {
            if (self.CachedNodes.Count > 0)
            {
                return self.CachedNodes.Dequeue();
            }

            GameObject go = new("CombatTextNode");
            go.transform.SetParent(self.CanvasRect, false);

            RectTransform rectTransform = go.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(260f, 90f);

            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.enableAutoSizing = false;

            return new CombatTextNodeData
            {
                GameObject = go,
                RectTransform = rectTransform,
                Text = text
            };
        }
    }
}

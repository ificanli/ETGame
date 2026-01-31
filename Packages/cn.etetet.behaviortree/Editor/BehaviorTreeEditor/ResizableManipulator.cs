using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;

namespace ET
{
    public class ResizableManipulator : MouseManipulator
    {
        public const float DefaultResizeZone = 10f;

        private bool isResizing;
        private Vector2 startMousePosition;
        private Rect startRect;
        private float currentWidth;

        public float MinWidth { get; set; }
        public float MinHeight { get; set; }
        public float ResizeZone { get; set; } = DefaultResizeZone;
        public bool ResizeHeight { get; set; }

        public System.Action OnResizeStart { get; set; }
        public System.Action<float> OnResizing { get; set; }
        public System.Action<float> OnResizeEnd { get; set; }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            // 判断鼠标是否在节点的右下角区域，开始调整大小
            var node = target as VisualElement;
            if (IsInResizeZone(evt.localMousePosition, node.layout))
            {
                isResizing = true;
                startMousePosition = evt.mousePosition;
                startRect = node.layout;
                currentWidth = startRect.width;
                OnResizeStart?.Invoke();
                target.CaptureMouse();
                evt.StopPropagation();
            }
        }

        private void OnMouseMove(MouseMoveEvent evt)
        {
            if (isResizing)
            {
                var delta = evt.mousePosition - startMousePosition;
                float newWidth = Mathf.Max(MinWidth, startRect.width + delta.x);
                currentWidth = newWidth;

                target.style.width = newWidth;

                if (ResizeHeight)
                {
                    float newHeight = Mathf.Max(MinHeight, startRect.height + delta.y);
                    target.style.height = newHeight;
                }

                OnResizing?.Invoke(newWidth);

                evt.StopPropagation();
            }
        }

        private void OnMouseUp(MouseUpEvent evt)
        {
            if (isResizing)
            {
                isResizing = false;
                target.ReleaseMouse();
                OnResizeEnd?.Invoke(currentWidth);
                evt.StopPropagation();
            }
        }

        // 判断鼠标是否在节点的右下角（用于触发调整大小）
        private bool IsInResizeZone(Vector2 mousePosition, Rect layout)
        {
            return mousePosition.x >= layout.width - ResizeZone && mousePosition.y >= layout.height - ResizeZone;
        }
    }
}

using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;

namespace ET
{
    public class ResizableManipulator : MouseManipulator
    {
        private bool isResizing;
        private Vector2 startMousePosition;
        private Rect startRect;

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
                target.CaptureMouse();
                evt.StopPropagation();
            }
        }

        private void OnMouseMove(MouseMoveEvent evt)
        {
            if (isResizing)
            {
                var delta = evt.mousePosition - startMousePosition;
                var newWidth = Mathf.Max(100, startRect.width + delta.x); // 最小宽度限制
                var newHeight = Mathf.Max(50, startRect.height + delta.y); // 最小高度限制

                target.style.width = newWidth;
                target.style.height = newHeight;

                evt.StopPropagation();
            }
        }

        private void OnMouseUp(MouseUpEvent evt)
        {
            if (isResizing)
            {
                isResizing = false;
                target.ReleaseMouse();
                evt.StopPropagation();
            }
        }

        // 判断鼠标是否在节点的右下角（用于触发调整大小）
        private bool IsInResizeZone(Vector2 mousePosition, Rect layout)
        {
            const float resizeZone = 10f; // 调整大小的触发区域大小（边角10x10）
            return mousePosition.x >= layout.width - resizeZone && mousePosition.y >= layout.height - resizeZone;
        }
    }
}
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace ET
{
    public class BehaviorTreeEditor : EditorWindow
    {
        public TreeView TreeView;

        private Label text;
        private double messageEndTime;
        private VisualElement debugPanel;
        private VisualElement pathList;
        private VisualElement snapshotContent;
        private TextField entityIdFilterField;
        private bool isDebugPanelVisible = true;

        // 用于检测路径列表是否需要更新
        private int lastPathCount = 0;
        private int lastPathExecuteCount = 0;
        private string lastFilterEntityId = "";

        // 路径按钮缓存，避免频繁重建
        private List<Button> pathButtons = new List<Button>();

        // 可拖动分隔条
        private VisualElement splitter;
        private bool isDraggingSplitter;
        private float debugPanelWidth = 200f;
        private const float MinDebugPanelWidth = 150f;
        private const float MaxDebugPanelWidth = 500f;

        // 所有窗口实例列表
        private static List<BehaviorTreeEditor> instances = new List<BehaviorTreeEditor>();

        // 路径记录器实例
        public BTDebugPathRecorder PathRecorder { get; private set; }

        [MenuItem("ET/BehaviorTreeEditor _F8")]
        public static void ShowWindow()
        {
            if (BTNodeDrawer.ScriptableObject == null)
            {
                return;
            }
            
            // 创建新窗口实例（支持多个实例）
            BehaviorTreeEditor wnd = CreateInstance<BehaviorTreeEditor>();
            wnd.titleContent = new GUIContent("BehaviorTreeEditor");
            wnd.PathRecorder = new BTDebugPathRecorder();
            wnd.ShowUtility();
            wnd.TreeView.InitTree(wnd, BTNodeDrawer.ScriptableObject, BTNodeDrawer.OpenNode);

            // 添加到实例列表
            instances.Add(wnd);
        }

        /// <summary>
        /// 获取所有编辑器实例（供外部访问）
        /// </summary>
        public static List<BehaviorTreeEditor> GetAllInstances()
        {
            // 清理已销毁的实例
            instances.RemoveAll(editor => editor == null);
            return instances;
        }

        public void CreateGUI()
        {
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/cn.etetet.behaviortree/Editor/BehaviorTreeEditor/BehaviorTreeEditor.uxml");
            visualTree.CloneTree(root);

            this.TreeView = root.Q<TreeView>();

            // Bind toolbar button events
            Button saveButton = root.Q<Button>("SaveButton");
            if (saveButton != null)
            {
                saveButton.clicked += OnSaveButtonClicked;
            }

            Button exportButton = root.Q<Button>("ExportButton");
            if (exportButton != null)
            {
                exportButton.clicked += OnExportButtonClicked;
            }

            // Bind debug button
            Button debugButton = root.Q<Button>("DebugButton");
            if (debugButton != null)
            {
                debugButton.clicked += OnDebugButtonClicked;
            }

            // Get debug panel and path list
            this.debugPanel = root.Q<VisualElement>("DebugPanel");
            this.pathList = root.Q<VisualElement>("PathList");
            this.snapshotContent = root.Q<VisualElement>("SnapshotContent");
            this.entityIdFilterField = root.Q<TextField>("EntityIdFilterField");

            // Get splitter
            this.splitter = root.Q<VisualElement>("Splitter");

            // Setup splitter drag events
            if (this.splitter != null)
            {
                this.splitter.RegisterCallback<MouseDownEvent>(this.OnSplitterMouseDown);
                this.splitter.RegisterCallback<MouseMoveEvent>(this.OnSplitterMouseMove);
                this.splitter.RegisterCallback<MouseUpEvent>(this.OnSplitterMouseUp);
            }

            // Bind entityId filter field change event
            if (this.entityIdFilterField != null)
            {
                this.entityIdFilterField.RegisterValueChangedCallback(_ => { this.OnEntityIdFilterChanged(); });
            }

            // Set debug panel to be visible by default
            if (this.debugPanel != null)
            {
                this.debugPanel.style.display = DisplayStyle.Flex;
                this.debugPanel.style.width = this.debugPanelWidth;
                this.UpdatePathList();
            }

            // Set splitter to be visible by default
            if (this.splitter != null)
            {
                this.splitter.style.display = DisplayStyle.Flex;
            }

            // Bind clear paths button
            Button clearPathsButton = root.Q<Button>("ClearPathsButton");
            if (clearPathsButton != null)
            {
                clearPathsButton.clicked += OnClearPathsButtonClicked;
            }

            // Schedule Update for ShowText and PathList
            rootVisualElement.schedule.Execute((_) => { this.Update(); }).Every(30);
        }

        private void OnSaveButtonClicked()
        {
            this.TreeView?.Save();
        }

        private void OnExportButtonClicked()
        {
            Client.ExportScriptableObjectEditor.ExportScriptableObject();
            this.ShowText("Export Finish!");
        }

        private void OnDebugButtonClicked()
        {
            this.isDebugPanelVisible = !this.isDebugPanelVisible;

            if (this.debugPanel != null)
            {
                this.debugPanel.style.display = this.isDebugPanelVisible ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (this.splitter != null)
            {
                this.splitter.style.display = this.isDebugPanelVisible ? DisplayStyle.Flex : DisplayStyle.None;
            }

            // 如果打开面板，更新路径列表
            if (this.isDebugPanelVisible)
            {
                this.UpdatePathList();
            }
            else
            {
                // 关闭面板时，清除节点的高亮状态
                this.TreeView?.ClearHighlight();
            }
        }

        private void OnClearPathsButtonClicked()
        {
            this.PathRecorder.Clear();
            this.lastPathCount = 0;
            this.lastPathExecuteCount = 0;
            this.lastFilterEntityId = "";
            this.UpdatePathList();
            // 清空路径时，同时清除节点的高亮状态
            this.TreeView?.ClearHighlight();
            this.ShowText("Paths cleared!");
        }

        /// <summary>
        /// 处理分隔条鼠标按下事件
        /// </summary>
        private void OnSplitterMouseDown(MouseDownEvent evt)
        {
            if (evt.button == 0) // Left mouse button
            {
                this.isDraggingSplitter = true;
                this.splitter.CaptureMouse();
                evt.StopPropagation();
            }
        }

        /// <summary>
        /// 处理分隔条鼠标移动事件
        /// </summary>
        private void OnSplitterMouseMove(MouseMoveEvent evt)
        {
            if (this.isDraggingSplitter && this.debugPanel != null)
            {
                // 计算新的宽度
                float newWidth = evt.localMousePosition.x + this.debugPanel.resolvedStyle.width;

                // 限制宽度范围
                newWidth = Mathf.Clamp(newWidth, MinDebugPanelWidth, MaxDebugPanelWidth);

                // 应用新宽度
                this.debugPanelWidth = newWidth;
                this.debugPanel.style.width = newWidth;

                evt.StopPropagation();
            }
        }

        /// <summary>
        /// 处理分隔条鼠标释放事件
        /// </summary>
        private void OnSplitterMouseUp(MouseUpEvent evt)
        {
            if (evt.button == 0 && this.isDraggingSplitter)
            {
                this.isDraggingSplitter = false;
                this.splitter.ReleaseMouse();
                evt.StopPropagation();
            }
        }

        /// <summary>
        /// 当entityId过滤器改变时触发
        /// </summary>
        private void OnEntityIdFilterChanged()
        {
            string filterEntityIdStr = this.entityIdFilterField?.value?.Trim() ?? "";

            // 解析并设置过滤的EntityId
            if (!string.IsNullOrEmpty(filterEntityIdStr) && long.TryParse(filterEntityIdStr, out long filterEntityId))
            {
                this.PathRecorder.SetCurrentEntityId(filterEntityId);
            }
            else
            {
                // 输入为空或无效，设置为0表示记录所有
                this.PathRecorder.SetCurrentEntityId(0);
            }

            // 清空现有记录并更新列表
            this.PathRecorder.Clear();
            this.lastPathCount = 0;
            this.lastPathExecuteCount = 0;
            this.UpdatePathList();
        }

        /// <summary>
        /// 更新路径列表
        /// </summary>
        private void UpdatePathList()
        {
            if (this.pathList == null)
            {
                return;
            }

            // 清空现有列表
            this.pathList.Clear();
            this.pathButtons.Clear();

            // 获取当前打开的行为树ID
            long currentTreeId = this.TreeView?.GetCurrentTreeId() ?? 0;
            if (currentTreeId == 0)
            {
                Label emptyLabel = new Label("No behavior tree opened");
                emptyLabel.style.color = new StyleColor(Color.gray);
                emptyLabel.style.paddingTop = 10;
                emptyLabel.style.paddingLeft = 5;
                this.pathList.Add(emptyLabel);
                return;
            }

            // 获取当前行为树的路径（已在记录阶段过滤）
            List<BTDebugPathInfo> currentPaths = this.PathRecorder.GetAllPaths();

            if (currentPaths.Count == 0)
            {
                string filterEntityIdStr = this.entityIdFilterField?.value?.Trim() ?? "";
                string emptyMessage = !string.IsNullOrEmpty(filterEntityIdStr)?
                    $"No paths for EntityId {filterEntityIdStr}" :
                    "No paths recorded";
                Label emptyLabel = new Label(emptyMessage);
                emptyLabel.style.color = new StyleColor(Color.gray);
                emptyLabel.style.paddingTop = 10;
                emptyLabel.style.paddingLeft = 5;
                this.pathList.Add(emptyLabel);
                return;
            }

            // 倒序显示，最新的在最前面
            for (int i = currentPaths.Count - 1; i >= 0; i--)
            {
                BTDebugPathInfo pathInfo = currentPaths[i];
                int index = i;

                Button pathButton = new Button(() => OnPathButtonClicked(pathInfo));
                string countText = pathInfo.Count > 1 ? $" x{pathInfo.Count}" : "";
                pathButton.text = $"Path {index + 1} [Entity:{pathInfo.EntityId}] [{pathInfo.RecordTime:HH:mm:ss}]{countText}";
                pathButton.style.marginBottom = 2;
                pathButton.style.backgroundColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f));

                this.pathList.Add(pathButton);
                this.pathButtons.Add(pathButton);
            }
        }

        /// <summary>
        /// 只更新路径按钮的文本（不重建按钮，避免点击失效）
        /// </summary>
        private void UpdatePathButtonTexts(List<BTDebugPathInfo> currentPaths)
        {
            if (this.pathButtons.Count != currentPaths.Count)
            {
                // 数量不匹配，完全重建
                this.UpdatePathList();
                return;
            }

            // 倒序更新文本
            for (int i = 0; i < currentPaths.Count; i++)
            {
                int pathIndex = currentPaths.Count - 1 - i;
                BTDebugPathInfo pathInfo = currentPaths[pathIndex];
                Button button = this.pathButtons[i];

                string countText = pathInfo.Count > 1 ? $" x{pathInfo.Count}" : "";
                button.text = $"Path {pathIndex + 1} [Entity:{pathInfo.EntityId}] [{pathInfo.RecordTime:HH:mm:ss}]{countText}";
            }
        }

        /// <summary>
        /// 点击路径按钮时触发
        /// </summary>
        private void OnPathButtonClicked(BTDebugPathInfo pathInfo)
        {
            if (this.TreeView != null)
            {
                this.TreeView.HighlightPath(pathInfo.Path);
                this.ShowText($"Highlighted {pathInfo.Path.Count} nodes");
            }

            // 显示快照内容
            this.UpdateSnapshotView(pathInfo);

            // 打印路径信息
            string pathStr = string.Join(" -> ", pathInfo.Path);
            Log.Debug($"Path clicked: EntityId={pathInfo.EntityId}, TreeId={pathInfo.TreeId}, Count={pathInfo.Count}, Time={pathInfo.RecordTime:HH:mm:ss}, Path=[{pathStr}]");
        }

        /// <summary>
        /// 更新快照视图
        /// </summary>
        private void UpdateSnapshotView(BTDebugPathInfo pathInfo)
        {
            if (this.snapshotContent == null)
            {
                return;
            }

            // 清空现有内容
            this.snapshotContent.Clear();

            // 获取快照字符串（延迟转换）
            string snapshotString = pathInfo.Snapshot == null? "" : pathInfo.Snapshot.ToString();
            if (string.IsNullOrEmpty(snapshotString))
            {
                Label emptyLabel = new Label("No snapshot data");
                emptyLabel.style.color = new StyleColor(Color.gray);
                emptyLabel.style.paddingTop = 5;
                this.snapshotContent.Add(emptyLabel);
                return;
            }

            // 显示快照文本
            Label snapshotLabel = new Label(snapshotString);
            snapshotLabel.style.color = new StyleColor(Color.white);
            snapshotLabel.style.fontSize = 11;
            snapshotLabel.style.whiteSpace = WhiteSpace.Normal;
            snapshotLabel.style.paddingTop = 5;
            this.snapshotContent.Add(snapshotLabel);
        }

        /// <summary>
        /// 显示提示信息
        /// </summary>
        /// <param name="message">提示信息内容</param>
        /// <param name="timeout">显示时长（秒）</param>
        public void ShowText(string message, float timeout = 1)
        {
            this.text = this.TreeView?.Q<Label>("LabelTips");
            if (this.text == null)
            {
                return;
            }

            this.messageEndTime = EditorApplication.timeSinceStartup + timeout;
            this.text.style.opacity = 1f;
            this.text.text = message;
        }

        private void Update()
        {
            // 如果当前时间超过 _messageEndTime，就隐藏提示
            if (EditorApplication.timeSinceStartup > this.messageEndTime)
            {
                if (this.text != null)
                {
                    this.text.style.opacity = 0f;
                    this.text = null;
                }
            }

            // 如果调试面板打开，检查路径列表是否需要更新
            if (this.isDebugPanelVisible)
            {
                this.CheckAndUpdatePathList();
            }
        }

        /// <summary>
        /// 检查并更新路径列表（如果有变化）
        /// </summary>
        private void CheckAndUpdatePathList()
        {
            long currentTreeId = this.TreeView?.GetCurrentTreeId() ?? 0;
            if (currentTreeId == 0)
            {
                return;
            }

            // 获取entityId过滤器的值
            string filterEntityIdStr = this.entityIdFilterField?.value?.Trim() ?? "";

            // 如果过滤器变化了，完全重建列表
            if (filterEntityIdStr != this.lastFilterEntityId)
            {
                this.lastFilterEntityId = filterEntityIdStr;
                this.UpdatePathList();
                return;
            }

            // 获取当前行为树的路径（已在记录阶段过滤）
            List<BTDebugPathInfo> currentPaths = this.PathRecorder.GetAllPaths();

            // 检查路径数量是否变化 - 需要完全重建列表
            if (currentPaths.Count != this.lastPathCount)
            {
                this.lastPathCount = currentPaths.Count;
                this.lastPathExecuteCount = currentPaths.Count > 0 ? currentPaths[currentPaths.Count - 1].Count : 0;
                this.UpdatePathList();
                return;
            }

            // 检查最新路径的执行次数是否变化 - 只需要更新文本
            if (currentPaths.Count > 0)
            {
                int currentExecuteCount = currentPaths[currentPaths.Count - 1].Count;
                if (currentExecuteCount != this.lastPathExecuteCount)
                {
                    this.lastPathExecuteCount = currentExecuteCount;
                    this.UpdatePathButtonTexts(currentPaths);
                }
            }
        }

        public void OnDestroy()
        {
            this.TreeView.Save();

            // 显式清理TreeView资源，防止内存泄漏
            this.TreeView?.Cleanup();

            // 从实例列表中移除
            instances.Remove(this);

            // 如果所有实例都关闭了，清理BTClipboard
            if (instances.Count == 0)
            {
                BTClipboard.Destroy();
            }
        }
    }
}
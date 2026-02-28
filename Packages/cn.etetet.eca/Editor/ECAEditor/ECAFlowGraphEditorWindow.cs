using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using ET;

namespace ET.Client
{
    public class ECAFlowGraphEditorWindow : EditorWindow
    {
        private ECAFlowGraphView graphView;
        private FlowGraphAsset graphAsset;

        [MenuItem("ET/ECA/Flow Graph Editor")]
        public static void Open()
        {
            ECAFlowGraphEditorWindow window = GetWindow<ECAFlowGraphEditorWindow>();
            window.titleContent = new GUIContent("ECA Flow Graph");
        }

        private void OnEnable()
        {
            ConstructGraphView();
            ConstructToolbar();
        }

        private void OnDisable()
        {
            if (graphView != null)
            {
                rootVisualElement.Remove(graphView);
            }
        }

        private void ConstructGraphView()
        {
            graphView = new ECAFlowGraphView(this)
            {
                name = "ECA Flow Graph"
            };
            graphView.StretchToParentSize();
            rootVisualElement.Add(graphView);
        }

        private void ConstructToolbar()
        {
            Toolbar toolbar = new Toolbar();

            ObjectField assetField = new ObjectField("Graph Asset")
            {
                objectType = typeof(FlowGraphAsset),
                allowSceneObjects = false
            };
            assetField.RegisterValueChangedCallback(evt =>
            {
                graphAsset = evt.newValue as FlowGraphAsset;
                graphView.BindGraphAsset(graphAsset);
            });
            toolbar.Add(assetField);

            toolbar.Add(new Button(() => graphView.LoadGraph()) { text = "Load" });
            toolbar.Add(new Button(() => graphView.SaveGraph()) { text = "Save" });

            toolbar.Add(new ToolbarSpacer());

            toolbar.Add(new Button(() => graphView.CreateNode(ECAFlowNodeType.Event)) { text = "Add Event" });
            toolbar.Add(new Button(() => graphView.CreateNode(ECAFlowNodeType.Condition)) { text = "Add Condition" });
            toolbar.Add(new Button(() => graphView.CreateNode(ECAFlowNodeType.Action)) { text = "Add Action" });
            toolbar.Add(new Button(() => graphView.CreateNode(ECAFlowNodeType.State)) { text = "Add State" });

            rootVisualElement.Add(toolbar);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using ET;

namespace ET.Client
{
    public class ECAFlowNodeView : Node
    {
        private const string EmptyKeyLabel = "<None>";

        public int NodeId { get; }
        public string NodeType { get; }
        public string NodeKey { get; private set; }
        public Port InputPort { get; }

        private readonly List<Port> outputPorts = new();
        private const string ParamState = "state";
        private string paramsText = string.Empty;
        private TextField paramsField;

        public Vector2 DefaultSize => new Vector2(260f, 200f);

        public ECAFlowNodeView(string nodeType, int nodeId)
        {
            NodeType = nodeType;
            NodeId = nodeId;
            NodeKey = string.Empty;
            title = $"{nodeType} {nodeId}";

            InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            InputPort.portName = "In";
            inputContainer.Add(InputPort);

            CreateOutputPorts();
            CreateKeyField();
            CreateParamsField();

            RefreshPorts();
            RefreshExpandedState();
        }

        public ECAFlowNodeView(FlowNodeData data)
        {
            NodeType = data.NodeType;
            NodeId = data.Id;
            NodeKey = data.NodeKey;
            paramsText = BuildParamsText(data.Params);
            title = string.IsNullOrEmpty(data.Title) ? $"{data.NodeType} {data.Id}" : data.Title;

            InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            InputPort.portName = "In";
            inputContainer.Add(InputPort);

            CreateOutputPorts();
            CreateKeyField();
            CreateParamsField();

            SetPosition(new Rect(new Vector2(data.PosX, data.PosY), DefaultSize));

            RefreshPorts();
            RefreshExpandedState();
        }

        private void CreateKeyField()
        {
            if (TryBuildKeyOptions(out List<string> options))
            {
                string displayValue = string.IsNullOrEmpty(NodeKey) ? EmptyKeyLabel : NodeKey;
                if (!options.Contains(displayValue))
                {
                    options.Insert(1, displayValue);
                }

                PopupField<string> keyField = new PopupField<string>("Key", options, displayValue);
                keyField.RegisterValueChangedCallback(evt =>
                {
                    NodeKey = evt.newValue == EmptyKeyLabel ? string.Empty : evt.newValue;
                    ApplyParamTemplate();
                });
                extensionContainer.Add(keyField);
                return;
            }

            TextField fallbackField = new TextField("Key")
            {
                value = NodeKey
            };
            fallbackField.RegisterValueChangedCallback(evt =>
            {
                NodeKey = evt.newValue;
                ApplyParamTemplate();
            });
            extensionContainer.Add(fallbackField);
        }

        private void CreateParamsField()
        {
            paramsField = new TextField("Params")
            {
                value = paramsText,
                multiline = true
            };
            paramsField.style.minHeight = 60f;
            paramsField.RegisterValueChangedCallback(evt => paramsText = evt.newValue);
            extensionContainer.Add(paramsField);
        }

        private void CreateOutputPorts()
        {
            outputPorts.Clear();
            outputContainer.Clear();

            if (NodeType == ECAFlowNodeType.Condition)
            {
                AddOutputPort("True");
                AddOutputPort("False");
            }
            else
            {
                AddOutputPort("Out");
            }
        }

        private void AddOutputPort(string name)
        {
            Port port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            port.portName = name;
            outputContainer.Add(port);
            outputPorts.Add(port);
        }

        public FlowNodeData ToData()
        {
            Rect rect = GetPosition();
            return new FlowNodeData
            {
                Id = NodeId,
                NodeType = NodeType,
                NodeKey = NodeKey,
                Title = title,
                PosX = rect.x,
                PosY = rect.y,
                Params = ParseParamsText(paramsText)
            };
        }

        public Port GetOutputPort(string branch)
        {
            string portName = branch;
            if (string.IsNullOrEmpty(portName))
            {
                portName = NodeType == ECAFlowNodeType.Condition ? "True" : "Out";
            }

            return outputPorts.FirstOrDefault(p => p.portName == portName);
        }

        private static string BuildParamsText(List<FlowParam> list)
        {
            if (list == null || list.Count == 0)
            {
                return string.Empty;
            }

            return string.Join("\n", list.Where(p => p != null).Select(p => $"{p.Key}={p.Value}"));
        }

        private static List<FlowParam> ParseParamsText(string text)
        {
            List<FlowParam> result = new();
            if (string.IsNullOrWhiteSpace(text))
            {
                return result;
            }

            string[] lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string raw in lines)
            {
                string line = raw.Trim();
                if (line.Length == 0)
                {
                    continue;
                }

                int index = line.IndexOf('=');
                string key;
                string value;
                if (index < 0)
                {
                    key = line;
                    value = string.Empty;
                }
                else
                {
                    key = line.Substring(0, index).Trim();
                    value = line.Substring(index + 1).Trim();
                }

                result.Add(new FlowParam { Key = key, Value = value });
            }

            return result;
        }

        private void ApplyParamTemplate()
        {
            string template = GetParamTemplate(NodeType, NodeKey);
            paramsText = template;
            if (paramsField != null)
            {
                paramsField.SetValueWithoutNotify(paramsText);
            }
        }

        private static string GetParamTemplate(string nodeType, string nodeKey)
        {
            if (string.IsNullOrEmpty(nodeKey))
            {
                return string.Empty;
            }

            if (nodeType == ECAFlowNodeType.Event)
            {
                return GetEventParamTemplate(nodeKey);
            }

            if (nodeType == ECAFlowNodeType.Condition)
            {
                return GetConditionParamTemplate(nodeKey);
            }

            if (nodeType == ECAFlowNodeType.Action)
            {
                return GetActionParamTemplate(nodeKey);
            }

            return string.Empty;
        }

        private static string GetEventParamTemplate(string nodeKey)
        {
            switch (nodeKey)
            {
                case ECAFlowEventType.OnTimerElapsed:
                    return "timer_id=";
                case ECAFlowEventType.OnCombatTimeReached:
                    return "seconds=";
                case ECAFlowEventType.OnTaskComplete:
                    return "task_id=";
                case ECAFlowEventType.OnSwitchPulled:
                    return "switch_id=";
                case ECAFlowEventType.OnTargetKilled:
                    return "target_id=\ngroup_id=";
                case ECAFlowEventType.OnAreaHoldCompleted:
                    return "area_id=";
                case ECAFlowEventType.OnEscortArrived:
                    return "escort_id=";
                default:
                    return string.Empty;
            }
        }

        private static string GetConditionParamTemplate(string nodeKey)
        {
            switch (nodeKey)
            {
                case ECAFlowConditionKey.PointStateEquals:
                    return $"{ParamState}=";
                case ECAFlowConditionKey.BackpackWeightLE:
                    return "max_weight=";
                case ECAFlowConditionKey.KillCountGE:
                    return "min_kill=";
                case ECAFlowConditionKey.TaskState:
                    return "task_id=\nstate=";
                case ECAFlowConditionKey.PointTypeEquals:
                    return "point_type=";
                default:
                    return string.Empty;
            }
        }

        private static string GetActionParamTemplate(string nodeKey)
        {
            switch (nodeKey)
            {
                case ECAFlowActionKey.SetPointState:
                    return $"{ParamState}=";
                case ECAFlowActionKey.SetPointActive:
                    return "active=";
                case ECAFlowActionKey.ShowInteractButton:
                case ECAFlowActionKey.HideInteractButton:
                    return "button_id=";
                case ECAFlowActionKey.StartSearchTimer:
                    return "seconds=\ntimer_id=";
                case ECAFlowActionKey.ShowSearchUI:
                case ECAFlowActionKey.OpenContainerUI:
                case ECAFlowActionKey.ShowEvacUI:
                case ECAFlowActionKey.ShowTaskAcceptUI:
                    return "ui_id=";
                case ECAFlowActionKey.SpawnItemsToGround:
                    return "loot_table=\ncount=\nradius=";
                case ECAFlowActionKey.PlayOpenAnim:
                    return "anim_id=";
                case ECAFlowActionKey.StartEvacCountdown:
                    return "seconds=\nreason=\ntimer_id=";
                case ECAFlowActionKey.TransferToLobby:
                    return "map_name=";
                case ECAFlowActionKey.SpawnMonsters:
                    return "group_id=\ncount=";
                case ECAFlowActionKey.BroadcastMap:
                    return "message_id=";
                case ECAFlowActionKey.StartTask:
                    return "task_id=";
                case ECAFlowActionKey.GiveReward:
                    return "reward_id=";
                case ECAFlowActionKey.StartAreaHold:
                    return "area_id=\nseconds=";
                case ECAFlowActionKey.StartEscort:
                    return "route_id=\ntarget_id=";
                case ECAFlowActionKey.StartZipline:
                    return "zipline_id=";
                case ECAFlowActionKey.ApplyStealth:
                    return "seconds=";
                default:
                    return string.Empty;
            }
        }

        private bool TryBuildKeyOptions(out List<string> options)
        {
            options = null;
            Type sourceType = null;
            if (NodeType == ECAFlowNodeType.Event)
            {
                sourceType = typeof(ECAFlowEventType);
            }
            else if (NodeType == ECAFlowNodeType.Condition)
            {
                sourceType = typeof(ECAFlowConditionKey);
            }
            else if (NodeType == ECAFlowNodeType.Action)
            {
                sourceType = typeof(ECAFlowActionKey);
            }

            if (sourceType == null)
            {
                return false;
            }

            options = new List<string> { EmptyKeyLabel };
            options.AddRange(GetConstStrings(sourceType));
            return true;
        }

        private static IEnumerable<string> GetConstStrings(Type type)
        {
            return type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(field => field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string))
                .Select(field => (string)field.GetRawConstantValue())
                .OrderBy(value => value);
        }
    }
}

using System;
using System.Globalization;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace ET.Client
{
    [EnableClass]
    public class ECAPointMarker : SerializedMonoBehaviour
    {
        private const string PointParamTemplatePrefix = "point_param_template__";

        [Header("基础配置")]
        public string ConfigId;
        [ValueDropdown(nameof(GetPointTypeOptions))]
        [OnValueChanged(nameof(OnTypeChanged))]
        public int Type = ECAPointType.EvacuationPoint;
        public List<FlowParam> Params = new();
        [HideInInspector]
        public List<string> FlowGraphTemplateKeys = new();
        [HideInInspector]
        [FormerlySerializedAs("Config")]
        public ECAConfigAsset LegacyConfig;

        [Header("流程图")]
        public FlowGraphAsset FlowGraph;

        [Header("可视化")]
        public bool ShowRange = true;
        public Color RangeColor = Color.green;

        private void OnValidate()
        {
            this.MigrateLegacyConfigIfNeeded();
            this.ApplyTypeParamTemplate();
        }

        private void OnDrawGizmos()
        {
            if (ShowRange)
            {
                Gizmos.color = RangeColor;
                Gizmos.DrawWireSphere(transform.position, this.GetInteractRangeForGizmos());
            }
        }

        public ECAConfig GetConfig()
        {
            this.MigrateLegacyConfigIfNeeded();
            this.ApplyTypeParamTemplate();

            if (string.IsNullOrWhiteSpace(this.ConfigId))
            {
                Debug.LogError($"ECAPointMarker at {transform.position} has empty ConfigId!");
                return null;
            }

            Vector3 worldPos = this.transform.position;
            ECAConfig config = new()
            {
                ConfigId = this.ConfigId,
                Type = this.Type,
                PosX = worldPos.x,
                PosY = worldPos.y,
                PosZ = worldPos.z,
                Params = CopyParams(this.Params),
                FlowGraph = this.FlowGraph != null ? this.FlowGraph.Graph : null
            };

            config.SyncLegacyFieldsForCompatibility();
            return config;
        }

        private float GetInteractRangeForGizmos()
        {
            if (FlowParamHelper.TryGetFloatParam(this.Params, ECAPointParamKey.InteractRange, out float range) && range > 0f)
            {
                return range;
            }

            return ECAConfig.DefaultInteractRange;
        }

        private static List<FlowParam> CopyParams(List<FlowParam> source)
        {
            List<FlowParam> copy = new();
            if (source == null || source.Count == 0)
            {
                return copy;
            }

            foreach (FlowParam param in source)
            {
                if (param == null || string.IsNullOrWhiteSpace(param.Key))
                {
                    continue;
                }

                copy.Add(new FlowParam
                {
                    Key = param.Key,
                    Value = param.Value
                });
            }

            return copy;
        }

        private void OnTypeChanged()
        {
            this.ApplyTypeParamTemplate();
        }

        private void ApplyTypeParamTemplate()
        {
            this.Params ??= new List<FlowParam>();
            this.FlowGraphTemplateKeys ??= new List<string>();

            HashSet<string> templateKeys = new(this.GetTemplateParamKeys());

            Dictionary<string, string> templateValues = new();
            foreach (FlowParam param in this.Params)
            {
                if (param == null || string.IsNullOrWhiteSpace(param.Key))
                {
                    continue;
                }

                if (templateKeys.Contains(param.Key))
                {
                    templateValues[param.Key] = param.Value;
                }
            }

            foreach (string templateKey in templateKeys)
            {
                this.RemoveTemplateParam(templateKey);
            }

            List<FlowParam> flowGraphTemplate = this.GetFlowGraphParamTemplate();
            this.FlowGraphTemplateKeys.Clear();
            foreach (FlowParam templateParam in flowGraphTemplate)
            {
                if (templateParam == null || string.IsNullOrWhiteSpace(templateParam.Key) || this.FlowGraphTemplateKeys.Contains(templateParam.Key))
                {
                    continue;
                }

                this.FlowGraphTemplateKeys.Add(templateParam.Key);
            }

            foreach (FlowParam templateParam in this.GetTypeParamTemplate(this.Type))
            {
                this.ApplyTemplateParam(templateValues, templateParam);
            }

            foreach (FlowParam templateParam in flowGraphTemplate)
            {
                this.ApplyTemplateParam(templateValues, templateParam);
            }
        }

        private void ApplyTemplateParam(Dictionary<string, string> templateValues, FlowParam templateParam)
        {
            if (templateParam == null || string.IsNullOrWhiteSpace(templateParam.Key))
            {
                return;
            }

            string value = templateParam.Value;
            if (templateValues.TryGetValue(templateParam.Key, out string oldValue) && !string.IsNullOrWhiteSpace(oldValue))
            {
                value = oldValue;
            }

            this.SetOrAddParam(templateParam.Key, value);
        }

        private List<FlowParam> GetTypeParamTemplate(int pointType)
        {
            List<FlowParam> template = new()
            {
                new FlowParam
                {
                    Key = ECAPointParamKey.InteractRange,
                    Value = ECAConfig.DefaultInteractRange.ToString(CultureInfo.InvariantCulture)
                },
                new FlowParam
                {
                    Key = ECAPointParamKey.CheckRangeIntervalMs,
                    Value = "0"
                },
                new FlowParam
                {
                    Key = ECAPointParamKey.NavBlockEnabled,
                    Value = "0"
                },
                new FlowParam
                {
                    Key = ECAPointParamKey.NavBlockHalfExtentsX,
                    Value = ECAConfig.DefaultNavBlockHalfExtentsX.ToString(CultureInfo.InvariantCulture)
                },
                new FlowParam
                {
                    Key = ECAPointParamKey.NavBlockHalfExtentsY,
                    Value = ECAConfig.DefaultNavBlockHalfExtentsY.ToString(CultureInfo.InvariantCulture)
                },
                new FlowParam
                {
                    Key = ECAPointParamKey.NavBlockHalfExtentsZ,
                    Value = ECAConfig.DefaultNavBlockHalfExtentsZ.ToString(CultureInfo.InvariantCulture)
                },
                new FlowParam
                {
                    Key = ECAPointParamKey.NavBlockStates,
                    Value = string.Empty
                }
            };

            if (pointType == ECAPointType.EvacuationPoint)
            {
                template.Add(new FlowParam
                {
                    Key = ECAPointParamKey.EvacuationDurationMs,
                    Value = ECAConfig.DefaultEvacuationDurationMs.ToString(CultureInfo.InvariantCulture)
                });
                template.Add(new FlowParam
                {
                    Key = ECAPointParamKey.LobbyMapName,
                    Value = ECAConfig.DefaultLobbyMapName
                });
                template.Add(new FlowParam
                {
                    Key = ECAPointParamKey.SideId,
                    Value = "0"
                });
                template.Add(new FlowParam
                {
                    Key = ECAPointParamKey.MapPoiVisible,
                    Value = string.Empty
                });
                template.Add(new FlowParam
                {
                    Key = ECAPointParamKey.MapPoiShowMinimap,
                    Value = string.Empty
                });
                template.Add(new FlowParam
                {
                    Key = ECAPointParamKey.MapPoiShowWorldmap,
                    Value = string.Empty
                });
                template.Add(new FlowParam
                {
                    Key = ECAPointParamKey.MapPoiIcon,
                    Value = string.Empty
                });
                template.Add(new FlowParam
                {
                    Key = ECAPointParamKey.MapPoiTipTextId,
                    Value = string.Empty
                });
            }

            if (pointType == ECAPointType.SpawnPoint)
            {
                template.Add(new FlowParam
                {
                    Key = ECAPointParamKey.TeamId,
                    Value = "0"
                });
                template.Add(new FlowParam
                {
                    Key = ECAPointParamKey.SideId,
                    Value = "0"
                });
            }

            if (pointType == ECAPointType.Container)
            {
                template.Add(new FlowParam
                {
                    Key = ECAPointParamKey.ContainerProfile,
                    Value = string.Empty
                });
                template.Add(new FlowParam
                {
                    Key = ECAPointParamKey.MapPoiVisible,
                    Value = string.Empty
                });
                template.Add(new FlowParam
                {
                    Key = ECAPointParamKey.MapPoiShowMinimap,
                    Value = string.Empty
                });
                template.Add(new FlowParam
                {
                    Key = ECAPointParamKey.MapPoiShowWorldmap,
                    Value = string.Empty
                });
                template.Add(new FlowParam
                {
                    Key = ECAPointParamKey.MapPoiIcon,
                    Value = string.Empty
                });
                template.Add(new FlowParam
                {
                    Key = ECAPointParamKey.MapPoiTipTextId,
                    Value = string.Empty
                });
            }

            if (pointType == ECAPointType.MonsterSpawnPoint)
            {
                template.Add(new FlowParam
                {
                    Key = ECAPointParamKey.SpawnProfile,
                    Value = string.Empty
                });
                template.Add(new FlowParam
                {
                    Key = ECAPointParamKey.MapPoiVisible,
                    Value = string.Empty
                });
                template.Add(new FlowParam
                {
                    Key = ECAPointParamKey.MapPoiShowMinimap,
                    Value = string.Empty
                });
                template.Add(new FlowParam
                {
                    Key = ECAPointParamKey.MapPoiShowWorldmap,
                    Value = string.Empty
                });
                template.Add(new FlowParam
                {
                    Key = ECAPointParamKey.MapPoiIcon,
                    Value = string.Empty
                });
                template.Add(new FlowParam
                {
                    Key = ECAPointParamKey.MapPoiTipTextId,
                    Value = string.Empty
                });
            }

            if (pointType == ECAPointType.Door)
            {
                template.Add(new FlowParam
                {
                    Key = ECAPointParamKey.NavBlockEnabled,
                    Value = "1"
                });
                template.Add(new FlowParam
                {
                    Key = ECAPointParamKey.NavBlockStates,
                    Value = ECADoorState.Closed.ToString(CultureInfo.InvariantCulture)
                });
                template.Add(new FlowParam
                {
                    Key = ECADoorParamKey.ClosedButtonTextId,
                    Value = "0"
                });
                template.Add(new FlowParam
                {
                    Key = ECADoorParamKey.OpenedButtonTextId,
                    Value = "0"
                });
            }

            if (pointType == ECAPointType.KeyDoor)
            {
                template.Add(new FlowParam
                {
                    Key = ECAPointParamKey.NavBlockEnabled,
                    Value = "1"
                });
                template.Add(new FlowParam
                {
                    Key = ECAPointParamKey.NavBlockStates,
                    Value = $"{ECADoorState.Locked.ToString(CultureInfo.InvariantCulture)},{ECADoorState.Closed.ToString(CultureInfo.InvariantCulture)}"
                });
                template.Add(new FlowParam
                {
                    Key = ECADoorParamKey.RequiredKeyItemId,
                    Value = "0"
                });
                template.Add(new FlowParam
                {
                    Key = ECADoorParamKey.LockedButtonTextId,
                    Value = "0"
                });
                template.Add(new FlowParam
                {
                    Key = ECADoorParamKey.ClosedButtonTextId,
                    Value = "0"
                });
                template.Add(new FlowParam
                {
                    Key = ECADoorParamKey.OpenedButtonTextId,
                    Value = "0"
                });
                template.Add(new FlowParam
                {
                    Key = ECADoorParamKey.ConsumeKeyCount,
                    Value = "1"
                });
            }

            return template;
        }

        private List<FlowParam> GetFlowGraphParamTemplate()
        {
            List<FlowParam> template = new();
            List<FlowNodeData> nodes = this.FlowGraph?.Graph?.Nodes;
            if (nodes == null || nodes.Count == 0)
            {
                return template;
            }

            bool containsStartTask = false;
            foreach (FlowNodeData node in nodes)
            {
                if (node == null ||
                    !string.Equals(node.NodeType, ECAFlowNodeType.Action, StringComparison.Ordinal) ||
                    !string.Equals(node.NodeKey, ECAFlowActionKey.StartTask, StringComparison.Ordinal))
                {
                    continue;
                }

                containsStartTask = true;
                break;
            }

            if (!containsStartTask)
            {
                return template;
            }

            foreach (FlowNodeData node in nodes)
            {
                if (node?.Params == null)
                {
                    continue;
                }

                foreach (FlowParam param in node.Params)
                {
                    if (param == null ||
                        string.IsNullOrWhiteSpace(param.Key) ||
                        !param.Key.StartsWith(PointParamTemplatePrefix, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    string key = param.Key.Substring(PointParamTemplatePrefix.Length);
                    if (string.IsNullOrWhiteSpace(key))
                    {
                        continue;
                    }

                    SetOrAddTemplateParam(template, key, param.Value);
                }
            }

            return template;
        }

        private static void SetOrAddTemplateParam(List<FlowParam> template, string key, string value)
        {
            foreach (FlowParam param in template)
            {
                if (param == null || param.Key != key)
                {
                    continue;
                }

                param.Value = value;
                return;
            }

            template.Add(new FlowParam
            {
                Key = key,
                Value = value
            });
        }

        private static IEnumerable<ValueDropdownItem<int>> GetPointTypeOptions()
        {
            return new List<ValueDropdownItem<int>>
            {
                new("EvacuationPoint(撤离点)", ECAPointType.EvacuationPoint),
                new("SpawnPoint(出生点)", ECAPointType.SpawnPoint),
                new("Container(容器)", ECAPointType.Container),
                new("MonsterSpawnPoint(刷怪点)", ECAPointType.MonsterSpawnPoint),
                new("RangeTrigger(通用范围交互)", ECAPointType.RangeTrigger),
                new("Door(门)", ECAPointType.Door),
                new("KeyDoor(钥匙门)", ECAPointType.KeyDoor),
            };
        }

        private IEnumerable<string> GetTemplateParamKeys()
        {
            yield return ECAPointParamKey.InteractRange;
            yield return ECAPointParamKey.CheckRangeIntervalMs;
            yield return ECAPointParamKey.EvacuationDurationMs;
            yield return ECAPointParamKey.LobbyMapName;
            yield return ECAPointParamKey.NavBlockEnabled;
            yield return ECAPointParamKey.NavBlockHalfExtentsX;
            yield return ECAPointParamKey.NavBlockHalfExtentsY;
            yield return ECAPointParamKey.NavBlockHalfExtentsZ;
            yield return ECAPointParamKey.NavBlockStates;
            yield return ECAPointParamKey.TeamId;
            yield return ECAPointParamKey.SideId;
            yield return ECAPointParamKey.MapPoiVisible;
            yield return ECAPointParamKey.MapPoiShowMinimap;
            yield return ECAPointParamKey.MapPoiShowWorldmap;
            yield return ECAPointParamKey.MapPoiIcon;
            yield return ECAPointParamKey.MapPoiTipTextId;
            yield return ECAPointParamKey.ContainerProfile;
            yield return ECAPointParamKey.SpawnProfile;
            yield return ECADoorParamKey.RequiredKeyItemId;
            yield return ECADoorParamKey.LockedButtonTextId;
            yield return ECADoorParamKey.ClosedButtonTextId;
            yield return ECADoorParamKey.OpenedButtonTextId;
            yield return ECADoorParamKey.ConsumeKeyCount;

            if (this.FlowGraphTemplateKeys == null)
            {
                yield break;
            }

            foreach (string templateKey in this.FlowGraphTemplateKeys)
            {
                if (!string.IsNullOrWhiteSpace(templateKey))
                {
                    yield return templateKey;
                }
            }
        }

        private void MigrateLegacyConfigIfNeeded()
        {
            if (this.LegacyConfig == null)
            {
                return;
            }

            bool isNewConfigEmpty = string.IsNullOrWhiteSpace(this.ConfigId) &&
                                    (this.Params == null || this.Params.Count == 0);
            if (!isNewConfigEmpty)
            {
                return;
            }

            this.ConfigId = this.LegacyConfig.ConfigId;
            this.Type = this.LegacyConfig.PointType;

            this.Params ??= new List<FlowParam>();
            this.SetOrAddParam(ECAPointParamKey.InteractRange, this.LegacyConfig.InteractRange.ToString(CultureInfo.InvariantCulture));
            if (this.LegacyConfig.TeamId != 0)
            {
                this.SetOrAddParam(ECAPointParamKey.TeamId, this.LegacyConfig.TeamId.ToString(CultureInfo.InvariantCulture));
            }
        }

        private void SetOrAddParam(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            foreach (FlowParam param in this.Params)
            {
                if (param == null || param.Key != key)
                {
                    continue;
                }

                param.Value = value;
                return;
            }

            this.Params.Add(new FlowParam
            {
                Key = key,
                Value = value
            });
        }

        private void RemoveTemplateParam(string key)
        {
            if (string.IsNullOrWhiteSpace(key) || this.Params == null || this.Params.Count == 0)
            {
                return;
            }

            this.Params.RemoveAll(param => param != null && param.Key == key);
        }
    }
}

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
        [Header("基础配置")]
        public string ConfigId;
        [ValueDropdown(nameof(GetPointTypeOptions))]
        [OnValueChanged(nameof(OnTypeChanged))]
        public int Type = ECAPointType.EvacuationPoint;
        public List<FlowParam> Params = new();
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

            Dictionary<string, string> templateValues = new();
            foreach (FlowParam param in this.Params)
            {
                if (param == null || string.IsNullOrWhiteSpace(param.Key))
                {
                    continue;
                }

                if (this.IsTemplateParamKey(param.Key))
                {
                    templateValues[param.Key] = param.Value;
                }
            }

            foreach (string templateKey in this.GetTemplateParamKeys())
            {
                this.RemoveTemplateParam(templateKey);
            }

            foreach (FlowParam templateParam in this.GetTypeParamTemplate(this.Type))
            {
                string value = templateParam.Value;
                if (templateValues.TryGetValue(templateParam.Key, out string oldValue) && !string.IsNullOrWhiteSpace(oldValue))
                {
                    value = oldValue;
                }

                this.SetOrAddParam(templateParam.Key, value);
            }
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
            }

            if (pointType == ECAPointType.SpawnPoint)
            {
                template.Add(new FlowParam
                {
                    Key = ECAPointParamKey.TeamId,
                    Value = "0"
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

        private bool IsTemplateParamKey(string key)
        {
            foreach (string templateKey in this.GetTemplateParamKeys())
            {
                if (templateKey == key)
                {
                    return true;
                }
            }

            return false;
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
            yield return ECADoorParamKey.RequiredKeyItemId;
            yield return ECADoorParamKey.LockedButtonTextId;
            yield return ECADoorParamKey.ClosedButtonTextId;
            yield return ECADoorParamKey.OpenedButtonTextId;
            yield return ECADoorParamKey.ConsumeKeyCount;
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

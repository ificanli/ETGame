using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;

namespace ET.Client
{
    [EntitySystemOf(typeof(ECAInteractClientComponent))]
    public static partial class ECAInteractClientComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ECAInteractClientComponent self)
        {
            self.InRangePointIds.Clear();
            self.PointButtonTextIds.Clear();
            self.PointCanInteract.Clear();
            self.PointStates.Clear();
            self.FocusPointId = null;
            self.SearchingPointId = null;
            self.SearchState = ContainerSearchState.Idle;
            self.SearchRemainMs = 0;
            self.EvacuationPointId = null;
            self.EvacuationState = ECAEvacuationState.None;
            self.EvacuationRemainMs = 0;
            self.EvacuationEndTimeMs = 0;
            self.OpenContainerPointId = null;
            self.OpenContainerUiKey = null;
            self.ContainerOutputMode = ContainerOutputMode.ContainerPanel;
            self.ContainerItems.Clear();
            self.ConcealmentConfigMapName = null;
            self.LocalConcealmentAreas.Clear();
            self.SelfInConcealmentArea = false;
            self.SelfConcealmentAlpha = 1f;
        }

        public static void EnsureLocalConcealmentConfigLoaded(this ECAInteractClientComponent self, Scene root)
        {
            if (root == null || root.IsDisposed)
            {
                return;
            }

            string mapName = root.Name.GetSceneConfigName();
            if (self.ConcealmentConfigMapName == mapName)
            {
                return;
            }

            self.ConcealmentConfigMapName = mapName;
            self.LocalConcealmentAreas.Clear();
            self.SelfInConcealmentArea = false;
            self.SelfConcealmentAlpha = 1f;

            string path = $"Packages/cn.etetet.map/Bundles/ECA/{mapName}.txt";
            if (!File.Exists(path))
            {
                return;
            }

            string json = File.ReadAllText(path);
            List<ECAConfig> configs = MongoHelper.FromJson<List<ECAConfig>>(json);
            if (configs == null || configs.Count == 0)
            {
                return;
            }

            foreach (ECAConfig config in configs)
            {
                if (!TryBuildLocalConcealmentArea(config, out LocalConcealmentAreaData concealmentArea))
                {
                    continue;
                }

                self.LocalConcealmentAreas.Add(concealmentArea);
            }
        }

        public static void RefreshLocalConcealmentState(this ECAInteractClientComponent self, float3 position)
        {
            bool inConcealmentArea = false;
            float alpha = 1f;

            foreach (LocalConcealmentAreaData concealmentArea in self.LocalConcealmentAreas)
            {
                if (concealmentArea.Radius <= 0f)
                {
                    continue;
                }

                if (math.distancesq(position, concealmentArea.Position) > concealmentArea.Radius * concealmentArea.Radius)
                {
                    continue;
                }

                inConcealmentArea = true;
                if (concealmentArea.SelfAlpha > 0f && concealmentArea.SelfAlpha < alpha)
                {
                    alpha = concealmentArea.SelfAlpha;
                }
            }

            self.SelfInConcealmentArea = inConcealmentArea;
            self.SelfConcealmentAlpha = inConcealmentArea ? alpha : 1f;
        }

        private static bool TryBuildLocalConcealmentArea(ECAConfig config, out LocalConcealmentAreaData concealmentArea)
        {
            concealmentArea = default;
            if (config == null || config.FlowGraph?.Nodes == null || config.FlowGraph.Nodes.Count == 0)
            {
                return false;
            }

            float selfAlpha = 1f;
            bool hasStealthAction = false;
            foreach (FlowNodeData node in config.FlowGraph.Nodes)
            {
                if (node == null || node.NodeType != ECAFlowNodeType.Action || node.NodeKey != ECAFlowActionKey.ApplyStealth)
                {
                    continue;
                }

                hasStealthAction = true;
                if (TryGetFloatParam(node.Params, "self_alpha", out float alphaValue) && alphaValue > 0f)
                {
                    selfAlpha = alphaValue;
                }
            }

            if (!hasStealthAction)
            {
                return false;
            }

            float radius = config.GetInteractRange();
            if (radius <= 0f)
            {
                return false;
            }

            concealmentArea = new LocalConcealmentAreaData
            {
                Position = new float3(config.PosX, config.PosY, config.PosZ),
                Radius = radius,
                SelfAlpha = selfAlpha
            };
            return true;
        }

        private static bool TryGetFloatParam(List<FlowParam> source, string key, out float value)
        {
            value = 0f;
            if (source == null || source.Count == 0)
            {
                return false;
            }

            foreach (FlowParam param in source)
            {
                if (param == null || param.Key != key)
                {
                    continue;
                }

                return float.TryParse(param.Value, out value);
            }

            return false;
        }
    }
}

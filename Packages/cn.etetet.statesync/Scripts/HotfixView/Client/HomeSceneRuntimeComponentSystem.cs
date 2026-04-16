using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ET.Client
{
    [EntitySystemOf(typeof(HomeSceneRuntimeComponent))]
    public static partial class HomeSceneRuntimeComponentSystem
    {
        private const string RuntimeRootName = "HomeRuntimeRoot";
        private const string UrpLitShaderName = "Universal Render Pipeline/Lit";
        private const string StandardShaderName = "Standard";
        private const float ClickDistance = 512f;

        [EntitySystem]
        private static void Awake(this HomeSceneRuntimeComponent self)
        {
            self.LastDataSnapshot = string.Empty;
            self.LastSelectedSlotId = 0;
            self.LastSelectedBuildingId = 0;
            self.RootObject = null;
            self.HitLookup.Clear();
            self.Nodes.Clear();
            self.RuntimeMaterials.Clear();
        }

        [EntitySystem]
        private static void Destroy(this HomeSceneRuntimeComponent self)
        {
            self.ClearRuntime();
            self.LastDataSnapshot = string.Empty;
            self.LastSelectedSlotId = 0;
            self.LastSelectedBuildingId = 0;
        }

        [EntitySystem]
        private static void Update(this HomeSceneRuntimeComponent self)
        {
            Scene currentScene = self.GetParent<Scene>();
            if (currentScene == null || currentScene.IsDisposed)
            {
                return;
            }

            if (currentScene.Name.GetSceneConfigName() != "Home")
            {
                self.ClearRuntime();
                return;
            }

            Scene root = self.Root();
            if (root == null || root.IsDisposed)
            {
                return;
            }

            HomeClientComponent runtime = HomeClientHelper.GetOrAddRuntime(root);
            string snapshot = BuildRuntimeSnapshot(runtime);
            if (self.LastDataSnapshot != snapshot)
            {
                self.LastDataSnapshot = snapshot;
                self.RebuildRuntime(runtime);
            }

            HomePanelComponent homePanel = root.YIUIMgr()?.GetPanel<HomePanelComponent>();
            self.HandleSceneClick(homePanel);
            self.SyncSelectionState(homePanel);
        }

        private static string BuildRuntimeSnapshot(HomeClientComponent runtime)
        {
            if (runtime == null)
            {
                return string.Empty;
            }

            StringBuilder builder = new(256);
            builder.Append(runtime.Slots.Count).Append('|');
            builder.Append(runtime.Buildings.Count).Append('|');

            List<HomeClientSlotData> slots = BuildSortedSlots(runtime);
            for (int i = 0; i < slots.Count; ++i)
            {
                HomeClientSlotData slot = slots[i];
                builder.Append(slot.SlotId).Append(':')
                        .Append(slot.Unlocked ? '1' : '0').Append(':')
                        .Append(slot.SortOrder).Append(':')
                        .Append(slot.SlotType).Append(':')
                        .Append(slot.SceneAnchorKey).Append('|');
            }

            for (int i = 0; i < runtime.Buildings.Count; ++i)
            {
                HomeClientBuildingData building = runtime.Buildings[i];
                if (building == null)
                {
                    continue;
                }

                builder.Append(building.BuildingId).Append(':')
                        .Append(building.ConfigId).Append(':')
                        .Append(building.Level).Append(':')
                        .Append(building.SlotId).Append('|');
            }

            return builder.ToString();
        }

        private static void RebuildRuntime(this HomeSceneRuntimeComponent self, HomeClientComponent runtime)
        {
            self.ClearRuntime();

            List<HomeClientSlotData> slots = BuildSortedSlots(runtime);
            if (slots.Count == 0)
            {
                return;
            }

            Transform parent = ResolveRuntimeParent(self.Root());
            if (parent == null)
            {
                Log.Warning("[HomeSceneRuntime] runtime parent not found");
                return;
            }

            GameObject rootObject = new(RuntimeRootName);
            rootObject.transform.SetParent(parent, false);
            rootObject.transform.localPosition = Vector3.zero;
            rootObject.transform.localRotation = Quaternion.identity;
            rootObject.transform.localScale = Vector3.one;
            self.RootObject = rootObject;

            for (int i = 0; i < slots.Count; ++i)
            {
                HomeClientSlotData slot = slots[i];
                HomeClientBuildingData building = FindBuildingBySlot(runtime, slot.SlotId);
                Vector3 position = ResolveSlotPosition(slot, i);
                HomeSceneNodeData node = self.CreateNode(rootObject.transform, slot, building, position);
                if (node == null)
                {
                    continue;
                }

                self.Nodes.Add(node);
                self.RegisterHitData(node);
            }
        }

        private static HomeSceneNodeData CreateNode(
            this HomeSceneRuntimeComponent self,
            Transform parent,
            HomeClientSlotData slot,
            HomeClientBuildingData building,
            Vector3 position)
        {
            if (slot == null)
            {
                return null;
            }

            HomeBuildingConfig config = HomeBuildingConfigCategory.Instance.GetOrDefault(building?.ConfigId ?? 0);
            Color accent = ResolveAccentColor(slot, config);
            GameObject nodeRoot = new($"HomeSlot_{slot.SlotId}");
            nodeRoot.transform.SetParent(parent, false);
            nodeRoot.transform.localPosition = position;
            nodeRoot.transform.localRotation = Quaternion.identity;
            nodeRoot.transform.localScale = Vector3.one;

            HomeSceneNodeData node = new()
            {
                SlotId = slot.SlotId,
                BuildingId = building?.BuildingId ?? 0,
                RootObject = nodeRoot,
            };

            self.CreatePrimitive(node, nodeRoot.transform, $"Pad_{slot.SlotId}", PrimitiveType.Cylinder,
                new Vector3(0f, 0.18f, 0f), new Vector3(2.6f, 0.08f, 2.6f), MultiplyColor(accent, 0.72f), Vector3.zero);

            if (!slot.Unlocked)
            {
                self.CreatePrimitive(node, nodeRoot.transform, "LockBarA", PrimitiveType.Cube,
                    new Vector3(0f, 0.95f, 0f), new Vector3(0.45f, 1.9f, 0.45f), accent, new Vector3(0f, 0f, 45f));
                self.CreatePrimitive(node, nodeRoot.transform, "LockBarB", PrimitiveType.Cube,
                    new Vector3(0f, 0.95f, 0f), new Vector3(0.45f, 1.9f, 0.45f), accent, new Vector3(0f, 0f, -45f));
                return node;
            }

            if (building == null || config == null)
            {
                self.CreatePrimitive(node, nodeRoot.transform, "PlotCore", PrimitiveType.Cylinder,
                    new Vector3(0f, 0.5f, 0f), new Vector3(1.4f, 0.06f, 1.4f), accent, Vector3.zero);
                self.CreatePrimitive(node, nodeRoot.transform, "PlotMarker", PrimitiveType.Cube,
                    new Vector3(0f, 1.1f, 0f), new Vector3(0.45f, 0.9f, 0.45f), MultiplyColor(accent, 1.12f), Vector3.zero);
                return node;
            }

            switch (config.SceneVisualKey)
            {
                case "main_city":
                    self.CreatePrimitive(node, nodeRoot.transform, "MainCityBase", PrimitiveType.Cube,
                        new Vector3(0f, 1.2f, 0f), new Vector3(3.4f, 1.6f, 3.4f), accent, Vector3.zero);
                    self.CreatePrimitive(node, nodeRoot.transform, "MainCityTower", PrimitiveType.Cylinder,
                        new Vector3(0f, 3.1f, 0f), new Vector3(1.1f, 1.2f, 1.1f), MultiplyColor(accent, 1.12f), Vector3.zero);
                    break;
                case "museum":
                    self.CreatePrimitive(node, nodeRoot.transform, "MuseumBase", PrimitiveType.Cylinder,
                        new Vector3(0f, 0.85f, 0f), new Vector3(2.2f, 0.35f, 2.2f), MultiplyColor(accent, 0.95f), Vector3.zero);
                    self.CreatePrimitive(node, nodeRoot.transform, "MuseumTop", PrimitiveType.Sphere,
                        new Vector3(0f, 2.0f, 0f), new Vector3(2.4f, 1.2f, 2.4f), MultiplyColor(accent, 1.06f), Vector3.zero);
                    break;
                case "recycle_room":
                    self.CreatePrimitive(node, nodeRoot.transform, "RecycleBody", PrimitiveType.Cube,
                        new Vector3(0f, 1.15f, 0f), new Vector3(2.8f, 1.3f, 2.0f), accent, Vector3.zero);
                    self.CreatePrimitive(node, nodeRoot.transform, "RecycleChimney", PrimitiveType.Cylinder,
                        new Vector3(0.9f, 2.6f, -0.45f), new Vector3(0.42f, 0.9f, 0.42f), MultiplyColor(accent, 1.12f), Vector3.zero);
                    break;
                case "farm":
                    self.CreatePrimitive(node, nodeRoot.transform, "FarmBarn", PrimitiveType.Capsule,
                        new Vector3(0f, 1.15f, 0f), new Vector3(2.2f, 1.15f, 2.2f), accent, new Vector3(0f, 90f, 90f));
                    self.CreatePrimitive(node, nodeRoot.transform, "FarmCropA", PrimitiveType.Sphere,
                        new Vector3(-0.9f, 0.75f, 0.75f), new Vector3(0.65f, 0.45f, 0.65f), MultiplyColor(accent, 1.16f), Vector3.zero);
                    self.CreatePrimitive(node, nodeRoot.transform, "FarmCropB", PrimitiveType.Sphere,
                        new Vector3(0.9f, 0.75f, -0.55f), new Vector3(0.65f, 0.45f, 0.65f), MultiplyColor(accent, 1.08f), Vector3.zero);
                    break;
                case "warehouse":
                    self.CreatePrimitive(node, nodeRoot.transform, "WarehouseBody", PrimitiveType.Cube,
                        new Vector3(0f, 1.0f, 0f), new Vector3(3.2f, 1.2f, 2.4f), accent, Vector3.zero);
                    self.CreatePrimitive(node, nodeRoot.transform, "WarehouseRoof", PrimitiveType.Cube,
                        new Vector3(0f, 2.0f, 0f), new Vector3(2.8f, 0.28f, 2.0f), MultiplyColor(accent, 1.12f), Vector3.zero);
                    break;
                default:
                    self.CreatePrimitive(node, nodeRoot.transform, "BuildingFallback", PrimitiveType.Cube,
                        new Vector3(0f, 1.1f, 0f), new Vector3(2.4f, 1.4f, 2.4f), accent, Vector3.zero);
                    break;
            }

            return node;
        }

        private static void HandleSceneClick(this HomeSceneRuntimeComponent self, HomePanelComponent homePanel)
        {
            if (self.RootObject == null || self.HitLookup.Count == 0)
            {
                return;
            }

            if (!Input.GetMouseButtonDown(0))
            {
                return;
            }

            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (!IsHomePanelOpen(homePanel))
            {
                return;
            }

            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            Ray ray = camera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, ClickDistance))
            {
                return;
            }

            if (!self.HitLookup.TryGetValue(hit.collider.GetInstanceID(), out HomeSceneHitData hitData) || hitData == null)
            {
                return;
            }

            homePanel.SelectSceneTarget(hitData.SlotId, hitData.BuildingId);

            self.ApplySelection(hitData.SlotId, hitData.BuildingId);
        }

        private static void SyncSelectionState(this HomeSceneRuntimeComponent self, HomePanelComponent homePanel)
        {
            int selectedSlotId = 0;
            long selectedBuildingId = 0;
            if (IsHomePanelOpen(homePanel))
            {
                selectedSlotId = homePanel.SelectedHomeSlotId;
                selectedBuildingId = homePanel.SelectedHomeBuildingId;
            }

            if (selectedSlotId == self.LastSelectedSlotId && selectedBuildingId == self.LastSelectedBuildingId)
            {
                return;
            }

            self.ApplySelection(selectedSlotId, selectedBuildingId);
        }

        private static void ApplySelection(this HomeSceneRuntimeComponent self, int selectedSlotId, long selectedBuildingId)
        {
            self.LastSelectedSlotId = selectedSlotId;
            self.LastSelectedBuildingId = selectedBuildingId;

            for (int i = 0; i < self.Nodes.Count; ++i)
            {
                HomeSceneNodeData node = self.Nodes[i];
                if (node?.RootObject == null)
                {
                    continue;
                }

                bool selected = selectedSlotId > 0
                    ? node.SlotId == selectedSlotId
                    : selectedBuildingId > 0 && node.BuildingId == selectedBuildingId;

                node.RootObject.transform.localScale = selected ? Vector3.one * 1.08f : Vector3.one;
                for (int j = 0; j < node.Materials.Count; ++j)
                {
                    HomeSceneMaterialEntry materialEntry = node.Materials[j];
                    if (materialEntry?.Material == null)
                    {
                        continue;
                    }

                    Color color = selected
                        ? Color.Lerp(materialEntry.BaseColor, Color.white, 0.32f)
                        : materialEntry.BaseColor;
                    ApplyMaterialColor(materialEntry.Material, color);
                }
            }
        }

        private static void RegisterHitData(this HomeSceneRuntimeComponent self, HomeSceneNodeData node)
        {
            if (node?.RootObject == null)
            {
                return;
            }

            HomeSceneHitData hitData = new()
            {
                SlotId = node.SlotId,
                BuildingId = node.BuildingId,
            };

            Collider[] colliders = node.RootObject.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; ++i)
            {
                Collider collider = colliders[i];
                if (collider == null)
                {
                    continue;
                }

                self.HitLookup[collider.GetInstanceID()] = hitData;
            }
        }

        private static GameObject CreatePrimitive(
            this HomeSceneRuntimeComponent self,
            HomeSceneNodeData node,
            Transform parent,
            string name,
            PrimitiveType primitiveType,
            Vector3 localPosition,
            Vector3 localScale,
            Color color,
            Vector3 localEulerAngles)
        {
            GameObject primitive = GameObject.CreatePrimitive(primitiveType);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = localPosition;
            primitive.transform.localRotation = Quaternion.Euler(localEulerAngles);
            primitive.transform.localScale = localScale;

            Renderer renderer = primitive.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = self.CreateRuntimeMaterial(color);
                renderer.sharedMaterial = material;
                node.Materials.Add(new HomeSceneMaterialEntry
                {
                    Material = material,
                    BaseColor = color,
                });
            }

            return primitive;
        }

        private static Material CreateRuntimeMaterial(this HomeSceneRuntimeComponent self, Color color)
        {
            Shader shader = Shader.Find(UrpLitShaderName);
            shader ??= Shader.Find(StandardShaderName);
            Material material = shader != null ? new Material(shader) : new Material(Shader.Find(StandardShaderName));
            ApplyMaterialColor(material, color);
            self.RuntimeMaterials.Add(material);
            return material;
        }

        private static void ApplyMaterialColor(Material material, Color color)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
        }

        private static void ClearRuntime(this HomeSceneRuntimeComponent self)
        {
            if (self.RootObject != null)
            {
                UnityEngine.Object.Destroy(self.RootObject);
                self.RootObject = null;
            }

            for (int i = 0; i < self.RuntimeMaterials.Count; ++i)
            {
                if (self.RuntimeMaterials[i] != null)
                {
                    UnityEngine.Object.Destroy(self.RuntimeMaterials[i]);
                }
            }

            self.RuntimeMaterials.Clear();
            self.HitLookup.Clear();
            self.Nodes.Clear();
        }

        private static Transform ResolveRuntimeParent(Scene root)
        {
            Transform parent = root?.GetComponent<GlobalComponent>()?.Unit;
            if (parent != null)
            {
                return parent;
            }

            GameObject fallback = GameObject.Find("/Global/Unit");
            return fallback != null ? fallback.transform : null;
        }

        private static List<HomeClientSlotData> BuildSortedSlots(HomeClientComponent runtime)
        {
            List<HomeClientSlotData> result = new();
            if (runtime == null)
            {
                return result;
            }

            for (int i = 0; i < runtime.Slots.Count; ++i)
            {
                HomeClientSlotData slot = runtime.Slots[i];
                if (slot != null)
                {
                    result.Add(slot);
                }
            }

            result.Sort(static (a, b) =>
            {
                int compare = a.SortOrder.CompareTo(b.SortOrder);
                return compare != 0 ? compare : a.SlotId.CompareTo(b.SlotId);
            });
            return result;
        }

        private static HomeClientBuildingData FindBuildingBySlot(HomeClientComponent runtime, int slotId)
        {
            if (runtime == null || slotId <= 0)
            {
                return null;
            }

            for (int i = 0; i < runtime.Buildings.Count; ++i)
            {
                HomeClientBuildingData building = runtime.Buildings[i];
                if (building != null && building.SlotId == slotId)
                {
                    return building;
                }
            }

            return null;
        }

        private static Vector3 ResolveSlotPosition(HomeClientSlotData slot, int index)
        {
            if (TryParseVector3(slot?.SceneAnchorKey, out Vector3 position))
            {
                return position;
            }

            if (slot != null && slot.SlotType == HomeSlotType.MainCity)
            {
                return Vector3.zero;
            }

            int ringIndex = Mathf.Max(0, index - 1);
            float angle = ringIndex * 72f * Mathf.Deg2Rad;
            float radius = 12f + (ringIndex / 5) * 4f;
            return new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        }

        private static bool TryParseVector3(string text, out Vector3 value)
        {
            value = Vector3.zero;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string[] parts = text.Split(',');
            if (parts.Length != 3)
            {
                return false;
            }

            if (!float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) ||
                !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y) ||
                !float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
            {
                return false;
            }

            value = new Vector3(x, y, z);
            return true;
        }

        private static bool IsHomePanelOpen(HomePanelComponent panel)
        {
            return panel != null &&
                !panel.IsDisposed;
        }

        private static Color ResolveAccentColor(HomeClientSlotData slot, HomeBuildingConfig config)
        {
            if (slot == null || !slot.Unlocked)
            {
                return GetLockedColor();
            }

            if (config == null)
            {
                return GetEmptySlotColor();
            }

            return config.BuildingType switch
            {
                HomeBuildingType.MainCity => GetMainCityColor(),
                HomeBuildingType.Museum => GetMuseumColor(),
                HomeBuildingType.RecycleRoom => GetRecycleColor(),
                HomeBuildingType.Farm => GetFarmColor(),
                HomeBuildingType.Warehouse => GetWarehouseColor(),
                _ => GetEmptySlotColor(),
            };
        }

        private static Color GetLockedColor()
        {
            return new Color(0.23f, 0.24f, 0.29f, 1f);
        }

        private static Color GetEmptySlotColor()
        {
            return new Color(0.28f, 0.44f, 0.66f, 1f);
        }

        private static Color GetMainCityColor()
        {
            return new Color(0.86f, 0.66f, 0.28f, 1f);
        }

        private static Color GetMuseumColor()
        {
            return new Color(0.72f, 0.30f, 0.33f, 1f);
        }

        private static Color GetRecycleColor()
        {
            return new Color(0.24f, 0.62f, 0.66f, 1f);
        }

        private static Color GetFarmColor()
        {
            return new Color(0.34f, 0.67f, 0.33f, 1f);
        }

        private static Color GetWarehouseColor()
        {
            return new Color(0.68f, 0.46f, 0.22f, 1f);
        }

        private static Color MultiplyColor(Color color, float scale)
        {
            return new Color(
                Mathf.Clamp01(color.r * scale),
                Mathf.Clamp01(color.g * scale),
                Mathf.Clamp01(color.b * scale),
                color.a);
        }
    }
}

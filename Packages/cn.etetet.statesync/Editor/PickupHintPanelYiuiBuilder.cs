using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using YIUIFramework;

namespace ET.Editor
{
    public static class PickupHintPanelYiuiBuilder
    {
        private const string MainPanelPrefabPath =
            "Packages/cn.etetet.statesync/Assets/GameRes/YIUI/Main/Prefabs/MainPanel.prefab";
        private const string PickupHintPanelPrefabPath =
            "Packages/cn.etetet.statesync/Assets/GameRes/YIUI/Main/Prefabs/PickupHintPanel.prefab";

        [MenuItem("ET/YIUI/Build PickupHintPanel Resources")]
        public static void Build()
        {
            try
            {
                StyleContext style = LoadStyleContext();
                BuildPickupHintPanel(style);
                GenerateYiuiCode();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[PickupHintPanelYiuiBuilder] Build finished.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"[PickupHintPanelYiuiBuilder] Build failed: {exception}");
                throw;
            }
        }

        private static void BuildPickupHintPanel(StyleContext style)
        {
            EnsureDirectoryForAsset(PickupHintPanelPrefabPath);

            GameObject root = new("PickupHintPanel", typeof(RectTransform), typeof(CanvasRenderer));
            try
            {
                SetLayerRecursively(root, LayerMask.NameToLayer("UI"));

                RectTransform rootRect = root.GetComponent<RectTransform>();
                Stretch(rootRect);

                UIBindCDETable cdeTable = root.GetOrAddComponent<UIBindCDETable>();
                cdeTable.UICodeType = EUICodeType.Panel;
                cdeTable.PanelLayer = EPanelLayer.Panel;
                cdeTable.PkgName = "Main";
                cdeTable.ResName = "PickupHintPanel";
                cdeTable.ComponentTable = root.GetOrAddComponent<UIBindComponentTable>();
                cdeTable.DataTable = root.GetOrAddComponent<UIBindDataTable>();
                cdeTable.EventTable = root.GetOrAddComponent<UIBindEventTable>();

                RectTransform content = CreateRect("Content", rootRect);
                content.anchorMin = new Vector2(0.5f, 0f);
                content.anchorMax = new Vector2(0.5f, 0f);
                content.pivot = new Vector2(0.5f, 0f);
                content.sizeDelta = new Vector2(360f, 134f);
                content.anchoredPosition = new Vector2(0f, 208f);

                RectTransform background = CreatePanel("Background", content, new Color32(13, 18, 24, 235));
                Stretch(background);
                EnsureBinding(cdeTable.ComponentTable, background, "Background");

                Outline backgroundOutline = background.gameObject.AddComponent<Outline>();
                backgroundOutline.effectColor = new Color32(0, 0, 0, 120);
                backgroundOutline.effectDistance = new Vector2(2f, -2f);

                RectTransform accent = CreatePanel("Accent", content, new Color32(230, 188, 86, 255));
                accent.anchorMin = new Vector2(0f, 0f);
                accent.anchorMax = new Vector2(0f, 1f);
                accent.pivot = new Vector2(0f, 0.5f);
                accent.sizeDelta = new Vector2(8f, 0f);
                accent.anchoredPosition = Vector2.zero;
                SetGraphicRaycastTarget(accent, false);
                EnsureBinding(cdeTable.ComponentTable, accent, "Accent");

                TextMeshProUGUI itemName = CreateText("ItemName", content, style, 32f, FontStyles.Bold);
                itemName.text = "拾取";
                itemName.alignment = TextAlignmentOptions.Left;
                itemName.textWrappingMode = TextWrappingModes.NoWrap;
                itemName.overflowMode = TextOverflowModes.Ellipsis;
                itemName.rectTransform.anchorMin = new Vector2(0f, 1f);
                itemName.rectTransform.anchorMax = new Vector2(1f, 1f);
                itemName.rectTransform.pivot = new Vector2(0.5f, 1f);
                itemName.rectTransform.offsetMin = new Vector2(26f, -56f);
                itemName.rectTransform.offsetMax = new Vector2(-22f, -14f);
                EnsureBinding(cdeTable.ComponentTable, itemName, "ItemName");

                TextMeshProUGUI subTitle = CreateText("SubTitle", content, style, 20f, FontStyles.Normal);
                subTitle.text = "靠近地面掉落物后，可直接在这里完成拾取。";
                subTitle.alignment = TextAlignmentOptions.TopLeft;
                subTitle.textWrappingMode = TextWrappingModes.Normal;
                subTitle.color = new Color32(181, 191, 205, 255);
                subTitle.rectTransform.anchorMin = new Vector2(0f, 0f);
                subTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
                subTitle.rectTransform.offsetMin = new Vector2(26f, 56f);
                subTitle.rectTransform.offsetMax = new Vector2(-128f, -62f);
                SetGraphicRaycastTarget(subTitle, false);
                EnsureBinding(cdeTable.ComponentTable, subTitle, "SubTitle");

                Button pickupButton = CreateButton("PickupButton", content, style);
                RectTransform pickupButtonRect = pickupButton.transform as RectTransform;
                pickupButtonRect.anchorMin = new Vector2(1f, 0.5f);
                pickupButtonRect.anchorMax = new Vector2(1f, 0.5f);
                pickupButtonRect.pivot = new Vector2(1f, 0.5f);
                pickupButtonRect.sizeDelta = new Vector2(126f, 54f);
                pickupButtonRect.anchoredPosition = new Vector2(-18f, 0f);
                SetButtonVisual(pickupButton, new Color32(226, 177, 66, 255), style.ButtonSprite, style.ButtonColors);
                EnsureBinding(cdeTable.ComponentTable, pickupButton, "PickupButton");

                TextMeshProUGUI pickupButtonText = CreateText("Text", pickupButtonRect, style, 24f, FontStyles.Bold);
                pickupButtonText.text = "拾取";
                pickupButtonText.alignment = TextAlignmentOptions.Center;
                pickupButtonText.color = new Color32(34, 26, 12, 255);
                Stretch(pickupButtonText.rectTransform);

                ConfigureDataTable(cdeTable.DataTable);
                ConfigureEventTable(cdeTable.EventTable);
                ConfigureItemNameBinding(itemName.gameObject.AddComponent<UIDataBindTextTMP>(), cdeTable.DataTable, "u_DataItemName");
                ConfigurePickupButtonBinding(pickupButton.gameObject.AddComponent<UITaskEventBindClick>(), "u_EventPickup");

                cdeTable.ComponentTable.AutoCheck();
                cdeTable.DataTable.AutoCheck();
                cdeTable.EventTable.AutoCheck();

                PrefabUtility.SaveAsPrefabAsset(root, PickupHintPanelPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void ConfigureDataTable(UIBindDataTable dataTable)
        {
            if (dataTable == null)
            {
                throw new InvalidOperationException("PickupHintPanel missing UIBindDataTable.");
            }

            Dictionary<string, UIData> dataDictionary = GetPrivateField<Dictionary<string, UIData>>(dataTable, "m_DataDic");
            dataDictionary.Clear();
            dataDictionary["u_DataItemName"] = new UIData("u_DataItemName", new UIDataValueString());
            EditorUtility.SetDirty(dataTable);
        }

        private static void ConfigureEventTable(UIBindEventTable eventTable)
        {
            if (eventTable == null)
            {
                throw new InvalidOperationException("PickupHintPanel missing UIBindEventTable.");
            }

            Dictionary<string, UIEventBase> eventDictionary = GetPrivateField<Dictionary<string, UIEventBase>>(eventTable, "m_EventDic");
            eventDictionary.Clear();
            eventDictionary["u_EventPickup"] =
                    UITaskEventBaseHelper.CreatorUITaskEventBase("u_EventPickup", new List<EUIEventParamType>());
            EditorUtility.SetDirty(eventTable);
        }

        private static void ConfigureItemNameBinding(UIDataBindTextTMP bind, UIBindDataTable dataTable, string dataName)
        {
            if (bind == null)
            {
                throw new InvalidOperationException("PickupHintPanel missing UIDataBindTextTMP.");
            }

            UIData data = dataTable.FindData(dataName);
            if (data == null)
            {
                throw new InvalidOperationException($"PickupHintPanel missing data definition: {dataName}");
            }

            Dictionary<string, UIDataSelect> dataSelectDictionary =
                    GetPrivateField<Dictionary<string, UIDataSelect>>(bind, "m_DataSelectDic");
            dataSelectDictionary.Clear();
            dataSelectDictionary[dataName] = new UIDataSelect(data);
            EditorUtility.SetDirty(bind);
        }

        private static void ConfigurePickupButtonBinding(UITaskEventBindClick bind, string eventName)
        {
            if (bind == null)
            {
                throw new InvalidOperationException("PickupHintPanel missing UITaskEventBindClick.");
            }

            SetPrivateField(bind, "m_EventName", eventName);
            EditorUtility.SetDirty(bind);
        }

        private static void GenerateYiuiCode()
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(PickupHintPanelPrefabPath);
            if (asset == null)
            {
                throw new InvalidOperationException("PickupHintPanel prefab save failed.");
            }

            UIBindCDETable cdeTable = asset.GetComponent<UIBindCDETable>();
            if (cdeTable == null)
            {
                throw new InvalidOperationException("PickupHintPanel missing UIBindCDETable.");
            }

            InvokeUICreatePackages(cdeTable, "statesync");
        }

        private static void InvokeUICreatePackages(UIBindCDETable cdeTable, string createPackageName)
        {
            Type createModuleType = Type.GetType("YIUIFramework.Editor.UICreateModule, ET.YIUIFramework.Editor");
            if (createModuleType == null)
            {
                throw new InvalidOperationException("Cannot find YIUIFramework.Editor.UICreateModule.");
            }

            MethodInfo methodInfo = createModuleType.GetMethod(
                "CreatePackages",
                new[] { typeof(UIBindCDETable), typeof(bool), typeof(bool), typeof(string) });
            if (methodInfo == null)
            {
                throw new InvalidOperationException("Cannot find UICreateModule.CreatePackages.");
            }

            methodInfo.Invoke(null, new object[] { cdeTable, false, false, createPackageName });
        }

        private static Button CreateButton(string name, RectTransform parent, StyleContext style)
        {
            GameObject buttonObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            Button button = buttonObject.GetComponent<Button>();
            SetButtonVisual(button, Color.white, style.ButtonSprite, style.ButtonColors);
            return button;
        }

        private static RectTransform CreatePanel(string name, RectTransform parent, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return rect;
        }

        private static RectTransform CreateRect(string name, RectTransform parent)
        {
            GameObject rectObject = new(name, typeof(RectTransform), typeof(CanvasRenderer));
            rectObject.transform.SetParent(parent, false);
            return rectObject.GetComponent<RectTransform>();
        }

        private static TextMeshProUGUI CreateText(
            string name,
            RectTransform parent,
            StyleContext style,
            float fontSize,
            FontStyles fontStyles)
        {
            GameObject textObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            if (style.FontAsset != null)
            {
                text.font = style.FontAsset;
            }

            if (style.FontMaterial != null)
            {
                text.fontSharedMaterial = style.FontMaterial;
            }

            text.fontSize = fontSize;
            text.fontStyle = fontStyles;
            text.color = new Color32(239, 240, 244, 255);
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            return text;
        }

        private static void SetButtonVisual(Button button, Color color, Sprite sprite, ColorBlock colorBlock)
        {
            Image image = button.GetComponent<Image>();
            image.sprite = sprite;
            image.type = sprite == null ? Image.Type.Simple : Image.Type.Sliced;
            image.color = color;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = colorBlock;
            button.targetGraphic = image;
        }

        private static void SetGraphicRaycastTarget(Component component, bool raycastTarget)
        {
            Graphic graphic = component != null ? component.GetComponent<Graphic>() : null;
            if (graphic != null)
            {
                graphic.raycastTarget = raycastTarget;
            }
        }

        private static void EnsureBinding(UIBindComponentTable componentTable, Component component, string bindName)
        {
            string uiName = $"u_Com{bindName}";
            if (componentTable.AllBindDic.ContainsKey(uiName))
            {
                return;
            }

            componentTable.EditorAddComponent(component, bindName);
        }

        private static void Stretch(RectTransform rectTransform)
        {
            Stretch(rectTransform, Vector2.zero, Vector2.zero);
        }

        private static void Stretch(RectTransform rectTransform, Vector2 offsetMin, Vector2 offsetMax)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
        }

        private static void EnsureDirectoryForAsset(string assetPath)
        {
            string directory = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), directory));
            }
        }

        private static void SetLayerRecursively(GameObject gameObject, int layer)
        {
            if (layer < 0)
            {
                return;
            }

            gameObject.layer = layer;
            Transform transform = gameObject.transform;
            for (int i = 0; i < transform.childCount; ++i)
            {
                SetLayerRecursively(transform.GetChild(i).gameObject, layer);
            }
        }

        private static StyleContext LoadStyleContext()
        {
            GameObject mainPanelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(MainPanelPrefabPath);
            if (mainPanelAsset == null)
            {
                return CreateFallbackStyle();
            }

            TextMeshProUGUI sourceText = mainPanelAsset.GetComponentInChildren<TextMeshProUGUI>(true);
            Button sourceButton = mainPanelAsset.transform.FindChildByName("SearchButton")?.GetComponent<Button>();
            Image sourceImage = sourceButton != null ? sourceButton.GetComponent<Image>() : null;

            StyleContext style = new();
            style.FontAsset = sourceText != null ? sourceText.font : TMP_Settings.defaultFontAsset;
            style.FontMaterial = sourceText != null ? sourceText.fontSharedMaterial : null;
            style.ButtonSprite = sourceImage != null ? sourceImage.sprite : null;
            style.ButtonColors = sourceButton != null ? sourceButton.colors : ColorBlock.defaultColorBlock;
            return style;
        }

        private static StyleContext CreateFallbackStyle()
        {
            StyleContext style = new();
            style.FontAsset = TMP_Settings.defaultFontAsset;
            style.FontMaterial = null;
            style.ButtonSprite = null;
            style.ButtonColors = ColorBlock.defaultColorBlock;
            return style;
        }

        private static T GetPrivateField<T>(object target, string fieldName) where T : class
        {
            FieldInfo fieldInfo = FindFieldInfo(target.GetType(), fieldName);
            if (fieldInfo == null)
            {
                throw new InvalidOperationException($"Cannot find field: {target.GetType().FullName}.{fieldName}");
            }

            T value = fieldInfo.GetValue(target) as T;
            if (value == null)
            {
                throw new InvalidOperationException($"Field value is null: {target.GetType().FullName}.{fieldName}");
            }

            return value;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo fieldInfo = FindFieldInfo(target.GetType(), fieldName);
            if (fieldInfo == null)
            {
                throw new InvalidOperationException($"Cannot find field: {target.GetType().FullName}.{fieldName}");
            }

            fieldInfo.SetValue(target, value);
        }

        private static FieldInfo FindFieldInfo(Type type, string fieldName)
        {
            while (type != null)
            {
                FieldInfo fieldInfo = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (fieldInfo != null)
                {
                    return fieldInfo;
                }

                type = type.BaseType;
            }

            return null;
        }

        private struct StyleContext
        {
            public TMP_FontAsset FontAsset;
            public Material FontMaterial;
            public Sprite ButtonSprite;
            public ColorBlock ButtonColors;
        }
    }
}

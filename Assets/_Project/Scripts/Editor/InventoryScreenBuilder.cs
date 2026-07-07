// Editor tool: builds the full-screen InventoryScreen (Part 5) under the scene Canvas and auto-wires it.
// Left = live character preview + paper-doll slots + stats summary; right = gear tabs/filter + icon grid.
// Menu: Tools/ProjectAlpha/Build Inventory Screen. Built in 1920x1080 ref space (anchors + layout groups).
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;

namespace ProjectAlpha.EditorTools
{
    public static class InventoryScreenBuilder
    {
        private const string HostName = "InventoryScreen";
        private const string PrefabDir = "Assets/_Project/Prefabs/UI";
        private const string CellPrefabPath = PrefabDir + "/BagItemCell.prefab";
        private const string RenderDir = "Assets/_Project/Rendering";
        private const string PreviewRtPath = RenderDir + "/InventoryPreview.renderTexture";

        // Per-slot filter tabs (All + one per EquipmentSlot). Slot is ignored on the "All" entry.
        private static readonly (bool isAll, EquipmentSlot slot, string label)[] FilterDefs =
        {
            (true,  EquipmentSlot.Head,            "All"),
            (false, EquipmentSlot.PrimaryWeapon,   "Weapon"),
            (false, EquipmentSlot.SecondaryWeapon, "Shield"),
            (false, EquipmentSlot.Head,            "Head"),
            (false, EquipmentSlot.Chest,           "Chest"),
            (false, EquipmentSlot.Gloves,          "Gloves"),
            (false, EquipmentSlot.Legs,            "Legs"),
            (false, EquipmentSlot.Boots,           "Boots"),
            (false, EquipmentSlot.Cape,            "Cape"),
            (false, EquipmentSlot.Ring,            "Ring"),
            (false, EquipmentSlot.Amulet,          "Amulet"),
        };

        // Palette (dark, HUD-neutral; restyle with AriaGUI afterwards if desired).
        private static readonly Color Backdrop   = new Color(0.05f, 0.06f, 0.08f, 0.96f);
        private static readonly Color PanelBg     = new Color(0.10f, 0.11f, 0.14f, 0.98f);
        private static readonly Color HeaderText  = new Color(0.85f, 0.87f, 0.92f, 1f);
        private static readonly Color BodyText    = new Color(0.92f, 0.93f, 0.96f, 1f);
        private static readonly Color DimText     = new Color(0.50f, 0.52f, 0.56f, 1f);
        private static readonly Color MsgText     = new Color(1f, 0.82f, 0.35f, 1f);
        private static readonly Color WidgetBg    = new Color(0.17f, 0.19f, 0.23f, 1f);
        private static readonly Color WidgetHi    = new Color(0.24f, 0.27f, 0.33f, 1f);
        private static readonly Color WidgetDown  = new Color(0.13f, 0.15f, 0.18f, 1f);

        [MenuItem("Tools/ProjectAlpha/Build Inventory Screen")]
        public static void Build()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Build Inventory Screen",
                    "No Canvas found in the open scene. Open the gameplay scene with the HUD Canvas first.", "OK");
                return;
            }
            if (canvas.transform.Find(HostName) != null)
            {
                EditorUtility.DisplayDialog("Build Inventory Screen",
                    $"A '{HostName}' object already exists under '{canvas.name}'. Delete it first to rebuild.", "OK");
                return;
            }

            EnsureEventSystem();

            // Host (always active) + toggled root ------------------------------------------------
            GameObject host = NewUI(HostName, canvas.transform);
            Stretch(RT(host));
            Undo.RegisterCreatedObjectUndo(host, "Build Inventory Screen");
            InventoryScreen screen = host.AddComponent<InventoryScreen>();

            GameObject root = NewUI("Root", host.transform);
            Stretch(RT(root));

            GameObject backdrop = NewUI("Backdrop", root.transform);
            Stretch(RT(backdrop));
            AddImage(backdrop, Backdrop);

            const float inset = 28f, topInset = 100f, botInset = 70f, colGap = 20f;

            GameObject title = NewUI("Title", root.transform);
            BandTop(RT(title), height: 56f, inset: inset, topMargin: 24f);
            AddText(title, "Inventory", 42f, HeaderText, TextAlignmentOptions.Left);

            GameObject message = NewUI("MessageLabel", root.transform);
            BandBottom(RT(message), height: 40f, inset: inset, bottomMargin: 18f);
            TMP_Text messageLabel = AddText(message, string.Empty, 26f, MsgText, TextAlignmentOptions.Left);

            // Left: character preview ------------------------------------------------------------
            GameObject left = NewUI("CharacterPanel", root.transform);
            FractionColumn(RT(left), 0f, 0.26f, inset, colGap * 0.5f, topInset, botInset);
            AddImage(left, PanelBg);
            AddVerticalLayout(left, spacing: 10f, pad: 14);

            GameObject nameGo = NewUI("CharacterName", left.transform);
            AddText(nameGo, "Warrior", 30f, HeaderText, TextAlignmentOptions.Left);
            AddLayoutHeight(nameGo, 44f, flexible: false);

            GameObject previewGo = NewUI("Preview", left.transform);
            RawImage previewImage = previewGo.AddComponent<RawImage>();
            previewImage.color = Color.white;
            AddLayoutHeight(previewGo, 320f, flexible: true);
            PreviewDragRotator rotator = previewGo.AddComponent<PreviewDragRotator>();

            // Center: category tabs + slot rail + grid -------------------------------------------
            GameObject center = NewUI("ItemsPanel", root.transform);
            FractionColumn(RT(center), 0.27f, 0.64f, colGap * 0.5f, colGap * 0.5f, topInset, botInset);
            AddImage(center, PanelBg);
            AddVerticalLayout(center, spacing: 8f, pad: 14);

            GameObject tabs = NewUI("Tabs", center.transform);
            AddHorizontalLayout(tabs, spacing: 8f);
            AddLayoutHeight(tabs, 44f, flexible: false);
            BuildTab(tabs.transform, "Gear", interactable: true);
            BuildTab(tabs.transform, "Materials", interactable: false);
            BuildTab(tabs.transform, "Consumables", interactable: false);

            GameObject filters = NewUI("SlotRail", center.transform);
            AddHorizontalLayout(filters, spacing: 6f);
            AddLayoutHeight(filters, 46f, flexible: false);
            var filterButtons = new List<GearFilterButton>(FilterDefs.Length);
            foreach ((bool isAll, EquipmentSlot slot, string label) in FilterDefs)
            {
                filterButtons.Add(BuildFilterButton(filters.transform, isAll, slot, label));
            }

            GameObject filterNameGo = NewUI("FilterName", center.transform);
            TMP_Text filterNameLabel = AddText(filterNameGo, "All", 26f, HeaderText, TextAlignmentOptions.Left);
            AddLayoutHeight(filterNameGo, 34f, flexible: false);

            Transform bagContent = BuildBagGrid(center.transform);

            // Right: item detail + stat comparison -----------------------------------------------
            GameObject right = NewUI("DetailPanel", root.transform);
            FractionColumn(RT(right), 0.65f, 1f, colGap * 0.5f, inset, topInset, botInset);
            AddImage(right, PanelBg);
            AddVerticalLayout(right, spacing: 6f, pad: 16);
            ItemDetailPanel detailPanel = BuildDetailInto(right);

            // Assets: cell prefab + preview RenderTexture + preview rig ---------------------------
            BagItemCell cellPrefab = BuildCellPrefab();
            RenderTexture previewRt = CreatePreviewRt();
            previewImage.texture = previewRt;
            CharacterPreview preview = BuildPreviewRig(previewRt, out string previewNote);

            if (preview != null)
            {
                var rso = new SerializedObject(rotator);
                rso.FindProperty("preview").objectReferenceValue = preview;
                rso.ApplyModifiedPropertiesWithoutUndo();
            }

            // Wire the controller ----------------------------------------------------------------
            EquipmentManager equipmentManager = Object.FindFirstObjectByType<EquipmentManager>();

            var so = new SerializedObject(screen);
            so.FindProperty("inputReader").objectReferenceValue = FindInputReader();
            so.FindProperty("equipment").objectReferenceValue = equipmentManager;
            so.FindProperty("inventory").objectReferenceValue = Object.FindFirstObjectByType<Inventory>();
            // Resolve the SAME CharacterStats the gear modifies (the enemy dummy also has one, so a blind
            // scene search can grab the wrong instance and the stats panel would never update on equip).
            so.FindProperty("characterStats").objectReferenceValue = ResolveCharacterStats(equipmentManager);
            so.FindProperty("preview").objectReferenceValue = preview;
            so.FindProperty("root").objectReferenceValue = root;
            so.FindProperty("bagContent").objectReferenceValue = bagContent;
            so.FindProperty("bagCellPrefab").objectReferenceValue = cellPrefab;
            so.FindProperty("detailPanel").objectReferenceValue = detailPanel;
            so.FindProperty("filterNameLabel").objectReferenceValue = filterNameLabel;
            so.FindProperty("messageLabel").objectReferenceValue = messageLabel;
            SetList(so, "filterButtons", filterButtons);
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(host.scene);
            Selection.activeGameObject = host;

            var missing = new List<string>();
            if (so.FindProperty("inputReader").objectReferenceValue == null) missing.Add("inputReader (InputReader asset)");
            if (so.FindProperty("equipment").objectReferenceValue == null) missing.Add("equipment (EquipmentManager in scene)");
            if (so.FindProperty("inventory").objectReferenceValue == null) missing.Add("inventory (add Inventory to the Player)");
            if (so.FindProperty("characterStats").objectReferenceValue == null) missing.Add("characterStats (CharacterStats in scene)");
            if (so.FindProperty("preview").objectReferenceValue == null) missing.Add("preview (CharacterPreview — see note)");

            string report = "Built full-screen InventoryScreen under " + canvas.name + ".";
            if (missing.Count > 0)
            {
                report += "\n\nAssign in the Inspector on the InventoryScreen object:\n - " + string.Join("\n - ", missing);
            }
            if (!string.IsNullOrEmpty(previewNote))
            {
                report += "\n\nPreview: " + previewNote;
            }
            report += "\n\nRemember: put the player on its own layer and frame 'InventoryPreviewCamera' so the character reads well. Enter Play and press I.";
            Debug.Log("[InventoryScreenBuilder] " + report.Replace("\n", "  "), host);
            EditorUtility.DisplayDialog("Build Inventory Screen", report, "OK");
        }

        // ---- Composite builders ------------------------------------------------------------

        private static GearFilterButton BuildFilterButton(Transform parent, bool isAll, EquipmentSlot slot, string label)
        {
            GameObject go = BuildBasicButton($"Filter_{label}", parent, label, out Button button, out TMP_Text text);
            text.fontSize = 16f;   // 11 slot tabs in a row are tight; the full name also shows in the header below

            // Art-ready slot-icon overlay: empty now (falls back to the text label), fills in when a sprite
            // is assigned to the GearFilterButton's icon field.
            GameObject iconGo = NewUI("Icon", go.transform);
            Stretch(RT(iconGo));
            Image slotIcon = AddImage(iconGo, Color.white);
            slotIcon.raycastTarget = false;
            slotIcon.preserveAspect = true;
            slotIcon.enabled = false;

            GameObject hl = NewUI("Selected", go.transform);
            RectTransform hlRt = RT(hl);
            hlRt.anchorMin = new Vector2(0f, 0f);
            hlRt.anchorMax = new Vector2(1f, 0f);
            hlRt.pivot = new Vector2(0.5f, 0f);
            hlRt.sizeDelta = new Vector2(0f, 4f);
            hlRt.anchoredPosition = Vector2.zero;
            Image hlImg = AddImage(hl, MsgText);
            hlImg.raycastTarget = false;
            hlImg.enabled = false;

            var comp = go.AddComponent<GearFilterButton>();
            var so = new SerializedObject(comp);
            so.FindProperty("isAll").boolValue = isAll;
            so.FindProperty("slot").enumValueIndex = (int)slot;
            so.FindProperty("displayName").stringValue = label;
            so.FindProperty("button").objectReferenceValue = button;
            so.FindProperty("highlight").objectReferenceValue = hlImg;
            so.FindProperty("iconImage").objectReferenceValue = slotIcon;
            so.FindProperty("label").objectReferenceValue = text;
            so.ApplyModifiedPropertiesWithoutUndo();
            return comp;
        }

        private static void BuildTab(Transform parent, string label, bool interactable)
        {
            GameObject go = BuildBasicButton($"Tab_{label}", parent, label, out Button button, out TMP_Text text);
            button.interactable = interactable;
            if (!interactable)
            {
                text.color = DimText;
            }
        }

        private static BagItemCell BuildCellPrefab()
        {
            GameObject go = NewUI("BagItemCell", null);
            Image bg = AddImage(go, Color.white);   // white base so ColorBlock state colours render as-is
            var button = go.AddComponent<Button>();
            button.targetGraphic = bg;
            ApplyButtonColors(button);

            GameObject iconGo = NewUI("Icon", go.transform);
            RectTransform iconRt = RT(iconGo);
            iconRt.anchorMin = iconRt.anchorMax = iconRt.pivot = new Vector2(0.5f, 1f);
            iconRt.sizeDelta = new Vector2(96f, 96f);
            iconRt.anchoredPosition = new Vector2(0f, -14f);
            Image icon = AddImage(iconGo, Color.white);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.enabled = false;

            GameObject labelGo = NewUI("Label", go.transform);
            RectTransform lblRt = RT(labelGo);
            lblRt.anchorMin = new Vector2(0f, 0f);
            lblRt.anchorMax = new Vector2(1f, 0f);
            lblRt.pivot = new Vector2(0.5f, 0f);
            lblRt.sizeDelta = new Vector2(-8f, 46f);
            lblRt.anchoredPosition = new Vector2(0f, 6f);
            TMP_Text label = AddText(labelGo, "Item", 20f, BodyText, TextAlignmentOptions.Top);
            label.raycastTarget = false;

            // "Equipped" badge, top-right corner.
            GameObject badgeGo = NewUI("EquippedBadge", go.transform);
            RectTransform badgeRt = RT(badgeGo);
            badgeRt.anchorMin = badgeRt.anchorMax = badgeRt.pivot = new Vector2(1f, 1f);
            badgeRt.sizeDelta = new Vector2(34f, 24f);
            badgeRt.anchoredPosition = new Vector2(-6f, -6f);
            Image badgeBg = AddImage(badgeGo, MsgText);
            badgeBg.raycastTarget = false;
            GameObject badgeTextGo = NewUI("E", badgeGo.transform);
            Stretch(RT(badgeTextGo));
            TMP_Text badgeText = AddText(badgeTextGo, "E", 18f, new Color(0.1f, 0.1f, 0.12f, 1f), TextAlignmentOptions.Center);
            badgeText.raycastTarget = false;
            badgeGo.SetActive(false);

            var cell = go.AddComponent<BagItemCell>();
            var so = new SerializedObject(cell);
            so.FindProperty("button").objectReferenceValue = button;
            so.FindProperty("icon").objectReferenceValue = icon;
            so.FindProperty("label").objectReferenceValue = label;
            so.FindProperty("equippedBadge").objectReferenceValue = badgeGo;
            so.ApplyModifiedPropertiesWithoutUndo();

            EnsureFolder(PrefabDir);
            GameObject asset = PrefabUtility.SaveAsPrefabAsset(go, CellPrefabPath);
            Object.DestroyImmediate(go);
            return asset.GetComponent<BagItemCell>();
        }

        private static Transform BuildBagGrid(Transform parent)
        {
            GameObject scroll = NewUI("Scroll", parent);
            AddImage(scroll, new Color(0.06f, 0.07f, 0.09f, 0.6f));
            AddLayoutHeight(scroll, 300f, flexible: true);
            var rect = scroll.AddComponent<ScrollRect>();
            rect.horizontal = false;
            rect.vertical = true;
            rect.movementType = ScrollRect.MovementType.Clamped;
            rect.scrollSensitivity = 30f;

            GameObject viewport = NewUI("Viewport", scroll.transform);
            Stretch(RT(viewport));
            viewport.AddComponent<RectMask2D>();

            GameObject content = NewUI("Content", viewport.transform);
            RectTransform contentRt = RT(content);
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = Vector2.zero;

            var grid = content.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(150f, 172f);
            grid.spacing = new Vector2(12f, 12f);
            grid.padding = new RectOffset(12, 12, 12, 12);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.Flexible;

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            rect.viewport = RT(viewport);
            rect.content = contentRt;
            return content.transform;
        }

        // Populates the (already-created, vertically-laid-out) right column with the item detail widgets.
        private static ItemDetailPanel BuildDetailInto(GameObject column)
        {
            GameObject nameGo = NewUI("Name", column.transform);
            TMP_Text nameLabel = AddText(nameGo, string.Empty, 30f, HeaderText, TextAlignmentOptions.Left);
            AddLayoutHeight(nameGo, 40f, flexible: false);

            // Header row: item icon beside its slot/description.
            GameObject header = NewUI("Header", column.transform);
            var headerHl = header.AddComponent<HorizontalLayoutGroup>();
            headerHl.spacing = 12f;
            headerHl.childAlignment = TextAnchor.UpperLeft;
            headerHl.childControlWidth = true;
            headerHl.childControlHeight = true;
            headerHl.childForceExpandWidth = false;
            headerHl.childForceExpandHeight = false;
            AddLayoutHeight(header, 120f, flexible: false);

            GameObject iconGo = NewUI("Icon", header.transform);
            Image iconImage = AddImage(iconGo, Color.white);
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            iconImage.enabled = false;
            var iconLe = iconGo.AddComponent<LayoutElement>();
            iconLe.preferredWidth = 108f;
            iconLe.preferredHeight = 108f;

            GameObject descGo = NewUI("Desc", header.transform);
            TMP_Text descLabel = AddText(descGo, string.Empty, 20f, DimText, TextAlignmentOptions.TopLeft);
            var descLe = descGo.AddComponent<LayoutElement>();
            descLe.flexibleWidth = 1f;

            // Job requirement row (crest badge + label); hidden unless the item is job-restricted.
            GameObject jobRow = NewUI("JobReq", column.transform);
            var jobHl = jobRow.AddComponent<HorizontalLayoutGroup>();
            jobHl.spacing = 8f;
            jobHl.childAlignment = TextAnchor.MiddleLeft;
            jobHl.childControlWidth = true;
            jobHl.childControlHeight = true;
            jobHl.childForceExpandWidth = false;
            jobHl.childForceExpandHeight = false;
            AddLayoutHeight(jobRow, 34f, flexible: false);

            Color jobColor = new Color(0.90f, 0.35f, 0.35f, 1f);   // recoloured at runtime by ItemDetailPanel
            GameObject crestGo = NewUI("Crest", jobRow.transform);
            Image jobReqBadge = AddImage(crestGo, jobColor);
            jobReqBadge.raycastTarget = false;
            var crestLe = crestGo.AddComponent<LayoutElement>();
            crestLe.preferredWidth = 26f;
            crestLe.preferredHeight = 30f;

            GameObject jobLabelGo = NewUI("Label", jobRow.transform);
            TMP_Text jobReqLabel = AddText(jobLabelGo, string.Empty, 20f, jobColor, TextAlignmentOptions.Left);
            var jobLabelLe = jobLabelGo.AddComponent<LayoutElement>();
            jobLabelLe.flexibleWidth = 1f;
            jobRow.SetActive(false);

            GameObject statsGo = NewUI("Stats", column.transform);
            TMP_Text statsLabel = AddText(statsGo, string.Empty, 22f, BodyText, TextAlignmentOptions.TopLeft);
            var statsLe = statsGo.AddComponent<LayoutElement>();
            statsLe.flexibleHeight = 1f;

            var comp = column.AddComponent<ItemDetailPanel>();
            var so = new SerializedObject(comp);
            so.FindProperty("iconImage").objectReferenceValue = iconImage;
            so.FindProperty("nameLabel").objectReferenceValue = nameLabel;
            so.FindProperty("descLabel").objectReferenceValue = descLabel;
            so.FindProperty("jobReqRoot").objectReferenceValue = jobRow;
            so.FindProperty("jobReqBadge").objectReferenceValue = jobReqBadge;
            so.FindProperty("jobReqLabel").objectReferenceValue = jobReqLabel;
            so.FindProperty("statsLabel").objectReferenceValue = statsLabel;
            so.ApplyModifiedPropertiesWithoutUndo();
            return comp;
        }

        // Centered-label button (tabs, filters).
        private static GameObject BuildBasicButton(string name, Transform parent, string label, out Button button, out TMP_Text text)
        {
            GameObject go = NewUI(name, parent);
            Image bg = AddImage(go, Color.white);   // white base so ColorBlock state colours render as-is
            button = go.AddComponent<Button>();
            button.targetGraphic = bg;
            ApplyButtonColors(button);

            GameObject labelGo = NewUI("Label", go.transform);
            Stretch(RT(labelGo));
            text = AddText(labelGo, label, 24f, BodyText, TextAlignmentOptions.Center);
            return go;
        }

        private static CharacterPreview BuildPreviewRig(RenderTexture rt, out string note)
        {
            note = null;
            Transform player = null;
            var eq = Object.FindFirstObjectByType<EquipmentManager>();
            if (eq != null) player = eq.transform;
            if (player == null)
            {
                var inv = Object.FindFirstObjectByType<Inventory>();
                if (inv != null) player = inv.transform;
            }
            if (player == null)
            {
                note = "no player (EquipmentManager/Inventory) in scene — create the preview camera + CharacterPreview manually (spec §10).";
                return null;
            }

            Transform existing = player.Find("InventoryPreviewCamera");
            if (existing != null && existing.TryGetComponent(out CharacterPreview reused))
            {
                return reused;
            }

            var camGo = new GameObject("InventoryPreviewCamera");
            Undo.RegisterCreatedObjectUndo(camGo, "Build Inventory Screen");
            camGo.transform.SetParent(player, false);
            camGo.transform.localPosition = new Vector3(1.6f, 1.0f, 2.6f);
            camGo.transform.localRotation = Quaternion.LookRotation(new Vector3(-1.6f, -0.1f, -2.6f).normalized, Vector3.up);

            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.10f, 0.11f, 0.14f, 1f);
            cam.fieldOfView = 30f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 25f;
            cam.targetTexture = rt;
            cam.enabled = false;

            int layer = player.gameObject.layer;
            if (layer != 0)
            {
                cam.cullingMask = 1 << layer;
            }
            else
            {
                note = "player is on the Default layer, so the preview camera renders the whole world. Put the player on its own layer and set 'InventoryPreviewCamera' Culling Mask to it (spec §10).";
            }

            var lightGo = new GameObject("InventoryPreviewLight");
            lightGo.transform.SetParent(camGo.transform, false);
            lightGo.transform.localRotation = Quaternion.Euler(30f, 10f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            if (layer != 0)
            {
                light.cullingMask = 1 << layer;
            }

            var preview = camGo.AddComponent<CharacterPreview>();
            var so = new SerializedObject(preview);
            so.FindProperty("previewCamera").objectReferenceValue = cam;
            so.FindProperty("orbitPivot").objectReferenceValue = player;
            so.ApplyModifiedPropertiesWithoutUndo();
            return preview;
        }

        private static RenderTexture CreatePreviewRt()
        {
            EnsureFolder(RenderDir);
            RenderTexture existing = AssetDatabase.LoadAssetAtPath<RenderTexture>(PreviewRtPath);
            if (existing != null)
            {
                return existing;
            }
            var rt = new RenderTexture(900, 1200, 16, RenderTextureFormat.ARGB32) { name = "InventoryPreview" };
            AssetDatabase.CreateAsset(rt, PreviewRtPath);
            return rt;
        }

        // ---- Primitive helpers -------------------------------------------------------------

        private static void ApplyButtonColors(Button button)
        {
            // NOTE: the target graphic is white so these state colours render directly (normal = base tint,
            // highlighted/selected = lighter). A dark base image would only ever darken on hover.
            ColorBlock cb = button.colors;
            cb.normalColor = WidgetBg;
            cb.highlightedColor = WidgetHi;
            cb.pressedColor = WidgetDown;
            cb.selectedColor = WidgetHi;
            cb.disabledColor = new Color(0.12f, 0.13f, 0.15f, 1f);
            cb.fadeDuration = 0.06f;
            button.colors = cb;
        }

        private static void AddVerticalLayout(GameObject go, float spacing, int pad)
        {
            var vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(pad, pad, pad, pad);
            vlg.spacing = spacing;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperCenter;
        }

        private static void AddHorizontalLayout(GameObject go, float spacing)
        {
            var hl = go.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = spacing;
            hl.childControlWidth = true;
            hl.childControlHeight = true;
            hl.childForceExpandWidth = true;
            hl.childForceExpandHeight = true;
            hl.childAlignment = TextAnchor.MiddleCenter;
        }

        private static void AddLayoutHeight(GameObject go, float height, bool flexible)
        {
            var le = go.AddComponent<LayoutElement>();
            if (flexible)
            {
                le.minHeight = height;
                le.flexibleHeight = 1f;
            }
            else
            {
                le.preferredHeight = height;
                le.minHeight = height;
            }
        }

        private static Image AddImage(GameObject go, Color color)
        {
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        private static TMP_Text AddText(GameObject go, string content, float size, Color color, TextAlignmentOptions align)
        {
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = content;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.overflowMode = TextOverflowModes.Ellipsis;
            return t;
        }

        private static void SetList<T>(SerializedObject so, string prop, IList<T> values) where T : Object
        {
            SerializedProperty arr = so.FindProperty(prop);
            arr.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
            {
                arr.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        private static GameObject NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }
            return go;
        }

        private static RectTransform RT(GameObject go) => (RectTransform)go.transform;

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void BandTop(RectTransform rt, float height, float inset, float topMargin)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(-2f * inset, height);
            rt.anchoredPosition = new Vector2(0f, -topMargin);
        }

        private static void BandBottom(RectTransform rt, float height, float inset, float bottomMargin)
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(-2f * inset, height);
            rt.anchoredPosition = new Vector2(0f, bottomMargin);
        }

        private static void FractionColumn(RectTransform rt, float xMin, float xMax, float left, float right, float top, float bottom)
        {
            rt.anchorMin = new Vector2(xMin, 0f);
            rt.anchorMax = new Vector2(xMax, 1f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        // Prefer the CharacterStats the EquipmentManager actually applies gear to (its serialized `stats`),
        // then the one on the player hierarchy, and only fall back to a scene-wide search as a last resort.
        private static CharacterStats ResolveCharacterStats(EquipmentManager equipment)
        {
            if (equipment != null)
            {
                var eso = new SerializedObject(equipment);
                SerializedProperty prop = eso.FindProperty("stats");
                if (prop != null && prop.objectReferenceValue is CharacterStats fromManager && fromManager != null)
                {
                    return fromManager;
                }

                CharacterStats onPlayer = equipment.GetComponentInParent<CharacterStats>();
                if (onPlayer != null)
                {
                    return onPlayer;
                }
            }

            return Object.FindFirstObjectByType<CharacterStats>();
        }

        private static InputReader FindInputReader()
        {
            string[] guids = AssetDatabase.FindAssets("t:InputReader");
            return guids.Length == 0 ? null
                : AssetDatabase.LoadAssetAtPath<InputReader>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }
            var go = new GameObject("EventSystem", typeof(EventSystem));
            var module = go.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
            Undo.RegisterCreatedObjectUndo(go, "Build Inventory Screen");
        }
    }
}

// Editor tool: builds a health/stamina HUD from AriaGUI bar prefabs and wires it to the player.
// One-time convenience — run "Project Alpha ▸ Build Player HUD", then it can be deleted.
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEditor;

namespace ProjectAlpha.EditorTools
{
    public static class HudBuilder
    {
        private const string BarPrefabPath = "Assets/Honeti/AriaGUI/Prefabs/Bars/BarSimple.prefab";

        [MenuItem("Project Alpha/Build Player HUD")]
        public static void BuildHud()
        {
            // The player is the object with a StaminaController (the dummy has Health but no Stamina).
            StaminaController stamina = Object.FindFirstObjectByType<StaminaController>();
            if (stamina == null)
            {
                Debug.LogError("[HudBuilder] No StaminaController found in the scene. Open Scene_TestArena " +
                               "with the wired player, then run this again.");
                return;
            }
            HealthController health = stamina.GetComponent<HealthController>();

            Canvas canvas = GetOrCreateCanvas();
            EnsureEventSystem();

            GameObject hudGo = new GameObject("PlayerHUD", typeof(RectTransform));
            hudGo.transform.SetParent(canvas.transform, false);
            PlayerHud hud = hudGo.AddComponent<PlayerHud>();

            ResourceBarView healthBar = CreateBar(hudGo.transform, "HealthBar", new Vector2(20f, -20f));
            ResourceBarView staminaBar = CreateBar(hudGo.transform, "StaminaBar", new Vector2(20f, -58f));

            SerializedObject so = new SerializedObject(hud);
            so.FindProperty("health").objectReferenceValue = health;
            so.FindProperty("stamina").objectReferenceValue = stamina;
            so.FindProperty("healthBar").objectReferenceValue = healthBar;
            so.FindProperty("staminaBar").objectReferenceValue = staminaBar;
            so.ApplyModifiedProperties();

            Selection.activeGameObject = hudGo;
            EditorUtility.SetDirty(hudGo);
            Debug.Log("[HudBuilder] HUD built and wired. Reposition/recolor as desired, then save the scene.", hudGo);
        }

        private static Canvas GetOrCreateCanvas()
        {
            Canvas existing = Object.FindFirstObjectByType<Canvas>();
            if (existing != null)
            {
                return existing;
            }

            GameObject canvasGo = new GameObject("HUD_Canvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            return canvas;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }
            // Input System module (project uses the Input System, not legacy input).
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private static ResourceBarView CreateBar(Transform parent, string barName, Vector2 anchoredPosition)
        {
            GameObject barGo;
            Slider slider;

            GameObject barPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BarPrefabPath);
            if (barPrefab != null)
            {
                barGo = (GameObject)PrefabUtility.InstantiatePrefab(barPrefab, parent);
                barGo.name = barName;
                slider = barGo.GetComponentInChildren<Slider>();
            }
            else
            {
                barGo = DefaultControls.CreateSlider(new DefaultControls.Resources());
                barGo.name = barName;
                barGo.transform.SetParent(parent, false);
                slider = barGo.GetComponent<Slider>();
                Debug.LogWarning($"[HudBuilder] AriaGUI bar not found at {BarPrefabPath}; used a plain Slider " +
                                 $"for {barName}. Skin it later.");
            }

            RectTransform rt = barGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = new Vector2(320f, 30f);

            if (slider != null)
            {
                slider.minValue = 0f;
                slider.maxValue = 1f;
                slider.wholeNumbers = false;
                slider.interactable = false;   // display bar, not an input control
                slider.value = 1f;
                if (slider.handleRect != null)
                {
                    slider.handleRect.gameObject.SetActive(false); // hide the draggable knob
                }
            }
            else
            {
                Debug.LogWarning($"[HudBuilder] {barName} has no Slider; wire ResourceBarView manually.");
            }

            ResourceBarView view = barGo.AddComponent<ResourceBarView>();
            SerializedObject so = new SerializedObject(view);
            if (slider != null)
            {
                so.FindProperty("slider").objectReferenceValue = slider;
            }
            so.ApplyModifiedProperties();
            return view;
        }
    }
}

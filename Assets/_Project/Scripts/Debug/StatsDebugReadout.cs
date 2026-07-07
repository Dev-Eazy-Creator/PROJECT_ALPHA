// On-screen IMGUI readout of live stats and stamina for Play Mode testing. Not shipping UI.
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectAlpha
{
    public class StatsDebugReadout : MonoBehaviour
    {
        [SerializeField] private CharacterStats stats;
        [SerializeField] private StaminaController staminaController;
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerClimb climb;

        [Header("Debug modifier keys")]
        [Tooltip("Adds a temporary +50% MaxStamina PercentAdd modifier.")]
        [SerializeField] private Key addModifierKey = Key.M;
        [Tooltip("Removes all modifiers from the debug source.")]
        [SerializeField] private Key removeModifierKey = Key.N;

        // Distinct source object so debug modifiers detach cleanly, exactly like gear/augments.
        private readonly object debugSource = new object();

        private void Update()
        {
            if (stats == null || stats.Attributes == null || Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current[addModifierKey].wasPressedThisFrame)
            {
                stats.Attributes.AddModifier(
                    new StatModifier(AttributeType.MaxStamina, StatModifierType.PercentAdd, 0.5f, debugSource));
            }

            if (Keyboard.current[removeModifierKey].wasPressedThisFrame)
            {
                stats.Attributes.RemoveAllModifiersFromSource(debugSource);
            }
        }

        private void OnGUI()
        {
            if (stats == null)
            {
                return;
            }

            GUIStyle style = LabelStyle();

            GUILayout.BeginArea(new Rect(10, 10, 470, 540), GUI.skin.box);
            GUILayout.Label("<b>STATS DEBUG</b>", style);

            if (staminaController != null)
            {
                ResourcePool stamina = staminaController.Stamina;
                GUILayout.Label($"Stamina: {stamina.Current:0} / {stamina.Max:0}", style);
                DrawBar(stamina.Normalized, new Color(0.3f, 0.8f, 0.3f));
            }

            GUILayout.Space(6);
            GUILayout.Label($"IsSprinting:   {(movement != null ? movement.IsSprinting.ToString() : "n/a")}", style);
            GUILayout.Label($"IsClimbing:    {(climb != null ? climb.IsClimbing.ToString() : "n/a")}", style);
            GUILayout.Label($"SprintAllowed: {(movement != null ? movement.SprintAllowed.ToString() : "n/a")}", style);

            GUILayout.Space(6);
            GUILayout.Label($"MoveSpeed:        {stats.GetValue(AttributeType.MoveSpeed):0.##}", style);
            GUILayout.Label($"MaxStamina:       {stats.GetValue(AttributeType.MaxStamina):0.##}", style);
            GUILayout.Label($"StaminaRegenRate: {stats.GetValue(AttributeType.StaminaRegenRate):0.##}", style);
            GUILayout.Label($"PhysicalAttack:   {stats.GetValue(AttributeType.PhysicalAttack):0.##}", style);
            GUILayout.Label($"PhysicalDefense:  {stats.GetValue(AttributeType.PhysicalDefense):0.##}", style);
            GUILayout.Label($"MaxHealth:        {stats.GetValue(AttributeType.MaxHealth):0.##}", style);

            GUILayout.Space(6);
            GUILayout.Label($"[{addModifierKey}] +50% MaxStamina   [{removeModifierKey}] clear debug mods", style);
            GUILayout.EndArea();
        }

        private static void DrawBar(float normalized, Color fill)
        {
            Rect rect = GUILayoutUtility.GetRect(430, 20);
            GUI.color = new Color(0f, 0f, 0f, 0.4f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = fill;
            Rect fillRect = new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(normalized), rect.height);
            GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private GUIStyle cachedStyle;

        private GUIStyle LabelStyle()
        {
            if (cachedStyle == null)
            {
                cachedStyle = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 16 };
            }
            return cachedStyle;
        }
    }
}

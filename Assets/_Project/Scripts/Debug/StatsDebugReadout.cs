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

            GUILayout.BeginArea(new Rect(10, 10, 320, 260), GUI.skin.box);
            GUILayout.Label("<b>STATS DEBUG</b>", RichLabel());

            if (staminaController != null)
            {
                ResourcePool stamina = staminaController.Stamina;
                GUILayout.Label($"Stamina: {stamina.Current:0} / {stamina.Max:0}");
                DrawBar(stamina.Normalized, new Color(0.3f, 0.8f, 0.3f));
            }

            GUILayout.Space(4);
            GUILayout.Label($"IsSprinting:  {(movement != null ? movement.IsSprinting.ToString() : "n/a")}");
            GUILayout.Label($"IsClimbing:   {(climb != null ? climb.IsClimbing.ToString() : "n/a")}");
            GUILayout.Label($"SprintAllowed:{(movement != null ? movement.SprintAllowed.ToString() : "n/a")}");

            GUILayout.Space(4);
            GUILayout.Label($"MoveSpeed:        {stats.GetValue(AttributeType.MoveSpeed):0.##}");
            GUILayout.Label($"MaxStamina:       {stats.GetValue(AttributeType.MaxStamina):0.##}");
            GUILayout.Label($"StaminaRegenRate: {stats.GetValue(AttributeType.StaminaRegenRate):0.##}");
            GUILayout.Label($"PhysicalAttack:   {stats.GetValue(AttributeType.PhysicalAttack):0.##}");
            GUILayout.Label($"MaxHealth:        {stats.GetValue(AttributeType.MaxHealth):0.##}");

            GUILayout.Space(4);
            GUILayout.Label($"[{addModifierKey}] +50% MaxStamina   [{removeModifierKey}] clear debug mods");
            GUILayout.EndArea();
        }

        private static void DrawBar(float normalized, Color fill)
        {
            Rect rect = GUILayoutUtility.GetRect(300, 14);
            GUI.color = new Color(0f, 0f, 0f, 0.4f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = fill;
            Rect fillRect = new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(normalized), rect.height);
            GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private static GUIStyle RichLabel()
        {
            return new GUIStyle(GUI.skin.label) { richText = true };
        }
    }
}

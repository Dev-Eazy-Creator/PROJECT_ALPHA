// Data container for a single vocation. One asset per vocation.
using UnityEngine;

namespace ProjectAlpha
{
    [CreateAssetMenu(fileName = "New_VocationData", menuName = "Game/Vocation Data")]
    public class VocationData : ScriptableObject
    {
        public VocationType Vocation;
        public string DisplayName;
        public GameObject WeaponPrefab;
        public AnimatorOverrideController CombatAnimatorOverride;
        [Range(0.5f, 2f)] public float MoveSpeedModifier = 1f;
    }
}

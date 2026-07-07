// Data container for a single job: identity (weapon, animator, name) plus its embedded base stats.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace ProjectAlpha
{
    [CreateAssetMenu(fileName = "New_JobData", menuName = "Game/Job Data")]
    public class JobData : ScriptableObject
    {
        [Header("Identity")]
        [FormerlySerializedAs("Vocation")] public JobType Job;
        public string DisplayName;
        public GameObject WeaponPrefab;
        public AnimatorOverrideController CombatAnimatorOverride;

        [Tooltip("Superseded by the MoveSpeed attribute; kept for Part 1 backward compatibility.")]
        [Range(0.5f, 2f)] public float MoveSpeedModifier = 1f;

        [Header("Base Stats")]
        [Tooltip("Starting base value for each attribute. Include all 12 for an authored job.")]
        public List<AttributeBaseValue> BaseStats = new List<AttributeBaseValue>();
    }
}

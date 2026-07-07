// Data container for a single job. One asset per job.
using UnityEngine;
using UnityEngine.Serialization;

namespace ProjectAlpha
{
    [CreateAssetMenu(fileName = "New_JobData", menuName = "Game/Job Data")]
    public class JobData : ScriptableObject
    {
        [FormerlySerializedAs("Vocation")] public JobType Job;
        public string DisplayName;
        public GameObject WeaponPrefab;
        public AnimatorOverrideController CombatAnimatorOverride;
        [Range(0.5f, 2f)] public float MoveSpeedModifier = 1f;
    }
}

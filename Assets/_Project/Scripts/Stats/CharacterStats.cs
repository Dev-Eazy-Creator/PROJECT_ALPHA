// Per-character owner of the AttributeSet. Builds stats from the active job's profile
// and re-applies them when the job changes. The single entry point other systems query.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAlpha
{
    // Runs after JobManager (-100) so CurrentJob is set, and before augments/pools so they read final stats.
    [DefaultExecutionOrder(-90)]
    public class CharacterStats : MonoBehaviour
    {
        [Tooltip("Player uses a JobManager. Leave empty for non-job characters (e.g. the enemy dummy).")]
        [SerializeField] private JobManager jobManager;
        [Tooltip("Base stats used when there is no JobManager/CurrentJob (e.g. the enemy dummy).")]
        [SerializeField] private List<AttributeBaseValue> baseStatsWhenNoJob = new List<AttributeBaseValue>();

        // One AttributeSet is kept for the character's lifetime; a job change resets base values in place
        // (Attribute objects persist, so pool subscriptions to their OnValueChanged stay valid).
        public AttributeSet Attributes { get; private set; }

        // Fires after a job change rebuilds base values, so resource pools resize to the new maxima.
        public event Action OnStatsRebuilt;

        private JobData currentJobSource;

        private void Awake()
        {
            Attributes = new AttributeSet(ResolveBaseValues());

            if (jobManager != null)
            {
                jobManager.OnJobChanged += HandleJobChanged;
            }
        }

        private void OnDestroy()
        {
            if (jobManager != null)
            {
                jobManager.OnJobChanged -= HandleJobChanged;
            }
        }

        public float GetValue(AttributeType type)
        {
            return Attributes != null ? Attributes.GetValue(type) : 0f;
        }

        private IEnumerable<AttributeBaseValue> ResolveBaseValues()
        {
            if (jobManager != null && jobManager.CurrentJob != null)
            {
                currentJobSource = jobManager.CurrentJob;
                if (currentJobSource.BaseStats == null || currentJobSource.BaseStats.Count == 0)
                {
                    Debug.LogWarning($"[CharacterStats] Job '{currentJobSource.DisplayName}' has empty BaseStats; " +
                                     "attributes default to 0.", this);
                }
                return currentJobSource.BaseStats;
            }

            currentJobSource = null;
            if (jobManager != null)
            {
                Debug.LogWarning("[CharacterStats] JobManager has no CurrentJob; using fallback base stats.", this);
            }
            return baseStatsWhenNoJob;
        }

        private void HandleJobChanged(JobData newJob)
        {
            // Drop the previous job's modifiers (if any were sourced from it), then reset base values.
            if (currentJobSource != null)
            {
                Attributes.RemoveAllModifiersFromSource(currentJobSource);
            }

            currentJobSource = newJob;
            ApplyBaseValues(newJob != null ? newJob.BaseStats : baseStatsWhenNoJob);
            OnStatsRebuilt?.Invoke();
        }

        // Resets every attribute to 0, then overlays the authored list — keeps existing (augment/gear) modifiers.
        private void ApplyBaseValues(IEnumerable<AttributeBaseValue> baseValues)
        {
            foreach (AttributeType type in Enum.GetValues(typeof(AttributeType)))
            {
                Attributes.Get(type)?.SetBaseValue(0f);
            }

            if (baseValues != null)
            {
                foreach (AttributeBaseValue baseValue in baseValues)
                {
                    Attributes.Get(baseValue.Attribute)?.SetBaseValue(baseValue.BaseValue);
                }
            }
        }
    }
}

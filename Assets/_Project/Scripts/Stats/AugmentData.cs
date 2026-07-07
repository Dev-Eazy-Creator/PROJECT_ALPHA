// Authoring asset for a single augment. Carries EITHER a stat modifier OR a behavioral effect.
// Part 2 implements/consumes only the stat-modifier path; behavioral effects are data-only.
using UnityEngine;

namespace ProjectAlpha
{
    public enum AugmentEffectKind
    {
        StatModifier,     // implemented in Part 2: applies a StatModifier to the AttributeSet
        Behavioral        // data-only in Part 2: a tagged effect a future system will query
    }

    // Tags for behavioral augments. Extend as systems that consume them are built.
    // None of these are read by any system in Part 2 — they exist so augment assets can be authored.
    public enum AugmentBehavior
    {
        None,
        HalveBlockStaminaCost,     // e.g. an Adamance-style augment (needs the block system)
        ReduceSkillStaminaCost,    // e.g. a Proficiency-style augment (needs skills)
        FasterStaminaRecovery,     // e.g. a Grit-style augment (could hook regen later)
        SurviveFatalBlow           // e.g. a Tenacity-style augment (needs the health/damage system)
    }

    [CreateAssetMenu(fileName = "New_Augment", menuName = "Game/Stats/Augment")]
    public class AugmentData : ScriptableObject
    {
        public string DisplayName;
        [Tooltip("Job whose leveling unlocks this augment. It can then be equipped on ANY job.")]
        public JobType SourceJob;

        public AugmentEffectKind EffectKind;

        [Header("Used when EffectKind == StatModifier")]
        public AttributeType TargetAttribute;
        public StatModifierType ModifierType;
        public float ModifierValue;

        [Header("Used when EffectKind == Behavioral (data-only in Part 2)")]
        public AugmentBehavior Behavior;
    }
}

// One stat contribution from a piece of gear, with a per-upgrade-level increment (DD1 enhancement).
namespace ProjectAlpha
{
    [System.Serializable]
    public struct GearModifier
    {
        public AttributeType Attribute;
        public StatModifierType Type;   // Flat / PercentAdd / PercentMult (reused from Part 2)
        public float BaseValue;         // contribution at upgrade level 0
        public float PerLevelValue;     // added per upgrade level
    }
}

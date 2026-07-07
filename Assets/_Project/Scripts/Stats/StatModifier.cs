// A single change applied to one attribute. Plain C# object, not a MonoBehaviour or SO.
namespace ProjectAlpha
{
    public enum StatModifierType
    {
        Flat,           // += value
        PercentAdd,     // sums with other PercentAdd, then applies once (e.g. +10% and +5% = +15%)
        PercentMult     // multiplies sequentially (e.g. *1.10 then *1.05)
    }

    public class StatModifier
    {
        public AttributeType Attribute { get; }
        public StatModifierType Type { get; }
        public float Value { get; }
        public object Source { get; }   // who added this (job SO, equipment, buff) — for clean removal

        public StatModifier(AttributeType attribute, StatModifierType type, float value, object source)
        {
            Attribute = attribute;
            Type = type;
            Value = value;
            Source = source;
        }
    }
}

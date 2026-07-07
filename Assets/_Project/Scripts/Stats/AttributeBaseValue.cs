// One attribute's authored base value. Serialized on JobData (and the enemy dummy) as the stat source.
using System;

namespace ProjectAlpha
{
    [Serializable]
    public struct AttributeBaseValue
    {
        public AttributeType Attribute;
        public float BaseValue;
    }
}

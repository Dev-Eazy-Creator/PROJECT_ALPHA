// A dictionary of all attributes for one character. The queryable stat block.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAlpha
{
    public class AttributeSet
    {
        private readonly Dictionary<AttributeType, Attribute> attributes = new Dictionary<AttributeType, Attribute>();

        // Builds every AttributeType (default 0), then overlays the authored base values.
        public AttributeSet(IEnumerable<AttributeBaseValue> baseValues)
        {
            foreach (AttributeType type in Enum.GetValues(typeof(AttributeType)))
            {
                attributes[type] = new Attribute(type, 0f);
            }

            if (baseValues != null)
            {
                foreach (AttributeBaseValue baseValue in baseValues)
                {
                    if (attributes.TryGetValue(baseValue.Attribute, out Attribute attribute))
                    {
                        attribute.SetBaseValue(baseValue.BaseValue);
                    }
                }
            }
        }

        public Attribute Get(AttributeType type)
        {
            if (attributes.TryGetValue(type, out Attribute attribute))
            {
                return attribute;
            }

            Debug.LogError($"[AttributeSet] Attribute {type} was not initialized.");
            return null;
        }

        public float GetValue(AttributeType type)
        {
            Attribute attribute = Get(type);
            return attribute != null ? attribute.Value : 0f;
        }

        // "What if" value for one attribute with a modifier source swapped out for another set. Read-only.
        public float PreviewValue(AttributeType type, object removeSource, IEnumerable<StatModifier> addModifiers)
        {
            Attribute attribute = Get(type);
            return attribute != null ? attribute.PreviewValue(removeSource, addModifiers) : 0f;
        }

        public void AddModifier(StatModifier modifier)
        {
            if (modifier == null)
            {
                return;
            }
            Get(modifier.Attribute)?.AddModifier(modifier);
        }

        public void RemoveModifier(StatModifier modifier)
        {
            if (modifier == null)
            {
                return;
            }
            Get(modifier.Attribute)?.RemoveModifier(modifier);
        }

        public void RemoveAllModifiersFromSource(object source)
        {
            foreach (Attribute attribute in attributes.Values)
            {
                attribute.RemoveAllModifiersFromSource(source);
            }
        }
    }
}

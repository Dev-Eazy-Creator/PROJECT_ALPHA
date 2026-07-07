// Shows a hovered/focused item and a DD-style "current ► new" comparison of the character's resulting
// totals if it were equipped (green = higher, red = lower). Read-only: projections are computed
// analytically from gear data and never mutate the live stats. When item is null it shows current
// totals only. Flat modifiers project exactly (the common case); percent modifiers aren't projected.
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectAlpha
{
    public class ItemDetailPanel : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text descLabel;
        [Tooltip("Row shown only when the item is job-restricted (crest + \"Requires: <job>\").")]
        [SerializeField] private GameObject jobReqRoot;
        [SerializeField] private Image jobReqBadge;
        [SerializeField] private TMP_Text jobReqLabel;
        [SerializeField] private TMP_Text statsLabel;

        [Tooltip("Optional per-job crest sprites. When a required job has one, the crest shows the sprite " +
                 "(white when usable, red-tinted when locked) instead of a plain coloured box.")]
        [SerializeField] private JobCrest[] jobCrests;

        [System.Serializable]
        private struct JobCrest
        {
            public JobType job;
            public Sprite sprite;
        }

        private const string GoodHex = "#73D973";
        private const string BadHex = "#E67373";

        private static readonly Color JobOk = new Color(0.85f, 0.72f, 0.29f);      // requirement met
        private static readonly Color JobLocked = new Color(0.90f, 0.35f, 0.35f);  // current job can't equip

        // Reused across hovers so building the stat table doesn't allocate a StringBuilder each time.
        private static readonly StringBuilder Builder = new StringBuilder(256);

        // Core stats shown, in order, as current ► projected.
        private static readonly AttributeType[] CoreStats =
        {
            AttributeType.PhysicalAttack,
            AttributeType.MagickAttack,
            AttributeType.PhysicalDefense,
            AttributeType.MagickDefense,
            AttributeType.MaxHealth,
            AttributeType.MaxStamina,
            AttributeType.StaggerResistance,
            AttributeType.KnockdownResistance,
        };

        public void Clear()
        {
            Set(nameLabel, string.Empty);
            Set(descLabel, string.Empty);
            Set(statsLabel, string.Empty);
            if (iconImage != null)
            {
                iconImage.enabled = false;
            }
            if (jobReqRoot != null)
            {
                jobReqRoot.SetActive(false);
            }
        }

        // item may be null → show the character's current totals with no projection.
        public void Show(ItemInstance item, EquipmentManager equipment, CharacterStats stats)
        {
            bool hasItem = item != null && item.Data != null;

            if (iconImage != null)
            {
                Sprite sprite = null;
                if (hasItem)
                {
                    sprite = item.Data.Icon != null ? item.Data.Icon : PlaceholderIcon.For(item.Data.DisplayName);
                }
                iconImage.sprite = sprite;
                iconImage.color = Color.white;
                iconImage.enabled = sprite != null;
            }

            Set(nameLabel, hasItem ? NameOf(item) : "Character");
            Set(descLabel, hasItem ? DescOf(item) : "Current stats");
            Set(statsLabel, BuildTable(item, equipment, stats));

            ShowJobRequirement(item, equipment, hasItem);
        }

        // Job crest + "Requires: <job>", gold when the current job qualifies, red when it can't equip.
        // Hidden entirely when the item has no job restriction (AllowedJobs empty = any job).
        private void ShowJobRequirement(ItemInstance item, EquipmentManager equipment, bool hasItem)
        {
            if (jobReqRoot == null)
            {
                return;
            }

            List<JobType> jobs = hasItem ? item.Data.AllowedJobs : null;
            if (jobs == null || jobs.Count == 0)
            {
                jobReqRoot.SetActive(false);
                return;
            }

            bool satisfied = equipment == null || equipment.CanEquip(item, out _);
            Color color = satisfied ? JobOk : JobLocked;

            if (jobReqLabel != null)
            {
                jobReqLabel.text = "Requires: " + string.Join(" / ", jobs);
                jobReqLabel.color = color;
            }
            if (jobReqBadge != null)
            {
                Sprite crest = CrestFor(jobs);
                jobReqBadge.sprite = crest;
                // With real crest art: show it, tinting red when the current job can't use it. Without
                // art: fall back to the gold/red coloured box.
                jobReqBadge.color = crest != null
                    ? (satisfied ? Color.white : new Color(1f, 0.55f, 0.55f))
                    : color;
            }
            jobReqRoot.SetActive(true);
        }

        private Sprite CrestFor(List<JobType> jobs)
        {
            if (jobCrests == null || jobs == null)
            {
                return null;
            }
            foreach (JobType job in jobs)
            {
                foreach (JobCrest crest in jobCrests)
                {
                    if (crest.job == job && crest.sprite != null)
                    {
                        return crest.sprite;
                    }
                }
            }
            return null;
        }

        private static string BuildTable(ItemInstance item, EquipmentManager equipment, CharacterStats stats)
        {
            bool hasItem = item != null && item.Data != null;
            AttributeSet attributes = stats != null ? stats.Attributes : null;

            // The item currently in the candidate's slot; equipping the candidate would remove its
            // modifiers and add the candidate's. If the candidate IS that item (already equipped),
            // displaced == candidate, so the projection equals current — equipping it is a no-op.
            ItemInstance displaced = null;
            List<StatModifier> candidateMods = null;
            if (hasItem)
            {
                if (equipment != null)
                {
                    equipment.Equipped.TryGetValue(item.Data.Slot, out displaced);
                }
                candidateMods = new List<StatModifier>(item.BuildModifiers());
            }

            StringBuilder sb = Builder;
            sb.Clear();
            foreach (AttributeType attr in CoreStats)
            {
                float current = stats != null ? stats.GetValue(attr) : 0f;
                sb.Append(ShortName(attr)).Append("<pos=56%>").Append(Num(current));

                if (hasItem)
                {
                    // Exact across flat + percent: recompute with the slot's item swapped for the candidate.
                    float projected = attributes != null
                        ? attributes.PreviewValue(attr, displaced, candidateMods)
                        : current;
                    sb.Append("<pos=74%>►<pos=82%>");
                    float delta = projected - current;
                    if (Mathf.Abs(delta) > 0.001f)
                    {
                        string hex = delta > 0f ? GoodHex : BadHex;
                        sb.Append("<color=").Append(hex).Append('>').Append(Num(projected)).Append("</color>");
                    }
                    else
                    {
                        sb.Append(Num(projected));
                    }
                }
                sb.Append('\n');
            }
            return sb.ToString().TrimEnd('\n');
        }

        private static string NameOf(ItemInstance item) =>
            item.UpgradeLevel > 0 ? $"{item.Data.DisplayName} +{item.UpgradeLevel}" : item.Data.DisplayName;

        private static string DescOf(ItemInstance item)
        {
            string slot = SlotName(item.Data.Slot);
            return string.IsNullOrEmpty(item.Data.Description) ? slot : $"{slot}\n{item.Data.Description}";
        }

        private static string Num(float v) => v.ToString("0.#");

        private static void Set(TMP_Text label, string text)
        {
            if (label != null)
            {
                label.text = text;
            }
        }

        private static string SlotName(EquipmentSlot slot)
        {
            switch (slot)
            {
                case EquipmentSlot.PrimaryWeapon: return "Primary Weapon";
                case EquipmentSlot.SecondaryWeapon: return "Off-hand";
                default: return slot.ToString();
            }
        }

        private static string ShortName(AttributeType type)
        {
            switch (type)
            {
                case AttributeType.MaxHealth: return "Max HP";
                case AttributeType.MaxStamina: return "Max Stamina";
                case AttributeType.PhysicalAttack: return "Phys Atk";
                case AttributeType.MagickAttack: return "Magick Atk";
                case AttributeType.PhysicalDefense: return "Phys Def";
                case AttributeType.MagickDefense: return "Magick Def";
                case AttributeType.StaggerResistance: return "Stagger Res";
                case AttributeType.KnockdownResistance: return "Knockdown Res";
                default: return type.ToString();
            }
        }
    }
}

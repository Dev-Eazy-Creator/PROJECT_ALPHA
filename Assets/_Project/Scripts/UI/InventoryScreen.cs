// Full-screen inventory/equipment screen: pauses the game, shows a live character preview + a stats
// summary + a per-item detail/comparison panel, and an icon grid of ALL owned items (bag + equipped,
// equipped ones badged) filtered by slot. Hover (mouse) or focus (controller) an item to inspect it;
// click (mouse) or Submit/A equips or unequips it. Items keep a STABLE slot->name order so equipping
// never reorders them, and cells update in place (not rebuilt) when only equip state changes.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

namespace ProjectAlpha
{
    public class InventoryScreen : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private InputReader inputReader;
        [SerializeField] private EquipmentManager equipment;
        [SerializeField] private Inventory inventory;
        [SerializeField] private CharacterStats characterStats;
        [SerializeField] private CharacterPreview preview;

        [Header("UI")]
        [Tooltip("The panel to show/hide. Must be a child of this (always-active) object, not this object itself.")]
        [SerializeField] private GameObject root;
        [Tooltip("The per-slot filter tabs (All + one per EquipmentSlot).")]
        [SerializeField] private List<GearFilterButton> filterButtons = new List<GearFilterButton>();
        [Tooltip("The GridLayoutGroup content transform that item cells are parented under.")]
        [SerializeField] private Transform bagContent;
        [SerializeField] private BagItemCell bagCellPrefab;
        [SerializeField] private ItemDetailPanel detailPanel;
        [Tooltip("Header above the grid naming the active slot filter (e.g. \"All\", \"Head\").")]
        [SerializeField] private TMP_Text filterNameLabel;
        [SerializeField] private TMP_Text messageLabel;

        private const string ControlsHint = "Hover to inspect   •   Click / (A) to equip or unequip   •   I to close";

        private readonly List<BagItemCell> spawnedCells = new List<BagItemCell>();
        private readonly List<ItemInstance> displayedItems = new List<ItemInstance>();
        private readonly List<ItemInstance> scratch = new List<ItemInstance>();
        private GearFilterButton currentFilter;
        private ItemInstance detailItem;
        private bool isPaused;
        private bool suppressRefresh;
        private float cachedTimeScale = 1f;

        private void Awake()
        {
            if (root != null)
            {
                root.SetActive(false);
            }

            foreach (GearFilterButton fb in filterButtons)
            {
                if (fb == null || fb.Button == null)
                {
                    continue;
                }
                GearFilterButton captured = fb;
                fb.Button.onClick.AddListener(() => SetFilter(captured));
                if (currentFilter == null || fb.IsAll)
                {
                    currentFilter = fb;
                }
            }
        }

        private void OnEnable()
        {
            if (inputReader != null)
            {
                inputReader.OnToggleInventoryStarted += Toggle;
                inputReader.OnConfirmEquipStarted += ConfirmEquip;
            }
            if (inventory != null)
            {
                inventory.OnInventoryChanged += HandleDataChanged;
            }
            if (equipment != null)
            {
                equipment.OnEquipmentChanged += HandleEquipmentChanged;
            }
        }

        private void OnDisable()
        {
            if (inputReader != null)
            {
                inputReader.OnToggleInventoryStarted -= Toggle;
                inputReader.OnConfirmEquipStarted -= ConfirmEquip;
                inputReader.SetGameplayInputEnabled(true);
            }
            if (inventory != null)
            {
                inventory.OnInventoryChanged -= HandleDataChanged;
            }
            if (equipment != null)
            {
                equipment.OnEquipmentChanged -= HandleEquipmentChanged;
            }

            Resume();
            if (preview != null)
            {
                preview.SetActive(false);
            }
        }

        // A single equip is a Remove+Equip(+Add) that fires several change events; suppress the
        // intermediate refreshes so the grid updates once, in place, instead of flickering mid-transaction.
        private void HandleDataChanged() { if (!suppressRefresh) Refresh(); }
        private void HandleEquipmentChanged(EquipmentSlot slot, ItemInstance item) { if (!suppressRefresh) Refresh(); }

        private void Toggle()
        {
            if (root == null)
            {
                return;
            }

            if (root.activeSelf)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        private void Open()
        {
            root.SetActive(true);
            Pause();
            if (inputReader != null)
            {
                inputReader.SetGameplayInputEnabled(false);
            }
            if (preview != null)
            {
                preview.SetActive(true);
            }
            SetMessage(ControlsHint);
            RefreshFilterButtons();
            Refresh();
            FocusFirstCell();
        }

        private void Close()
        {
            root.SetActive(false);
            if (preview != null)
            {
                preview.SetActive(false);
            }
            if (inputReader != null)
            {
                inputReader.SetGameplayInputEnabled(true);
            }
            detailItem = null;
            if (detailPanel != null)
            {
                detailPanel.Clear();
            }
            Resume();
        }

        private void Pause()
        {
            if (isPaused)
            {
                return;
            }
            cachedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            isPaused = true;
        }

        private void Resume()
        {
            if (!isPaused)
            {
                return;
            }
            Time.timeScale = cachedTimeScale;
            isPaused = false;
        }

        private void SetFilter(GearFilterButton filter)
        {
            currentFilter = filter;
            RefreshFilterButtons();
            RefreshBag();
            FocusFirstCell();
        }

        // Give the first cell EventSystem focus so controller navigation has a starting point and the
        // detail panel shows something on open / filter change.
        private void FocusFirstCell()
        {
            if (EventSystem.current == null || spawnedCells.Count == 0 || spawnedCells[0] == null)
            {
                // Nothing to focus (empty filter): show current character totals with no projection.
                if (detailPanel != null && spawnedCells.Count == 0)
                {
                    detailItem = null;
                    detailPanel.Show(null, equipment, characterStats);
                }
                return;
            }
            EventSystem.current.SetSelectedGameObject(spawnedCells[0].gameObject);
            OnFocusItem(spawnedCells[0].Item);
        }

        private void Refresh()
        {
            if (root == null || !root.activeSelf)
            {
                return;
            }

            RefreshBag();
        }

        private void RefreshFilterButtons()
        {
            foreach (GearFilterButton fb in filterButtons)
            {
                if (fb != null)
                {
                    fb.SetSelected(fb == currentFilter);
                }
            }

            if (filterNameLabel != null)
            {
                filterNameLabel.text = currentFilter != null ? currentFilter.DisplayName : "All";
            }
        }

        // Builds the desired (stably ordered, filtered) item list, then reuses the existing cells in place
        // when the list is unchanged (equip/unequip) and only rebuilds when the set actually changes.
        private void RefreshBag()
        {
            if (bagContent == null || bagCellPrefab == null)
            {
                return;
            }

            scratch.Clear();
            if (inventory != null)
            {
                foreach (ItemInstance item in inventory.Items)
                {
                    if (Include(item))
                    {
                        scratch.Add(item);
                    }
                }
            }
            if (equipment != null)
            {
                foreach (KeyValuePair<EquipmentSlot, ItemInstance> pair in equipment.Equipped)
                {
                    if (Include(pair.Value))
                    {
                        scratch.Add(pair.Value);
                    }
                }
            }
            scratch.Sort(CompareItems);

            if (!SameSequence(scratch, displayedItems))
            {
                foreach (BagItemCell cell in spawnedCells)
                {
                    if (cell != null)
                    {
                        Destroy(cell.gameObject);
                    }
                }
                spawnedCells.Clear();
                displayedItems.Clear();

                foreach (ItemInstance item in scratch)
                {
                    spawnedCells.Add(Instantiate(bagCellPrefab, bagContent));
                    displayedItems.Add(item);
                }
            }

            for (int i = 0; i < spawnedCells.Count; i++)
            {
                ItemInstance item = displayedItems[i];
                spawnedCells[i].Bind(item, IsEquipped(item, out _), OnFocusItem, OnEquipItem);
            }
        }

        private bool Include(ItemInstance item)
        {
            return item != null && item.Data != null
                && (currentFilter == null || currentFilter.Matches(item.Data.Slot));
        }

        // Stable order: by slot, then display name, then upgrade level. Independent of equip state.
        private static int CompareItems(ItemInstance a, ItemInstance b)
        {
            int slotCompare = ((int)a.Data.Slot).CompareTo((int)b.Data.Slot);
            if (slotCompare != 0)
            {
                return slotCompare;
            }
            int nameCompare = string.CompareOrdinal(a.Data.DisplayName, b.Data.DisplayName);
            if (nameCompare != 0)
            {
                return nameCompare;
            }
            return a.UpgradeLevel.CompareTo(b.UpgradeLevel);
        }

        private static bool SameSequence(List<ItemInstance> a, List<ItemInstance> b)
        {
            if (a.Count != b.Count)
            {
                return false;
            }
            for (int i = 0; i < a.Count; i++)
            {
                if (!ReferenceEquals(a[i], b[i]))
                {
                    return false;
                }
            }
            return true;
        }

        // Hover / focus: inspect the item in the detail panel.
        private void OnFocusItem(ItemInstance item)
        {
            detailItem = item;
            if (detailPanel != null)
            {
                detailPanel.Show(item, equipment, characterStats);
            }
        }

        // Click / Submit(A): equip the item, or unequip it if already equipped.
        private void OnEquipItem(ItemInstance item)
        {
            if (item == null)
            {
                return;
            }

            detailItem = item;
            suppressRefresh = true;
            if (IsEquipped(item, out EquipmentSlot slot))
            {
                UnequipToBag(slot);
            }
            else
            {
                EquipFromBag(item);
            }
            suppressRefresh = false;

            Refresh();
            if (detailPanel != null)
            {
                detailPanel.Show(item, equipment, characterStats);   // reflect the new equipped state + comparison
            }
        }

        // E keyboard shortcut: equip/unequip whatever is currently being inspected.
        private void ConfirmEquip()
        {
            if (root == null || !root.activeSelf || detailItem == null)
            {
                return;
            }
            OnEquipItem(detailItem);
        }

        private bool IsEquipped(ItemInstance item, out EquipmentSlot slot)
        {
            slot = default;
            if (equipment == null || item == null)
            {
                return false;
            }

            foreach (KeyValuePair<EquipmentSlot, ItemInstance> pair in equipment.Equipped)
            {
                if (pair.Value == item)
                {
                    slot = pair.Key;
                    return true;
                }
            }
            return false;
        }

        // §3 flow: bag -> slot, returning any displaced item to the bag. Returns false if it can't equip
        // (the detail panel surfaces the job-lock reason).
        private bool EquipFromBag(ItemInstance item)
        {
            if (item == null || equipment == null || inventory == null)
            {
                return false;
            }

            if (!equipment.CanEquip(item, out _))
            {
                return false;
            }

            ItemInstance displaced = null;
            equipment.Equipped.TryGetValue(item.Data.Slot, out displaced);

            inventory.Remove(item);
            equipment.Equip(item);
            if (displaced != null)
            {
                inventory.Add(displaced);
            }
            return true;
        }

        // §3 flow: slot -> bag.
        private void UnequipToBag(EquipmentSlot slot)
        {
            if (equipment == null || inventory == null)
            {
                return;
            }

            if (!equipment.Equipped.TryGetValue(slot, out ItemInstance item) || item == null)
            {
                return;
            }

            equipment.Unequip(slot);
            inventory.Add(item);
        }

        private void SetMessage(string message)
        {
            if (messageLabel != null)
            {
                messageLabel.text = message;
            }
        }
    }
}

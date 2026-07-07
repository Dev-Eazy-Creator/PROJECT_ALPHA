# GDD Part 4 — Technical Specification for Claude Code
# 3D Sidescroller: Inventory & Equipment UI

## HOW TO USE THIS DOCUMENT
Continues from Parts 1–3. Same rules: implement sections in order, do not skip, do not invent
architecture not covered here. If something is ambiguous, stop and ask.

> **STATUS: DRAFT for ratification.** Authored collaboratively. Review it, edit anything, then it
> becomes the locked spec. **[RATIFY]** marks decisions to confirm; **[TBD]** marks open items.

This milestone surfaces the Part 3 gear system with a real **equipment screen**: see what's equipped
per slot, browse an inventory of owned items, and **click to equip/unequip** — replacing the debug
keys. It builds on the existing uGUI/AriaGUI/HUD foundation and the `EquipmentManager`. **No loot
system, no save/load, no tooltips-with-comparison, no drag-and-drop** yet — just a working, readable
equip screen driven by click.

---

## SECTION 0 — CONTEXT: WHAT'S ALREADY BUILT (reuse, do not rewrite)
- Namespace `ProjectAlpha`. Unity `6000.3.9f1`. Input System (Action-based). No legacy `Input.*` in
  gameplay. `PlayerMotor` owns `CharacterController.Move()`.
- **Stats:** `CharacterStats`/`AttributeSet`/`StatModifier` — gear applies modifiers here.
- **Gear (Part 3):** `EquipmentSlot`, `ItemData`→`EquipmentData`→`WeaponData`/`ArmorData`,
  `ItemInstance` (data + upgrade level), and **`EquipmentManager`** with `Equip(ItemInstance)`,
  `Unequip(EquipmentSlot)`, `UpgradeEquipped`, `CanEquip(item, out reason)`,
  `IReadOnlyDictionary<EquipmentSlot,ItemInstance> Equipped`, and `event OnEquipmentChanged`.
  `ItemData` already has a `Sprite Icon` field for this UI.
- **UI foundation:** uGUI + **AriaGUI** (Slider/panel/button prefabs), a `Canvas` with a `CanvasScaler`
  (1920×1080), TextMeshPro. `PlayerHud`/`ResourceBarView` show the pools. `InputReader` (ScriptableObject)
  is the sole input source and broadcasts C# events; `PlayerControls.inputactions` has an empty **`UI`**
  action map ready to use.
- **Input:** the `SwitchJob` action is bound to **Tab** — so do **not** use Tab for inventory.

**Rule:** the UI only *drives* `EquipmentManager` + the new `Inventory`; it never computes stats or
duplicates equip logic.

---

## SECTION 1 — DESIGN PILLARS
1. **Surface Part 3** — one screen shows equipped gear per slot and the owned-item bag; equipping
   there does exactly what the debug keys did, through `EquipmentManager`.
2. **Click-to-equip** — click a bag item to equip it to its slot; click an equipped slot to unequip.
   No drag-and-drop this milestone. **[RATIFY]** (click is simpler and controller-friendly).
3. **Honest feedback** — a job-locked or otherwise-invalid equip shows the reason on-screen (reusing
   `EquipmentManager.CanEquip`'s `reason`), it does not silently fail.
4. **Reuse the UI foundation** — same Canvas, AriaGUI widgets, and TMP as the HUD.

---

## SECTION 2 — INVENTORY (owned-item container)
`Assets/_Project/Scripts/Inventory/Inventory.cs`
```
// The player's owned, UNEQUIPPED items (the bag). Equipped items live in EquipmentManager, not here.
```
**Serialized:** `List<EquipmentData> startingItems` — items the player owns at boot (stand-in for the
future loot system). **[RATIFY]**

**Responsibilities:**
- Hold `List<ItemInstance> items` (the bag). Expose `IReadOnlyList<ItemInstance> Items`.
- `event Action OnInventoryChanged` — the UI refreshes on this.
- On `Start`, wrap each `startingItems` entry in a `new ItemInstance(data)` and add it.
- `void Add(ItemInstance item)` / `bool Remove(ItemInstance item)` — mutate the bag, fire the event.
- **Do not** apply stat modifiers here — only `EquipmentManager` does that on equip.

---

## SECTION 3 — EQUIP / UNEQUIP FLOW (bag ↔ slots)
Equipped items leave the bag and appear in their slot; unequipping returns them to the bag (DD1/Souls
model). This coordination is a thin layer over Parts 3 + Section 2 (put it on the UI controller in
§6 for now; it may be extracted to an `InventoryController` later):

- **Equip from bag** (`item`): if `equipmentManager.CanEquip(item, out reason)`:
  1. capture `displaced = Equipped[item.Data.Slot]` if any,
  2. `inventory.Remove(item)`,
  3. `equipmentManager.Equip(item)`,
  4. if `displaced != null` → `inventory.Add(displaced)`.
  Else surface `reason` (no state change).
- **Unequip to bag** (`slot`): if occupied, capture `item = Equipped[slot]`, `equipmentManager.Unequip(slot)`,
  then `inventory.Add(item)`.

> **[RATIFY]** `EquipmentManager.Equip` currently unequips the slot's occupant internally (dropping its
> modifiers) but does not *return* it. For the "displaced item goes back to the bag" behavior, the
> coordinator captures `Equipped[slot]` **before** calling `Equip`. No change to `EquipmentManager` is
> required — confirm this approach vs. adding a return value to `Equip`.

---

## SECTION 4 — INPUT: TOGGLE THE SCREEN
Add a real Input Action (not a debug key) — this is shipping UI.
- In `PlayerControls.inputactions`, add action **`ToggleInventory`** (Button) to the **`UI`** action
  map. Bindings: **`I`** (KBM) and **gamepad Start/Menu** (`<Gamepad>/start`). **[RATIFY]** (not Tab).
- `InputReader` gains `event Action OnToggleInventoryStarted` and enables the `UI` map alongside
  `Player` (subscribe/find the action the same way it does for `Player` actions).
- The equipment screen (§6) subscribes to `OnToggleInventoryStarted` to open/close.

**[RATIFY]** Opening the screen does **not** pause the game (`Time.timeScale` untouched) this milestone
— simplest; a pause can be added later.

---

## SECTION 5 — EQUIPMENT SCREEN (UI structure)
A screen-space panel on the existing Canvas, hidden by default. Assemble it in the Editor with AriaGUI
widgets (like the HUD) — code provides the controller + row binding; a small builder tool is optional.

Layout:
- **Root panel** (`InventoryScreen`) — a full or side panel, `SetActive(false)` by default.
- **Equipment slots panel** — one **slot widget** per `EquipmentSlot` (10 total), each a Button showing
  the equipped item's **name** (icon optional — see §7) or "Empty". Clicking a slot **unequips** it.
- **Bag panel** — a scrollable vertical list of the inventory's items; each row is a Button showing the
  item's name. Clicking a row **equips** that item.
- **Message line** — a TMP label that shows equip failures (e.g. "job-locked (needs Job2)").
- **[TBD]** Item detail/stat panel and stat-comparison — deferred to a later polish pass.

---

## SECTION 6 — EQUIPMENT SCREEN CONTROLLER
`Assets/_Project/Scripts/UI/InventoryScreen.cs`
```
// Opens/closes the equipment screen and drives equip/unequip through EquipmentManager + Inventory.
```
**Serialized:** `InputReader inputReader`, `EquipmentManager equipment`, `Inventory inventory`,
`GameObject root` (the panel), the slot-widget references (one per slot, or a list), the bag
`ScrollRect`/content transform, a bag-row prefab, and a TMP `messageLabel`.

**Responsibilities:**
- Subscribe to `InputReader.OnToggleInventoryStarted` → toggle `root.activeSelf`; on open, `Refresh()`.
- Subscribe to `Inventory.OnInventoryChanged` and `EquipmentManager.OnEquipmentChanged` → `Refresh()`.
- `Refresh()`:
  - For each slot widget: set its label to `Equipped[slot].Data.DisplayName` (or "Empty"); wire its
    Button to `UnequipToBag(slot)`.
  - Rebuild the bag list: clear old rows, instantiate a row per `inventory.Items`, label it, wire its
    Button to `EquipFromBag(item)`.
- `EquipFromBag`/`UnequipToBag` implement the §3 flow; on a failed equip, set `messageLabel` to the
  `CanEquip` reason.
- Unsubscribe in `OnDestroy`.

No `FindObjectOfType` — all references assigned in the Inspector.

---

## SECTION 7 — ITEM PRESENTATION
- Rows and slots show the item's **`DisplayName`** (TMP). **`Icon`** is optional this milestone —
  the example items have no sprites yet; show the name and, if `Icon != null`, the sprite. **[RATIFY]**
- Slot widgets are labeled with a friendly slot name ("Head", "Primary Weapon", …) so empty slots read
  clearly.

---

## SECTION 8 — SCENE / PLAYER SETUP
- Add **`Inventory`** to the Player; set `startingItems` to a few owned pieces (e.g. `Warrior_Helm`,
  `Warrior_Chest`, `Warrior_Gloves`, `Warrior_Greaves`, `Warrior_Boots`, `Ring_Test`, and `Mage_Staff`
  to exercise the job-lock message).
- Build the **InventoryScreen** panel under the existing Canvas (AriaGUI); add `InventoryScreen` and
  wire `inputReader`, `equipment`, `inventory`, `root`, slot widgets, bag list, row prefab, message label.
- Keep the starting weapon on `EquipmentManager.startingEquipment` (Warrior spawns armed).
- `DebugEquipmentTester` may be **removed** now that the screen equips — or kept as a dev shortcut. **[RATIFY]**

---

## SECTION 9 — DEBUG / TEST SUPPORT (throwaway)
Since there is no loot yet, add a tiny debug affordance to put items in the bag for testing:
- Either author them into `Inventory.startingItems` (preferred, no code), **or** a throwaway key in a
  debug script that calls `inventory.Add(new ItemInstance(someData))`. Reuse the Part 2/3 debug
  exception (`Keyboard.current`) if a key is used.

---

## SECTION 10 — ACCEPTANCE CRITERIA
**Open/close**
- [ ] Pressing the `ToggleInventory` action (I / gamepad Start) opens and closes the equipment screen;
      game keeps running (no pause).
**Display**
- [ ] Each equipment slot shows the equipped item's name (or "Empty"); the bag lists owned unequipped items.
- [ ] The starting weapon shows in the Primary Weapon slot; starting bag items appear in the bag.
**Equip / unequip (through EquipmentManager)**
- [ ] Clicking a bag item equips it: it leaves the bag, fills its slot, and its stats apply (verify via
      the HUD / a stat readout — e.g. +MaxHealth grows the health bar).
- [ ] Equipping into an occupied slot returns the displaced item to the bag.
- [ ] Clicking an equipped slot unequips it: the item returns to the bag and its stats revert.
- [ ] A **job-locked** item shows the reason on the message line and does **not** equip or change stats.
- [ ] The screen refreshes live on every equip/unequip (slots and bag stay correct).
**Code quality (carried)**
- [ ] Single-responsibility comment per file; `namespace ProjectAlpha`; no `FindObjectOfType`;
      no legacy `Input.*` in gameplay; UI only drives `EquipmentManager`/`Inventory`; no NREs.

---

## SECTION 11 — WHAT LATER PARTS COVER (do not implement now)
- **Character Creator** — next milestone; reuses this equipment UI to pick gender/job/starting gear,
  and writes starting attributes on top of job `BaseStats`. Introduces the persistent "character
  definition."
- **Loot & drop system** — fills the inventory from enemy/chest drops using `ItemData.DropWeight`.
- **Upgrade UI / blacksmith** — an in-screen upgrade action + the material/currency economy.
- **Tooltips & stat comparison** — hover/select to compare an item vs. what's equipped.
- **Item icons & rarity-free polish** — real `Icon` sprites, sorting/filtering, grid layout.
- **Controller navigation** — full gamepad focus/navigation of the screen.
- **Save/Load** — persist inventory contents, equipped gear, and upgrade levels.
- **Drag-and-drop** — optional alternative to click-to-equip.

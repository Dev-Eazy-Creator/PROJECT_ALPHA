# GDD Part 5 — Technical Specification for Claude Code
# 3D Sidescroller: Full-Screen Inventory & Equipment Screen

## HOW TO USE THIS DOCUMENT
Continues from Parts 1–4. Same rules: implement sections in order, do not skip, do not invent
architecture not covered here.

> **STATUS: AS-BUILT (2026-07-08).** This document was drafted from a design conversation, ratified,
> then **revised to match what actually shipped** after several rounds of hands-on iteration. It
> supersedes the original draft: the paper-doll slots, click-to-equip, and coarse Weapons/Armor filter
> described in early revisions were replaced by the design below.

A full-screen, Dragon's-Dogma-style equipment screen: the player renders live on the left, owned gear
appears as an icon grid in the center (filtered per slot), and a detail panel on the right compares the
hovered item against what's equipped. It drives the existing `EquipmentManager` + a new `Inventory` —
no new equip logic.

---

## SECTION 0 — CONTEXT: WHAT PART 4 BUILT (reuse, do not rewrite)
- `Inventory` (the bag: `List<ItemInstance>`, `OnInventoryChanged`, `Add`/`Remove`, serialized `startingItems`).
- `EquipmentManager` (Part 3) — single source of truth for equip/unequip/upgrade, job-locks, `Equipped`,
  `OnEquipmentChanged`, `CanEquip`. The UI only drives it; it never computes stats.
- `InputReader` (ScriptableObject) — the sole input event hub; `UI` action map alongside `Player`.
- Canvas: uGUI + CanvasScaler, reference **1920×1080**, match 0.5 (user runs 4K). Build in reference
  space with anchors + layout groups. EventSystem uses the new-Input-System UI module.
- Stats: `CharacterStats`/`AttributeSet`/`Attribute`/`StatModifier`; gear applies modifiers sourced to
  the `ItemInstance`.

---

## SECTION 1 — DESIGN PILLARS
1. **Full-screen, three columns** — preview · items · detail.
2. **Character on the left** — a live 3D render of the player.
3. **Inspect, then act** — hovering/focusing an item shows its stats and how they compare; a single
   click / gamepad A equips it. No blind commits, no two-device dance.
4. **Built to grow** — category tabs (Gear / Materials / Consumables) exist; only Gear is functional.
5. **Same brain** — equipping goes through `EquipmentManager` + `Inventory`; the UI never duplicates it.

---

## SECTION 2 — RATIFIED DECISIONS
- **Player view:** live 3D preview via a preview camera → RenderTexture → `RawImage` (§4).
- **Item scope:** gear only; category tabs are placeholders. No `MaterialData`/`ConsumableData` yet.
- **Icons:** placeholder icons generated at runtime; real `ItemData.Icon` used when present. Slot-rail
  icons and job crests are art-ready hooks (§7) that fall back to placeholders.
- **Pause:** `Time.timeScale = 0` while open, restored on close; the `Player` action map is suspended so
  gameplay input doesn't leak into the menu.
- **Interaction:** per-slot filter tabs; **hover/focus to inspect, click / gamepad A to equip/unequip**
  (E is a keyboard shortcut). No paper-doll slots, no drag-and-drop.

---

## SECTION 3 — LAYOUT (three columns)
Full-screen root over the Canvas, `SetActive(false)` by default; a top title band and a bottom controls
hint span the width.
- **Left — Character (~26%):** character name + the live 3D **preview** (`RawImage`).
- **Center — Items (~37%):**
  - **Category tabs** (Gear active; Materials/Consumables greyed placeholders).
  - **Slot rail** — a row of tabs: All + one per `EquipmentSlot` (Weapon, Shield, Head, Chest, Gloves,
    Legs, Boots, Cape, Ring, Amulet). Art-ready: each can take a slot icon sprite.
  - **Active-slot header** — names the current filter ("All", "Head", …).
  - **Item grid** — a `GridLayoutGroup` in a `ScrollRect` of `BagItemCell`s.
- **Right — Detail (~35%):** the `ItemDetailPanel` (§6).

---

## SECTION 4 — LIVE 3D CHARACTER PREVIEW
`CharacterPreview` (`Scripts/UI/CharacterPreview.cs`) + a preview camera parented to the player.
- A second **camera** renders the player to a `RenderTexture` (`Rendering/InventoryPreview.renderTexture`),
  culling-masked to the player's layer, solid background; enabled only while the screen is open.
- Optional `PreviewDragRotator` orbits the camera around the player (drag).
- Because the screen pauses, the player is a clean frozen pose. **⚠ Armor has no meshes yet** (Part 3
  spawns visuals for weapons only), so weapon swaps reflect but armor does not — expected, not a bug.
- Scene setup (manual): put the player on its own layer; frame `InventoryPreviewCamera`.

---

## SECTION 5 — ITEM GRID
- The grid shows **all owned items — bag + equipped** — with equipped cells carrying an **"E" badge**.
- **Stable order** (slot → name → upgrade level), independent of equip state, so equipping never
  reorders items.
- Cells update **in place**; the grid only rebuilds when the visible set changes (filter switch, or a
  real add/remove), never on equip — a `suppressRefresh` guard batches the Remove+Equip(+Add) transaction
  into one refresh. No flicker.
- `BagItemCell`: icon (`ItemData.Icon` or generated placeholder) + name + badge; hover (`IPointerEnter`)
  / focus (`ISelect`) → inspect; the cell's `Button.onClick` → equip/unequip (fires on mouse click AND
  gamepad Submit/A). On open the first cell is given EventSystem focus.

---

## SECTION 6 — ITEM DETAIL & COMPARISON PANEL
`ItemDetailPanel` (`Scripts/UI/ItemDetailPanel.cs`), right column, top-to-bottom:
- **Icon** (item's `Icon` or placeholder) beside the **slot + flavor Description**.
- **Name** (+ upgrade level).
- **Job-requirement crest row** — shown only when the item is job-restricted (`EquipmentData.AllowedJobs`
  non-empty): a crest badge + "Requires: <job>", **gold** when the current job qualifies, **red** when
  locked. Art-ready: per-`JobType` crest sprites (§7).
- **Stat comparison** — for a core set of attributes, `current ► new` resulting totals, colored green
  (higher) / red (lower). Computed by `AttributeSet.PreviewValue(attr, displacedItem, candidate.BuildModifiers())`
  — a non-destructive recompute through the real Flat→PercentAdd→PercentMult pipeline (exact for percent
  modifiers, no side-effects on live stats). With no item hovered it shows current totals only.

---

## SECTION 7 — ART-READY HOOKS (placeholders until sprites exist)
- **Item icons:** `ItemData.Icon`; grid cells + detail panel already use it, falling back to a generated
  flat-color `PlaceholderIcon`.
- **Slot-rail icons:** `GearFilterButton.icon` (Sprite). When set it replaces the tab's text label.
- **Job crests:** `ItemDetailPanel.jobCrests` (per-`JobType` sprite list). When a required job has one,
  the crest shows the sprite (white when usable, red-tinted when locked) instead of a colored box.

---

## SECTION 8 — INPUT
- `ToggleInventory` (Button) in the `UI` map — **I** (KBM) / **gamepad Start**. Opens/closes.
- `ConfirmEquip` (Button) in the `UI` map — **E** (KBM only). Keyboard shortcut to equip/unequip the
  inspected item. Gamepad A is handled by the EventSystem's Submit → the focused cell's `Button.onClick`,
  so A is deliberately NOT bound to `ConfirmEquip` (would double-fire).
- `InputReader.SetGameplayInputEnabled(false/true)` suspends/restores the `Player` map on open/close.

---

## SECTION 9 — CONTROLLER (`InventoryScreen`) & BUILDER
- `InventoryScreen` (`Scripts/UI/InventoryScreen.cs`) — lives on an always-active object, toggles a child
  `root`. Subscribes to input + `Inventory`/`EquipmentManager` changes; owns pause, filtering, the grid
  (stable sort + in-place update), focus/inspect, and the equip/unequip transaction. No `FindObjectOfType`;
  all refs Inspector-assigned. **`characterStats` must be the player's** (the enemy dummy also has one) —
  the builder resolves it from the `EquipmentManager`.
- `InventoryScreenBuilder` (`Scripts/Editor/InventoryScreenBuilder.cs`), menu **Tools/ProjectAlpha/Build
  Inventory Screen** — generates the full hierarchy, saves `Prefabs/UI/BagItemCell.prefab` + the preview
  RenderTexture, builds the preview camera rig on the player, ensures the EventSystem, and auto-wires
  everything. Refuses to run if an `InventoryScreen` already exists (delete it to rebuild).

---

## SECTION 10 — SCENE / PLAYER SETUP
- Add `Inventory` to the Player; set `startingItems` (e.g. `Warrior_Helm/Chest/Gloves/Greaves/Boots`,
  `Ring_Test`, and `Mage_Staff` to exercise the job-lock crest).
- Put the player visual on its own layer; run the builder; frame `InventoryPreviewCamera`.
- Assign any refs the builder couldn't (it reports them).

---

## SECTION 11 — ACCEPTANCE CRITERIA
- [ ] `I` / gamepad Start opens a full-screen, three-column screen and closes it; `Time.timeScale` is 0
      while open and restored on close; gameplay input is suspended while open.
- [ ] The player renders live on the left; a weapon equip/unequip updates it (armor not reflected).
- [ ] The grid shows all owned gear (bag + equipped, equipped badged) for the selected slot filter,
      placeholder icons when no sprite; items never reorder on equip and the grid doesn't flicker.
- [ ] Hovering/focusing an item shows its icon, description, job crest (gold/red), and `current ► new`
      comparison; the projection is correct including percent modifiers.
- [ ] Click / gamepad A equips or unequips the item through `EquipmentManager`; a job-locked item shows a
      red crest and does not equip; displaced items return to the bag; stats and comparison update live.
- [ ] Slot filter tabs change only what's shown; Materials/Consumables tabs are disabled.
- [ ] Single-responsibility comment per file; `namespace ProjectAlpha`; no `FindObjectOfType` in runtime;
      no legacy `Input.*`; no NREs; pause always restored.

---

## SECTION 12 — DEFERRED (later parts)
- **Materials & consumables** data + loot system filling the disabled tabs.
- **Armor meshes / attachment** so the preview reflects armor.
- **Real art** — item icons, slot-rail icons, job crests (hooks in §7).
- **Full controller navigation** polish (per-tab focus memory, wrap-around).
- **Upgrade UI / blacksmith**, **character creator**, **save/load**, **drag-and-drop**.

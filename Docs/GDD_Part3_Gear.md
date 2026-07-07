# GDD Part 3 — Technical Specification for Claude Code
# 3D Sidescroller: Gear & Equipment

## HOW TO USE THIS DOCUMENT
Continues from Parts 1–2. Same rules: implement sections in order, do not skip, do not
invent architecture not covered here. If something is ambiguous, stop and ask.

> **STATUS: DRAFT for ratification.** This spec was authored collaboratively from the design
> decisions below. Review it, edit anything you disagree with, then it becomes the locked spec.
> Points explicitly flagged **[RATIFY]** are places a real decision was made that you should confirm.

This milestone builds the **Gear system** — the game's *primary power source* — on top of the
Part 2 stat keystone. Gear is **job-locked**, **DD1-style**, **upgradeable**, and applies
`StatModifier`s through the exact path augments already use. There is **no inventory UI, no loot/drop
system, and no material economy** yet — only the equip / upgrade / stat plumbing, proven debug-driven
(equip an item → watch stats change → deal more damage), exactly how Part 2 proved the damage path.

---

## SECTION 0 — CONTEXT: WHAT PARTS 1–2 BUILT (reuse, do not rewrite)
- Namespace `ProjectAlpha`. Unity `6000.3.9f1`. Input System (Action-based), no legacy input in
  gameplay. `PlayerMotor` is the sole caller of `CharacterController.Move()`.
- **Stat system (the keystone):** `AttributeType` (12, locked), `Attribute` (Flat→PercentAdd→
  PercentMult pipeline), `AttributeSet`, `StatModifier` (with a `Source` tag), `CharacterStats`
  (`Attributes`, `GetValue`, `OnStatsRebuilt`). **Gear applies `StatModifier`s to `CharacterStats.
  Attributes` with the item as the `Source` — identical to how `AugmentEquipper` already works.**
- **Jobs:** `JobType` (Warrior=0 … Job6=5), `JobData` (identity + `BaseStats`), `JobManager`
  (`CurrentJob`, `OnJobChanged`). Only the **Warrior** is authored.
- **Weapons (Part 1):** `IWeapon`, `WeaponEquipper`, a `WeaponAnchor` child on the player. Part 1
  spawned `JobData.WeaponPrefab` on job change. **Part 3 shifts weapon spawning to equipped gear —
  see §7.**
- **HUD (UI foundation):** health/stamina bars bound to the resource pools. The health bar's max
  will visibly grow when +MaxHealth gear is equipped — a free visual test.

**Rule:** reuse the stat pipeline exactly. Gear never computes final stats itself; it only adds/
removes `StatModifier`s and lets `Attribute` do the math.

---

## SECTION 1 — DESIGN PILLARS
1. **Gear is the primary power source** — it supplies the bulk of offensive and defensive numbers.
2. **Job-locked** — every piece declares which job(s) may use it. A Warrior cannot wear a mage robe
   or wield a staff, and vice-versa.
3. **DD1-faithful** — layered armor, discrete upgrade tiers, and weapons carry stagger/knockdown
   power (a property of the weapon, not the character sheet).
4. **No rarity/quality tiers** — items have no common/rare/epic grade, no colored names, no random
   affix rolls. An item's stats are fixed by its asset (plus its upgrade level). **"Rare" means
   rarely *dropped*** — a drop-chance value exists as data for a future loot system only.
5. **Stamina is a contested resource** — skills spend stamina, so gear stat budgets must respect the
   stamina economy. **Mage gear leans harder into stamina** (mages live on skills) than warrior gear.

---

## SECTION 2 — ATTRIBUTE MAPPING (no new attributes)
The 12 Part 2 attributes are sufficient. **Do NOT add new attributes.** DD1 terms map onto them:

| DD1 / design term | This project's attribute |
|---|---|
| Strength (melee power) | `PhysicalAttack` |
| Magick (staff power) | `MagickAttack` |
| Damage reduction (physical) | `PhysicalDefense` |
| Damage reduction (magick) | `MagickDefense` |
| Health | `MaxHealth` |
| Stamina | `MaxStamina` |
| Poise/stagger resist | `StaggerResistance` / `KnockdownResistance` |

Gear only ever touches these existing attributes via `StatModifier`s. `Weight`/`EquipLoad` remain
**excluded** (still no consequence until dodge — see §9, §15).

---

## SECTION 3 — EQUIPMENT SLOTS
`Assets/_Project/Scripts/Gear/EquipmentSlot.cs`
```csharp
// Fixed equipment slots. DD1-style layered armor. Do not reorder (save data references these).
public enum EquipmentSlot
{
    PrimaryWeapon,
    SecondaryWeapon,   // shield / off-hand; empty for two-handed weapons
    Head,
    ChestOuter,        // outer torso armor
    ChestInner,        // inner clothing layer (DD1 layered torso)
    Arms,
    Legs,
    Cape,
    Ring1,             // jewelry
    Ring2
}
```
**[RATIFY]** One layered torso (Outer + Inner) captures DD1's layering without exploding the slot
count. If you want legs layered too (DD1 does), add `LegsInner`. Rings are two slots of one category.

---

## SECTION 4 — GEAR STAT MODIFIER (authoring struct)
`Assets/_Project/Scripts/Gear/GearModifier.cs`
```csharp
// One stat contribution from a piece of gear, with a per-upgrade-level increment (DD1 enhancement).
[System.Serializable]
public struct GearModifier
{
    public AttributeType Attribute;
    public StatModifierType Type;   // Flat / PercentAdd / PercentMult (reused from Part 2)
    public float BaseValue;         // contribution at upgrade level 0
    public float PerLevelValue;     // added per upgrade level (DD1: each star adds a fixed amount)
}
```
Effective value at upgrade level `L` = `BaseValue + L * PerLevelValue`.

---

## SECTION 5 — ITEM DATA MODEL (ScriptableObjects)
Folder `Assets/_Project/Scripts/Gear/`. Assets under `ScriptableObjects/Gear/`.

```csharp
// Base authoring asset for any item.
public class ItemData : ScriptableObject
{
    public string DisplayName;
    public Sprite Icon;                       // for the future inventory UI
    [Tooltip("Kilograms-ish; summed into EquipLoad later. No movement effect in Part 3.")]
    public float Weight = 1f;
    [TextArea] public string Description;
    [Tooltip("Drop-chance weight for the FUTURE loot system. Data-only in Part 3; nothing reads it.")]
    public float DropWeight = 1f;
}

// Anything you can equip into a slot.
[CreateAssetMenu(fileName = "New_Equipment", menuName = "Game/Gear/Equipment")]
public class EquipmentData : ItemData
{
    public EquipmentSlot Slot;
    [Tooltip("Jobs allowed to equip this. EMPTY = any job.")]
    public List<JobType> AllowedJobs = new List<JobType>();
    public List<GearModifier> Modifiers = new List<GearModifier>();
    [Min(0)] public int MaxUpgradeLevel = 3;   // DD1: three enhancement stars by default
}

[CreateAssetMenu(fileName = "New_Weapon", menuName = "Game/Gear/Weapon")]
public class WeaponData : EquipmentData
{
    public WeaponClass Class;                  // Sword, GreatSword, Staff, Bow, Dagger, Mace...
    public DamageType DamageType;              // Physical / Magick
    public float StaggerPower;                 // consumed by combat later; authored now
    public float KnockdownPower;
    public GameObject WeaponPrefab;            // spawned at the Part 1 WeaponAnchor
}

[CreateAssetMenu(fileName = "New_Armor", menuName = "Game/Gear/Armor")]
public class ArmorData : EquipmentData { }     // no extra fields; distinct type + menu for clarity
```
Enums (`Gear/WeaponClass.cs`, `Gear/DamageType.cs`):
```csharp
public enum WeaponClass { Sword, GreatSword, Dagger, Mace, Staff, Bow }   // extend as needed
public enum DamageType { Physical, Magick }
```
**[RATIFY]** `MaxUpgradeLevel = 3` mirrors DD1's three stars (Dragonforging/Rarification tiers are
deferred, §8). Adjust freely.

---

## SECTION 6 — ITEM INSTANCE (runtime)
`Assets/_Project/Scripts/Gear/ItemInstance.cs`
```csharp
// A concrete owned/equipped item: its data asset plus its per-instance upgrade level.
// Plain C# (not a MonoBehaviour/SO). This is the modifier Source, so removal targets THIS item.
public class ItemInstance
{
    public EquipmentData Data { get; }
    public int UpgradeLevel { get; private set; }

    public ItemInstance(EquipmentData data, int upgradeLevel = 0) { ... clamp to Data.MaxUpgradeLevel }
    public void SetUpgradeLevel(int level) { ... clamp, callers re-apply }

    // Yields the StatModifiers this instance contributes at its current level, sourced to itself.
    public IEnumerable<StatModifier> BuildModifiers() =>
        Data.Modifiers.Select(m => new StatModifier(
            m.Attribute, m.Type, m.BaseValue + UpgradeLevel * m.PerLevelValue, this));
}
```
**Why per-instance:** upgrade level is per item, and Source-based removal (Part 2) must detach the
*specific* equipped item. In Part 3 these are created in code/debug; inventory + save create them later.

---

## SECTION 7 — EQUIPMENT MANAGER (runtime)
`Assets/_Project/Scripts/Gear/EquipmentManager.cs`
```
// Holds equipped items per slot, applies/removes their stat modifiers, enforces job-locks,
// spawns weapon visuals, and tracks total weight. The single entry point for equip/unequip.
```
**Serialized:** `CharacterStats stats`, `JobManager jobManager`, `Transform weaponAnchor`.

**Responsibilities:**
- `Dictionary<EquipmentSlot, ItemInstance> Equipped` (read-only exposure).
- `bool CanEquip(ItemInstance item, out string reason)` — false if the item's `AllowedJobs` is
  non-empty and does not contain `jobManager.CurrentJob.Job` (job-lock). Extendable later.
- `bool Equip(ItemInstance item)` — if `CanEquip`: unequip the current occupant of `item.Data.Slot`;
  store it; add each of `item.BuildModifiers()` to `stats.Attributes`; if `WeaponData` in a weapon
  slot, spawn `WeaponPrefab` under `weaponAnchor` (see integration below); recompute weight; fire
  `OnEquipmentChanged`. Return success. On failure log the `reason` (info) and change nothing.
- `void Unequip(EquipmentSlot slot)` — `stats.Attributes.RemoveAllModifiersFromSource(instance)`;
  clear slot; despawn weapon if any; recompute weight; fire event.
- `void UpgradeEquipped(EquipmentSlot slot)` — bump the instance's level, then remove+re-add its
  modifiers so the higher values apply live. (No cost in Part 3 — see §8.)
- `float TotalWeight { get; }` — sum of equipped `Data.Weight`. Groundwork only (§9).
- `event Action<EquipmentSlot, ItemInstance> OnEquipmentChanged`.
- **On `JobManager.OnJobChanged`:** re-validate every equipped item; any now-disallowed by the new
  job is **auto-unequipped** (modifiers removed, weapon despawned) and logged. **[RATIFY]** (no
  inventory yet, so it unequips "to nowhere"; when inventory exists it returns to the bag.)

**Weapon integration with Part 1 [RATIFY]:** the equipped `PrimaryWeapon`'s `WeaponPrefab` becomes
the source of truth for the visible weapon, replacing Part 1's job-driven spawn. Recommended:
`EquipmentManager` owns weapon spawning at `weaponAnchor`; `WeaponEquipper`'s auto-spawn on job
change is disabled (leave the script, stop it spawning), and `JobData.WeaponPrefab` becomes a
"starting weapon" the scene equips at boot. The spawned prefab still implements `IWeapon`.

---

## SECTION 8 — UPGRADE SYSTEM (DD1-style — mechanics only)
- `ItemInstance.UpgradeLevel` ranges `0 .. Data.MaxUpgradeLevel`. Each level raises every
  `GearModifier` by its `PerLevelValue` (DD1: each enhancement star adds a fixed amount).
- Part 3 implements **the level, the stat scaling, and live re-apply** (via `UpgradeEquipped`).
- **Deferred:** material/currency costs, the blacksmith UI, and the **Dragonforged / Rarified**
  bonus tiers (DD1/Dark Arisen). In Part 3, upgrading is a debug/API call with no cost.
- **[RATIFY]** Model Dragonforging later as an extra flag/tier on `ItemInstance` that adds a bonus
  on top of the max upgrade level.

---

## SECTION 9 — WEIGHT (groundwork only)
- Each `ItemData.Weight`; `EquipmentManager.TotalWeight` sums equipped weights and fires on change.
- **No movement, dodge, or stamina penalty in Part 3.** `Weight`/`EquipLoad` become real attributes
  and drive movement/dodge tiers only alongside the dodge/roll system (§15). Do **not** add
  `Weight`/`EquipLoad` to `AttributeType` now.

---

## SECTION 10 — RARE DROPS (data-only)
- `ItemData.DropWeight` is authorable but **read by nothing** in Part 3. It exists so the future loot
  system can weight drop tables (a lower weight = a rarer drop).
- **No rarity tiers, no quality colors, no random stat rolls.** Reaffirming pillar #4.

---

## SECTION 11 — EXAMPLE ITEMS TO AUTHOR
Enough to prove job-locking + per-job stat focus + weapons + upgrades. Numbers are starting points.

**Warrior set** (`AllowedJobs = [Warrior]`) — focus: Health, Stamina, physical damage reduction:
| Asset | Slot | Modifiers (Base, +PerLevel) |
|---|---|---|
| `Warrior_Helm` | Head | MaxHealth +20 (+5), PhysicalDefense +8 (+2) |
| `Warrior_Chest` | ChestOuter | MaxHealth +40 (+10), PhysicalDefense +15 (+3), StaggerResistance +10 |
| `Warrior_Greaves` | Legs | PhysicalDefense +10 (+2), MaxStamina +15 (+5) |
| `Warrior_Sword` | PrimaryWeapon | **WeaponData** Class=Sword, DamageType=Physical, PhysicalAttack +12 (+4), StaggerPower 20, KnockdownPower 10 |

**Mage-oriented set** (`AllowedJobs = [Job2]` — the future "Mage" index) — focus: Stamina (skills),
Magick:
| Asset | Slot | Modifiers (Base, +PerLevel) |
|---|---|---|
| `Mage_Hat` | Head | MaxStamina +20 (+5), MagickDefense +8 (+2) |
| `Mage_Robe` | ChestOuter | MaxStamina +40 (+10), MagickDefense +12 (+3), MaxHealth +10 |
| `Mage_Staff` | PrimaryWeapon | **WeaponData** Class=Staff, DamageType=Magick, MagickAttack +15 (+5) |

Note the deliberate asymmetry: warrior chest is Health/Defense-heavy; mage chest is Stamina-heavy
(skills). **[RATIFY]** Only the **Warrior** job is authored (Part 2), so the Mage set exists mainly
to prove the **job-lock rejects it on a Warrior**. To see the Mage set *apply*, temporarily author a
throwaway `Job2` `JobData` (or verify via unit-style debug); no permanent asset required.

---

## SECTION 12 — DEBUG-DRIVEN VERIFICATION (throwaway)
`Assets/_Project/Scripts/Debug/DebugEquipmentTester.cs` — the same throwaway exception as Part 2's
P-key (reads `Keyboard.current`, not the Input Action asset).
- Serialized `EquipmentManager` and a small list of `EquipmentData` to test, plus keys to
  **equip / unequip / upgrade** the selected item.
- On each action, log the affected attribute before/after so the pipeline is visible.
- **Key proof (ties to Part 2):** equip `Warrior_Sword` → `PhysicalAttack` rises → the existing
  **P-key damage test deals more damage**. Equip `Warrior_Chest` → `MaxHealth` rises → the **HUD
  health bar's max grows**. Upgrade the chest → it grows again. Unequip → everything reverts.
  Try to equip `Mage_Staff` on the Warrior → **rejected, logged, no stat change.**

This reuses Part 2's `DebugCombatTester` and the HUD as the readout — no new debug UI needed.

---

## SECTION 13 — SCENE / PLAYER SETUP
- Add `EquipmentManager` to the Player; wire `stats` (player `CharacterStats`), `jobManager`, and
  `weaponAnchor` (Part 1 child).
- Disable `WeaponEquipper`'s auto-spawn (per §7); optionally have the scene equip a starting weapon
  `ItemInstance` at boot so the Warrior spawns armed.
- Add `DebugEquipmentTester` with the example items assigned.
- No shipping UI, no new Input Action-asset entries.

---

## SECTION 14 — ACCEPTANCE CRITERIA
**Equip / stats**
- [ ] Equipping a Warrior item adds its `GearModifier`s to the `AttributeSet` (Source = the
      instance); unequipping removes exactly those and nothing else.
- [ ] Equipping +MaxHealth gear raises `MaxHealth`'s final value (HUD max grows); unequipping reverts.
- [ ] Equipping `Warrior_Sword` raises `PhysicalAttack`, and the P-key damage increases — proving
      gear → attribute → damage formula end to end.
- [ ] Two items touching the same attribute stack per the Part 2 Flat→PercentAdd→PercentMult order.
**Job-lock**
- [ ] A Warrior **cannot** equip `Mage_*` gear: `Equip` returns false, logs a reason, changes no stats.
- [ ] A Warrior **can** equip Warrior gear and any gear with empty `AllowedJobs`.
- [ ] Changing to a job that disallows an equipped item auto-unequips it (modifiers removed, logged).
**Upgrade**
- [ ] Upgrading an equipped item raises its contribution to `Base + Level*PerLevel`, applied live,
      clamped to `MaxUpgradeLevel`.
**Weight**
- [ ] `TotalWeight` equals the sum of equipped item weights and updates on equip/unequip.
      (No movement effect — intentional this milestone.)
**Weapon**
- [ ] Equipping a `WeaponData` spawns its prefab at the `WeaponAnchor`; swapping/unequipping
      replaces/despawns it. Melee weapon boosts `PhysicalAttack`; staff boosts `MagickAttack`.
**Data-only**
- [ ] `DropWeight` is authorable; no system reads it; no rarity tiers exist anywhere.
**Code quality (carried)**
- [ ] Single-responsibility comment per file; `namespace ProjectAlpha`; no `FindObjectOfType`;
      no legacy `Input.*` in gameplay (debug excepted); `PlayerMotor` sole `Move()` caller; no NREs.

---

## SECTION 15 — WHAT LATER PARTS COVER (do not implement now)
- **Inventory & equipment UI** — a bag + equip screen on the HUD/uGUI foundation; drag/equip,
  compare, upgrade at a station.
- **Loot & drop system** — uses `DropWeight`; enemy/chest drop tables; rare drops. (No rarity tiers.)
- **Weight → EquipLoad tiers** — added *with* the dodge/roll + combat milestone; introduces the
  `Weight`/`EquipLoad` attributes and the movement/dodge speed tiers then.
- **Blacksmith / economy** — material + currency costs for upgrades; **Dragonforged / Rarified** tiers.
- **Full combat** — consumes weapon `StaggerPower`/`KnockdownPower` and skills spending stamina;
  replaces the P-key with real hitboxes calling `Damageable`.
- **Save/Load** — equipped gear, upgrade levels, and inventory contents.
- **Character creator** — starting attributes layered on job `BaseStats` + starting gear.

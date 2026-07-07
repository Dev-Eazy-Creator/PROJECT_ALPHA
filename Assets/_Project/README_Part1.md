# PROJECT_ALPHA — Part 1 (Core Systems)

## How to build Part 1
1. Open the project in Unity 6 and let it finish importing (it will pull **Cinemachine 3.1.x**,
   added to `Packages/manifest.json`). Wait for compilation to finish with no console errors.
2. In the menu bar, click **Project Alpha ▸ Build Part 1**.
3. Open `Assets/_Project/Scenes/Scene_TestArena.unity` and press **Play**.

The menu command generates every non-script asset programmatically: folders, `Ground`/`Climbable`
tags, the `PlayerControls` input asset wiring, `InputReader_Data`, the 6 `VocationData` assets,
`Player_Base_AC` + the 6 `_AOC` override controllers, the weapon and player prefabs, and the test
scene (ground, climbable wall, player, VocationManager, Cinemachine camera, light).

Re-running the command rebuilds everything cleanly (existing generated assets are replaced).

## Architecture notes / decisions made
These resolve ambiguities or internal conflicts in the GDD. Flagged here per the GDD's
"stop and ask / document decisions" rule.

- **Input (`InputReader`)** references the `InputActionAsset` (`PlayerControls.inputactions`)
  directly and subscribes to actions by name, instead of a generated `PlayerControls` C# wrapper.
  This avoids a compile-order chicken-and-egg and is more robust. All input still flows through
  `InputReader`; no gameplay script touches `PlayerInput`. (No `PlayerInput` component is placed on
  the player, for the same reason — it would redundantly enable the same actions.)
- **Vocation AOCs**: GDD §7.2 says wire all 6 AOCs into their `VocationData` so the swap is
  testable, while §8.2 says only Warrior has an AOC. The acceptance criteria (§11) test switching to
  Vocation2 assigning `Vocation2_AOC`, so **all 6 vocations reference their own AOC**. Only the
  **weapon prefab** is Warrior-only (others null, handled gracefully).
- **Animator `verticalVelocity`**: §7.1 lists only 4 parameters, but the `Jump_Rise`/`Jump_Fall`
  states branch on "vertical velocity". A `verticalVelocity` float parameter was added and is driven
  by `PlayerAnimator` so those transitions work. All animation clips are placeholders (no motion).
- **Camera**: Cinemachine is configured by the generator via reflection (so a package/API mismatch
  can't break the tool). Verify damping (X 0.1 / Y 0.2), the Y dead zone, and the fixed Z=-10 in the
  Inspector against §4.
- **Plane lock (sidescroller)**: Overriding §11's "W/S moves depth (Z)" — this is a true sidescroller,
  so horizontal movement is **X only** and `PlayerMotor` hard-locks the player to its starting Z plane
  (`lockZPlane`, default on). W/S / Left-Stick-Y are used only for climbing.

## Debug: testing vocation switching
`VocationManager.SetVocation(VocationType.Vocation2)` is the public entry point. Call it from a
temporary debug script or the Inspector context to verify the weapon is removed and the AOC swaps.

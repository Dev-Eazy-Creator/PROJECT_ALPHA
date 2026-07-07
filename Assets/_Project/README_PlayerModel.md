# Adding the Player Model (N-Hance Humanoid — Male)

Do these once, in the Unity Editor, after importing the N-Hance pack and your animation clips.

## 1. Rig the model as Humanoid
- Select the Male model FBX → Inspector → **Rig** tab.
- **Animation Type = Humanoid**, **Avatar Definition = Create From This Model** → **Apply**.
- (Optional) click **Configure…** to confirm the avatar mapped with no errors.

## 2. Fix materials for URP (likely needed)
This project is URP. If the character shows up **magenta/pink**, its materials use Built-in shaders.
- Select the model's materials (or all) → **Edit ▸ Rendering ▸ Materials ▸ Convert Selected Built-in Materials to URP**, or assign **URP/Lit** and re-hook the textures.

## 3. Swap the model into the player prefab
- Double-click `Assets/_Project/Prefabs/Player/Player_Warrior` to open it in Prefab Mode.
- **Delete** the placeholder `[Model]` capsule child.
- Drag the **N-Hance Male** model as a child of the `Player_Warrior` root.
- Set the model's Transform: **localPosition (0,0,0)**, rotation **(0,0,0)**, scale **(1,1,1)**.
  Its **feet should sit at y=0** (the CharacterController bottom). Nudge localPosition.y if not.

## 4. Animator settings on the model (IMPORTANT)
The model should have an **Animator** component with the **Avatar** assigned (N-Hance prefabs include this).
- **Uncheck `Apply Root Motion`** on that Animator. We drive movement via CharacterController; leaving
  root motion on will fight our code and make the character drift or refuse to move.
- Do **not** manually set the Animator Controller — `PlayerAnimator` assigns it at runtime from the
  active vocation's Override Controller.

## 5. Repoint PlayerAnimator
- Select the `Player_Warrior` root → **PlayerAnimator** component → set the **Animator** field to the
  model's Animator. (It also auto-resolves via GetComponentInChildren, but explicit is cleaner.)

## 6. Match the CharacterController to the model
- On the root, check **CharacterController**: Height ≈ 1.8, Radius ≈ 0.4, Center Y ≈ 0.9.
- Tweak so the capsule wraps the body and the feet line up with the ground.

## 7. Wire the animation clips into the Base controller
- Make sure each clip is **Humanoid**: select the clip's FBX → Rig → Humanoid → Apply.
  Prefer **in-place** clips (e.g. Mixamo "In Place"); set Idle/Walk/Run/Climb to **Loop Time**.
- Open `Assets/_Project/AnimatorControllers/Base/Player_Base_AC` (Animator window).
- Click each state → Inspector → assign **Motion**:
  | State | Clip | Driven by |
  |---|---|---|
  | Idle | idle (loop) | moveSpeed < 0.1 |
  | Walk | walk (loop) | moving, not sprinting |
  | Run | run / jog (loop) | moving + sprint (Shift/L3) |
  | Jump_Rise | jump up / rising | airborne, rising |
  | Jump_Fall | falling | airborne, falling |
  | Land | landing | just grounded |
  | Climb | climb (loop) | on a Climbable |
- The vocation Override Controllers inherit these Base clips until per-vocation combat anims arrive in Part 2.

## 8. Weapon anchor (optional, for Part 2 combat)
- Reparent `WeaponAnchor` under the model's **right-hand bone** so weapons attach to the hand,
  then reset its localPosition. Fine to leave as-is until combat.

---

### Note
The one-time build tool has been removed, so nothing will overwrite `Player_Warrior` — your model
edits are safe. The `StylizedCharacter` art pack is intentionally **not** committed to git (see the
project `.gitignore`); re-import it from the Asset Store on a fresh clone and the prefab reconnects
by GUID.

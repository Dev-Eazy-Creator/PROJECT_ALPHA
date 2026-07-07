// Editor tool that programmatically builds all Part 1 Unity assets (folders, tags, ScriptableObjects,
// Animator Controllers/Overrides, prefabs, and the test scene). Run via the "Project Alpha" menu.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using ProjectAlpha;

namespace ProjectAlpha.EditorTools
{
    public static class ProjectAlphaSetup
    {
        private const string ProjectRoot = "Assets/_Project";
        private const string InputAssetPath = ProjectRoot + "/Input/PlayerControls.inputactions";
        private const string InputReaderPath = ProjectRoot + "/ScriptableObjects/InputReader_Data.asset";
        private const string BaseAcPath = ProjectRoot + "/AnimatorControllers/Base/Player_Base_AC.controller";
        private const string WeaponPrefabPath = ProjectRoot + "/Prefabs/Weapons/Weapon_SwordShield.prefab";
        private const string PlayerPrefabPath = ProjectRoot + "/Prefabs/Player/Player_Warrior.prefab";
        private const string ScenePath = ProjectRoot + "/Scenes/Scene_TestArena.unity";

        private static readonly string[] VocationNames =
        {
            "Warrior", "Vocation2", "Vocation3", "Vocation4", "Vocation5", "Vocation6"
        };

        [MenuItem("Project Alpha/Build Part 1")]
        public static void BuildPart1()
        {
            CreateFolders();
            AddTag("Ground");
            AddTag("Climbable");

            // Make sure the input actions asset is imported before we try to reference it.
            AssetDatabase.ImportAsset(InputAssetPath, ImportAssetOptions.ForceUpdate);
            InputActionAsset inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
            if (inputAsset == null)
            {
                Debug.LogError($"[ProjectAlphaSetup] Could not load {InputAssetPath}. Aborting. " +
                               "Ensure the asset exists and re-run.");
                return;
            }

            InputReader inputReader = CreateInputReader(inputAsset);
            AnimatorController baseAc = CreateBaseAnimatorController();
            Dictionary<VocationType, AnimatorOverrideController> aocs = CreateOverrideControllers(baseAc);
            GameObject weaponPrefab = CreateWeaponPrefab();
            List<VocationData> vocations = CreateVocationData(weaponPrefab, aocs);
            GameObject playerPrefab = CreatePlayerPrefab(inputReader, baseAc);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            BuildScene(playerPrefab, vocations);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ProjectAlphaSetup] Part 1 build complete. Open Scene_TestArena and press Play.");
        }

        // Safe, targeted upgrade: rebuilds ONLY Player_Base_AC (Idle/Walk/Run + isSprinting) without
        // touching the player prefab, model, or scene. Existing clip assignments are re-applied by
        // matching state name, so you keep the animations you already wired (Walk starts empty).
        [MenuItem("Project Alpha/Rebuild Base Animator Controller")]
        public static void RebuildBaseAnimatorController()
        {
            EnsureFolder("Assets/_Project/AnimatorControllers/Base");

            var savedMotions = new Dictionary<string, Motion>();
            AnimatorController existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(BaseAcPath);
            if (existing != null)
            {
                foreach (AnimatorControllerLayer layer in existing.layers)
                {
                    foreach (ChildAnimatorState child in layer.stateMachine.states)
                    {
                        if (child.state != null && child.state.motion != null)
                        {
                            savedMotions[child.state.name] = child.state.motion;
                        }
                    }
                }
            }

            AnimatorController ac = CreateBaseAnimatorController();

            int restored = 0;
            foreach (AnimatorControllerLayer layer in ac.layers)
            {
                foreach (ChildAnimatorState child in layer.stateMachine.states)
                {
                    if (savedMotions.TryGetValue(child.state.name, out Motion motion))
                    {
                        child.state.motion = motion;
                        restored++;
                    }
                }
            }

            EditorUtility.SetDirty(ac);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ProjectAlphaSetup] Base Animator Controller rebuilt with Idle/Walk/Run. " +
                      $"Re-applied {restored} existing clip(s) by state name — now assign the Walk clip to the Walk state.");
        }

        // ---------------------------------------------------------------- Folders & tags

        private static void CreateFolders()
        {
            string[] folders =
            {
                "Assets/_Project",
                "Assets/_Project/Animations",
                "Assets/_Project/Animations/Shared",
                "Assets/_Project/Animations/Warrior",
                "Assets/_Project/Animations/Vocation2",
                "Assets/_Project/Animations/Vocation3",
                "Assets/_Project/Animations/Vocation4",
                "Assets/_Project/Animations/Vocation5",
                "Assets/_Project/Animations/Vocation6",
                "Assets/_Project/AnimatorControllers",
                "Assets/_Project/AnimatorControllers/Base",
                "Assets/_Project/AnimatorControllers/Overrides",
                "Assets/_Project/Input",
                "Assets/_Project/Prefabs",
                "Assets/_Project/Prefabs/Player",
                "Assets/_Project/Prefabs/Weapons",
                "Assets/_Project/ScriptableObjects",
                "Assets/_Project/ScriptableObjects/Vocations",
                "Assets/_Project/Scripts",
                "Assets/_Project/Scripts/Camera",
                "Assets/_Project/Scripts/Core",
                "Assets/_Project/Scripts/Input",
                "Assets/_Project/Scripts/Player",
                "Assets/_Project/Scripts/Player/Movement",
                "Assets/_Project/Scripts/Player/Vocations",
                "Assets/_Project/Scenes"
            };

            foreach (string folder in folders)
            {
                EnsureFolder(folder);
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static void AddTag(string tag)
        {
            UnityEngine.Object[] managers = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (managers == null || managers.Length == 0)
            {
                return;
            }

            SerializedObject so = new SerializedObject(managers[0]);
            SerializedProperty tags = so.FindProperty("tags");

            for (int i = 0; i < tags.arraySize; i++)
            {
                if (tags.GetArrayElementAtIndex(i).stringValue == tag)
                {
                    return;
                }
            }

            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
            so.ApplyModifiedProperties();
        }

        // ---------------------------------------------------------------- Input reader

        private static InputReader CreateInputReader(InputActionAsset inputAsset)
        {
            DeleteIfExists(InputReaderPath);
            InputReader reader = ScriptableObject.CreateInstance<InputReader>();
            AssetDatabase.CreateAsset(reader, InputReaderPath);

            SerializedObject so = new SerializedObject(reader);
            so.FindProperty("actions").objectReferenceValue = inputAsset;
            so.ApplyModifiedProperties();
            return reader;
        }

        // ---------------------------------------------------------------- Animator controllers

        private static AnimatorController CreateBaseAnimatorController()
        {
            // Rebuild in place (preserve the asset GUID) so the AOCs and the model's Animator
            // keep their reference to this controller across rebuilds.
            AnimatorController ac = AssetDatabase.LoadAssetAtPath<AnimatorController>(BaseAcPath);
            if (ac == null)
            {
                ac = AnimatorController.CreateAnimatorControllerAtPath(BaseAcPath);
            }
            else
            {
                while (ac.parameters.Length > 0)
                {
                    ac.RemoveParameter(0);
                }

                AnimatorStateMachine root = ac.layers[0].stateMachine;
                foreach (AnimatorStateTransition t in root.anyStateTransitions)
                {
                    root.RemoveAnyStateTransition(t);
                }
                foreach (ChildAnimatorState child in root.states)
                {
                    root.RemoveState(child.state);
                }
            }

            ac.AddParameter("moveSpeed", AnimatorControllerParameterType.Float);
            ac.AddParameter("isGrounded", AnimatorControllerParameterType.Bool);
            ac.AddParameter("isClimbing", AnimatorControllerParameterType.Bool);
            ac.AddParameter("vocationIndex", AnimatorControllerParameterType.Int);
            ac.AddParameter("verticalVelocity", AnimatorControllerParameterType.Float);
            ac.AddParameter("isSprinting", AnimatorControllerParameterType.Bool);

            // isGrounded defaults to true.
            AnimatorControllerParameter[] parameters = ac.parameters;
            foreach (AnimatorControllerParameter p in parameters)
            {
                if (p.name == "isGrounded")
                {
                    p.defaultBool = true;
                }
            }
            ac.parameters = parameters;

            AnimatorStateMachine sm = ac.layers[0].stateMachine;
            AnimatorState idle = sm.AddState("Idle");
            AnimatorState walk = sm.AddState("Walk");
            AnimatorState run = sm.AddState("Run");
            AnimatorState rise = sm.AddState("Jump_Rise");
            AnimatorState fall = sm.AddState("Jump_Fall");
            AnimatorState land = sm.AddState("Land");
            AnimatorState climb = sm.AddState("Climb");
            sm.defaultState = idle;

            // Idle -> Walk or Run depending on whether the player is sprinting.
            AnimatorStateTransition idleToWalk = idle.AddTransition(walk);
            idleToWalk.hasExitTime = false;
            idleToWalk.duration = 0.1f;
            idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "moveSpeed");
            idleToWalk.AddCondition(AnimatorConditionMode.If, 0f, "isGrounded");
            idleToWalk.AddCondition(AnimatorConditionMode.IfNot, 0f, "isSprinting");

            AnimatorStateTransition idleToRun = idle.AddTransition(run);
            idleToRun.hasExitTime = false;
            idleToRun.duration = 0.1f;
            idleToRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "moveSpeed");
            idleToRun.AddCondition(AnimatorConditionMode.If, 0f, "isGrounded");
            idleToRun.AddCondition(AnimatorConditionMode.If, 0f, "isSprinting");

            // Walk <-> Run switch on the sprint flag while still moving.
            AnimatorStateTransition walkToRun = walk.AddTransition(run);
            walkToRun.hasExitTime = false;
            walkToRun.duration = 0.1f;
            walkToRun.AddCondition(AnimatorConditionMode.If, 0f, "isSprinting");
            walkToRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "moveSpeed");

            AnimatorStateTransition runToWalk = run.AddTransition(walk);
            runToWalk.hasExitTime = false;
            runToWalk.duration = 0.1f;
            runToWalk.AddCondition(AnimatorConditionMode.IfNot, 0f, "isSprinting");
            runToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "moveSpeed");

            // Back to Idle when movement stops.
            AnimatorStateTransition walkToIdle = walk.AddTransition(idle);
            walkToIdle.hasExitTime = false;
            walkToIdle.duration = 0.1f;
            walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "moveSpeed");

            AnimatorStateTransition runToIdle = run.AddTransition(idle);
            runToIdle.hasExitTime = false;
            runToIdle.duration = 0.1f;
            runToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "moveSpeed");

            // Airborne states via AnyState.
            AnimatorStateTransition toRise = sm.AddAnyStateTransition(rise);
            toRise.hasExitTime = false;
            toRise.duration = 0.05f;
            toRise.canTransitionToSelf = false;
            toRise.AddCondition(AnimatorConditionMode.IfNot, 0f, "isGrounded");
            toRise.AddCondition(AnimatorConditionMode.Greater, 0f, "verticalVelocity");

            AnimatorStateTransition toFall = sm.AddAnyStateTransition(fall);
            toFall.hasExitTime = false;
            toFall.duration = 0.05f;
            toFall.canTransitionToSelf = false;
            toFall.AddCondition(AnimatorConditionMode.IfNot, 0f, "isGrounded");
            toFall.AddCondition(AnimatorConditionMode.Less, 0.01f, "verticalVelocity");

            AnimatorStateTransition fallToLand = fall.AddTransition(land);
            fallToLand.hasExitTime = false;
            fallToLand.duration = 0.05f;
            fallToLand.AddCondition(AnimatorConditionMode.If, 0f, "isGrounded");

            // Land exits back to Idle after a short placeholder time.
            AnimatorStateTransition landToIdle = land.AddTransition(idle);
            landToIdle.hasExitTime = true;
            landToIdle.exitTime = 0.9f;
            landToIdle.duration = 0.1f;

            // Climb via AnyState.
            AnimatorStateTransition toClimb = sm.AddAnyStateTransition(climb);
            toClimb.hasExitTime = false;
            toClimb.duration = 0.05f;
            toClimb.canTransitionToSelf = false;
            toClimb.AddCondition(AnimatorConditionMode.If, 0f, "isClimbing");

            AnimatorStateTransition climbToIdle = climb.AddTransition(idle);
            climbToIdle.hasExitTime = false;
            climbToIdle.duration = 0.1f;
            climbToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "isClimbing");

            EditorUtility.SetDirty(ac);
            return ac;
        }

        private static Dictionary<VocationType, AnimatorOverrideController> CreateOverrideControllers(AnimatorController baseAc)
        {
            var result = new Dictionary<VocationType, AnimatorOverrideController>();

            for (int i = 0; i < VocationNames.Length; i++)
            {
                string path = $"{ProjectRoot}/AnimatorControllers/Overrides/{VocationNames[i]}_AOC.overrideController";
                DeleteIfExists(path);

                AnimatorOverrideController aoc = new AnimatorOverrideController(baseAc);
                aoc.name = $"{VocationNames[i]}_AOC";
                AssetDatabase.CreateAsset(aoc, path);
                result[(VocationType)i] = aoc;
            }

            return result;
        }

        // ---------------------------------------------------------------- Prefabs & data

        private static GameObject CreateWeaponPrefab()
        {
            DeleteIfExists(WeaponPrefabPath);

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Weapon_SwordShield";
            go.transform.localScale = new Vector3(0.12f, 0.12f, 1.0f);
            RemoveColliders(go);
            go.AddComponent<WarriorWeapon>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, WeaponPrefabPath);
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }

        private static List<VocationData> CreateVocationData(
            GameObject weaponPrefab,
            Dictionary<VocationType, AnimatorOverrideController> aocs)
        {
            var list = new List<VocationData>();

            for (int i = 0; i < VocationNames.Length; i++)
            {
                VocationType type = (VocationType)i;
                string path = $"{ProjectRoot}/ScriptableObjects/Vocations/{VocationNames[i]}_Data.asset";
                DeleteIfExists(path);

                VocationData data = ScriptableObject.CreateInstance<VocationData>();
                data.Vocation = type;
                data.DisplayName = VocationNames[i];
                data.MoveSpeedModifier = 1f;
                // All vocations reference their own AOC so the runtime swap is testable (GDD 7.2).
                data.CombatAnimatorOverride = aocs[type];
                // Only Warrior has a weapon prefab in Part 1; others stay null (handled gracefully).
                data.WeaponPrefab = type == VocationType.Warrior ? weaponPrefab : null;

                AssetDatabase.CreateAsset(data, path);
                list.Add(data);
            }

            return list;
        }

        private static GameObject CreatePlayerPrefab(InputReader inputReader, AnimatorController baseAc)
        {
            DeleteIfExists(PlayerPrefabPath);

            GameObject root = new GameObject("Player_Warrior");

            CharacterController cc = root.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.slopeLimit = 45f;
            cc.stepOffset = 0.3f;
            cc.skinWidth = 0.08f;

            PlayerMotor motor = root.AddComponent<PlayerMotor>();
            PlayerJump jump = root.AddComponent<PlayerJump>();
            PlayerMovement movement = root.AddComponent<PlayerMovement>();
            PlayerClimb climb = root.AddComponent<PlayerClimb>();

            // [Model] child with the Animator.
            GameObject model = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            model.name = "[Model]";
            model.transform.SetParent(root.transform, false);
            model.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            model.transform.localScale = new Vector3(0.8f, 0.9f, 0.8f);
            RemoveColliders(model);
            Animator animator = model.AddComponent<Animator>();
            animator.runtimeAnimatorController = baseAc;

            // WeaponAnchor child.
            GameObject anchor = new GameObject("WeaponAnchor");
            anchor.transform.SetParent(root.transform, false);
            anchor.transform.localPosition = new Vector3(0.5f, 1.0f, 0f);

            PlayerAnimator playerAnimator = root.AddComponent<PlayerAnimator>();
            WeaponEquipper equipper = root.AddComponent<WeaponEquipper>();

            // Wire references (vocationManager left null here — it is a scene object, wired in BuildScene).
            SetRef(movement, "inputReader", inputReader);
            SetRef(movement, "motor", motor);
            SetRef(movement, "climb", climb);
            SetRef(movement, "jump", jump);

            SetRef(jump, "inputReader", inputReader);
            SetRef(jump, "motor", motor);

            SetRef(climb, "inputReader", inputReader);
            SetRef(climb, "motor", motor);
            SetRef(climb, "jump", jump);

            SetRef(playerAnimator, "animator", animator);
            SetRef(playerAnimator, "motor", motor);
            SetRef(playerAnimator, "jump", jump);
            SetRef(playerAnimator, "climb", climb);
            SetRef(playerAnimator, "movement", movement);

            SetRef(equipper, "weaponAnchor", anchor.transform);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        // ---------------------------------------------------------------- Scene

        private static void BuildScene(GameObject playerPrefab, List<VocationData> vocations)
        {
            UnityEngine.SceneManagement.Scene scene =
                EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Ground.
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.tag = "Ground";
            ground.transform.localScale = new Vector3(5f, 1f, 5f);

            // Climbable wall (trigger volume the player can stand within).
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Climbable_Wall";
            wall.tag = "Climbable";
            wall.transform.position = new Vector3(4f, 3f, 0f);
            wall.transform.localScale = new Vector3(2f, 6f, 0.5f);
            BoxCollider wallCollider = wall.GetComponent<BoxCollider>();
            if (wallCollider != null)
            {
                wallCollider.isTrigger = true;
            }

            // Player.
            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.position = Vector3.zero;

            // VocationManager.
            GameObject vmGo = new GameObject("VocationManager");
            VocationManager vm = vmGo.AddComponent<VocationManager>();

            SerializedObject vmSo = new SerializedObject(vm);
            SerializedProperty listProp = vmSo.FindProperty("allVocations");
            listProp.arraySize = vocations.Count;
            for (int i = 0; i < vocations.Count; i++)
            {
                listProp.GetArrayElementAtIndex(i).objectReferenceValue = vocations[i];
            }
            vmSo.FindProperty("startingVocation").objectReferenceValue = vocations[0]; // Warrior
            vmSo.ApplyModifiedProperties();

            // Wire the scene VocationManager into the player instance.
            PlayerAnimator playerAnimator = player.GetComponent<PlayerAnimator>();
            WeaponEquipper equipper = player.GetComponent<WeaponEquipper>();
            SetRef(playerAnimator, "vocationManager", vm);
            SetRef(equipper, "vocationManager", vm);

            // Camera (Cinemachine via reflection so a package/API mismatch cannot break this tool).
            SetupCinemachine(player.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        // ---------------------------------------------------------------- Cinemachine (reflection)

        private static void SetupCinemachine(Transform follow)
        {
            try
            {
                Type camType = FindType("Unity.Cinemachine.CinemachineCamera");
                Type brainType = FindType("Unity.Cinemachine.CinemachineBrain");
                Type composerType = FindType("Unity.Cinemachine.CinemachinePositionComposer");

                if (camType == null)
                {
                    Debug.LogWarning("[ProjectAlphaSetup] Cinemachine not found yet. Once the package finishes " +
                                     "importing, re-run 'Project Alpha/Build Part 1' to add CM_PlayerFollowCam, " +
                                     "or configure the camera manually per GDD Section 4.");
                    return;
                }

                Camera mainCam = Camera.main;
                if (mainCam != null && brainType != null && mainCam.GetComponent(brainType) == null)
                {
                    mainCam.gameObject.AddComponent(brainType);
                }

                GameObject go = new GameObject("CM_PlayerFollowCam");
                go.transform.position = new Vector3(0f, 0.5f, -10f);
                Component cam = go.AddComponent(camType);

                // Set the Follow target.
                PropertyInfo followProp = camType.GetProperty("Follow");
                if (followProp != null && followProp.CanWrite)
                {
                    followProp.SetValue(cam, follow);
                }

                if (composerType != null)
                {
                    Component composer = go.AddComponent(composerType);
                    ConfigureComposer(composer);
                }

                Debug.Log("[ProjectAlphaSetup] CM_PlayerFollowCam created. Verify damping/dead-zone against " +
                          "GDD Section 4 (X damp 0.1, Y damp 0.2, Y dead zone ~1.5 world units) in the Inspector.");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ProjectAlphaSetup] Camera setup incomplete: {e.Message}. Configure the " +
                                 "Cinemachine camera manually per GDD Section 4.");
            }
        }

        private static void ConfigureComposer(Component composer)
        {
            SerializedObject so = new SerializedObject(composer);

            TrySetFloat(so, "CameraDistance", 10f);
            TrySetVector3(so, "Damping", new Vector3(0.1f, 0.2f, 0f));
            TrySetVector3(so, "TargetOffset", new Vector3(0f, 0.5f, 0f));

            so.ApplyModifiedProperties();
        }

        // ---------------------------------------------------------------- Helpers

        private static void SetRef(Component component, string field, UnityEngine.Object value)
        {
            SerializedObject so = new SerializedObject(component);
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogWarning($"[ProjectAlphaSetup] Field '{field}' not found on {component.GetType().Name}.");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedProperties();
        }

        private static void TrySetFloat(SerializedObject so, string path, float value)
        {
            SerializedProperty p = so.FindProperty(path);
            if (p != null)
            {
                p.floatValue = value;
            }
        }

        private static void TrySetVector3(SerializedObject so, string path, Vector3 value)
        {
            SerializedProperty p = so.FindProperty(path);
            if (p != null && p.propertyType == SerializedPropertyType.Vector3)
            {
                p.vector3Value = value;
            }
        }

        private static void RemoveColliders(GameObject go)
        {
            foreach (Collider c in go.GetComponents<Collider>())
            {
                UnityEngine.Object.DestroyImmediate(c);
            }
        }

        private static void DeleteIfExists(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }
        }

        private static Type FindType(string fullName)
        {
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type t = asm.GetType(fullName);
                if (t != null)
                {
                    return t;
                }
            }
            return null;
        }
    }
}

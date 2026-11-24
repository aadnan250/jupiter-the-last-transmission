using Jupiter.Audio;
using Jupiter.Player;
using Jupiter.UI;
using Jupiter.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Jupiter.EditorTools
{
    /// <summary>
    /// Builds the reusable PlayerRig prefab: first-person controller, camera, interaction, footsteps and HUD.
    /// Every level instantiates this prefab, so player changes happen in one place.
    /// </summary>
    public static class PlayerRigBuilder
    {
        public const string PrefabPath = "Assets/_Project/Prefabs/Player/PlayerRig.prefab";
        const string InputAssetPath = "Assets/_Project/Settings/Input/JupiterControls.inputactions";
        const string EarthProfilePath = "Assets/_Project/Data/Environments/Earth.asset";

        public static GameObject BuildPrefab()
        {
            GreyboxKit.EnsureFolder("Assets/_Project/Prefabs/Player");

            var root = new GameObject("PlayerRig");
            root.tag = "Player";

            var cc = root.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0, 0.9f, 0);
            cc.stepOffset = 0.3f;
            cc.slopeLimit = 45f;
            cc.skinWidth = 0.03f;
            // Default 0.001 silently drops tiny moves at high frame rates, which breaks isGrounded.
            cc.minMoveDistance = 0f;

            var input = root.AddComponent<InputReader>();
            var inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
            if (inputAsset == null) Debug.LogError($"Input actions not found at {InputAssetPath}");
            GreyboxKit.Set(input, "actions", inputAsset);

            var controller = root.AddComponent<PlayerController>();
            GreyboxKit.Set(controller, "input", input);
            GreyboxKit.Set(controller, "defaultEnvironment", AssetDatabase.LoadAssetAtPath<EnvironmentProfile>(EarthProfilePath));
            root.AddComponent<CursorLock>();

            // Camera
            var pivot = new GameObject("CameraPivot");
            pivot.transform.SetParent(root.transform, false);
            pivot.transform.localPosition = new Vector3(0, 1.62f, 0);
            var cam = pivot.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 2000f;
            cam.fieldOfView = 70f;
            pivot.AddComponent<AudioListener>();
            var look = pivot.AddComponent<CameraController>();
            GreyboxKit.Set(look, "input", input);
            GreyboxKit.Set(look, "player", controller);
            var interaction = pivot.AddComponent<PlayerInteraction>();
            GreyboxKit.Set(interaction, "input", input);

            // Footsteps
            var feet = new GameObject("Feet");
            feet.transform.SetParent(root.transform, false);
            feet.AddComponent<AudioSource>();
            var steps = feet.AddComponent<Footsteps>();
            GreyboxKit.Set(steps, "player", controller);
            GreyboxKit.Set(steps, "clips", AudioAssetGenerator.LoadFootsteps());

            BuildHud(root.transform, interaction, input);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>Instantiates the rig in the open scene, facing <paramref name="yaw"/>, with a scene-specific environment.</summary>
        public static GameObject Spawn(Vector3 position, float yaw, EnvironmentProfile environment)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var rig = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            rig.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            GreyboxKit.Set(rig.GetComponent<PlayerController>(), "defaultEnvironment", environment);
            return rig;
        }

        static void BuildHud(Transform parent, PlayerInteraction interaction, InputReader input)
        {
            var hud = new GameObject("HUD");
            hud.transform.SetParent(parent, false);
            var canvas = hud.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = hud.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var cross = NewUI<Image>(hud.transform, "Crosshair");
            cross.rectTransform.sizeDelta = new Vector2(4, 4);
            cross.raycastTarget = false;

            var prompt = NewText(hud.transform, "Prompt", font, 18, TextAnchor.UpperCenter, new Color(1, 1, 1, 0.9f));
            prompt.rectTransform.anchoredPosition = new Vector2(0, -24);
            prompt.rectTransform.sizeDelta = new Vector2(300, 30);

            var help = NewText(hud.transform, "ControlsHelp", font, 13, TextAnchor.LowerLeft, new Color(1, 1, 1, 0.5f));
            help.rectTransform.anchorMin = help.rectTransform.anchorMax = help.rectTransform.pivot = Vector2.zero;
            help.rectTransform.anchoredPosition = new Vector2(16, 12);
            help.rectTransform.sizeDelta = new Vector2(1000, 22);
            help.text = "WASD move   Shift run   Ctrl/C crouch   Space jump   E interact   F3 performance   Esc free cursor";

            var promptUi = hud.AddComponent<InteractionPromptUI>();
            GreyboxKit.Set(promptUi, "interaction", interaction);
            GreyboxKit.Set(promptUi, "promptText", prompt);
            GreyboxKit.Set(promptUi, "crosshair", cross);

            // Reading panel
            var panel = NewUI<Image>(hud.transform, "ReadPanel");
            panel.color = new Color(0.04f, 0.05f, 0.06f, 0.92f);
            panel.rectTransform.sizeDelta = new Vector2(640, 440);
            var title = NewText(panel.transform, "Title", font, 22, TextAnchor.UpperLeft, new Color(0.95f, 0.85f, 0.6f));
            title.rectTransform.anchoredPosition = new Vector2(0, 180);
            title.rectTransform.sizeDelta = new Vector2(580, 40);
            var body = NewText(panel.transform, "Body", font, 17, TextAnchor.UpperLeft, new Color(0.92f, 0.92f, 0.9f));
            body.rectTransform.anchoredPosition = new Vector2(0, -20);
            body.rectTransform.sizeDelta = new Vector2(580, 340);
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Truncate;
            body.lineSpacing = 1.15f;
            var hint = NewText(panel.transform, "Hint", font, 13, TextAnchor.LowerRight, new Color(1, 1, 1, 0.5f));
            hint.rectTransform.anchoredPosition = new Vector2(0, -200);
            hint.rectTransform.sizeDelta = new Vector2(580, 24);
            hint.text = "E / Esc  close";

            var readUi = hud.AddComponent<ReadableUI>();
            GreyboxKit.Set(readUi, "panel", panel.gameObject);
            GreyboxKit.Set(readUi, "titleText", title);
            GreyboxKit.Set(readUi, "bodyText", body);
            GreyboxKit.Set(readUi, "input", input);

            hud.AddComponent<DevStatsOverlay>();
        }

        static T NewUI<T>(Transform parent, string name) where T : Graphic
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var g = go.AddComponent<T>();
            g.raycastTarget = false;
            return g;
        }

        static Text NewText(Transform parent, string name, Font font, int size, TextAnchor anchor, Color color)
        {
            var t = NewUI<Text>(parent, name);
            t.font = font;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = color;
            return t;
        }
    }
}

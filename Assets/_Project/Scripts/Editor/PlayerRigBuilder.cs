using Jupiter.Audio;
using Jupiter.Narrative;
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
    /// Builds the reusable PlayerRig prefab: first-person controller, camera, interaction, footsteps,
    /// HUD (prompt, reading panel, dialogue with four replies, objective tracker), the dialogue manager,
    /// consequence feedback and controller-aware button prompts. Every level instantiates this prefab.
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
            root.AddComponent<InputDeviceWatcher>();

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
            CombatRigBuilder.AddCombat(root, input, pivot.transform);

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

            // Consequence pulse sits behind everything else on the HUD.
            var pulse = NewUI<Image>(hud.transform, "ConsequencePulse");
            Stretch(pulse.rectTransform);
            pulse.color = new Color(0.12f, 0f, 0.02f, 0f);

            var cross = NewUI<Image>(hud.transform, "Crosshair");
            cross.rectTransform.sizeDelta = new Vector2(4, 4);

            var prompt = NewText(hud.transform, "Prompt", font, 18, TextAnchor.UpperCenter, new Color(1, 1, 1, 0.9f));
            prompt.rectTransform.anchoredPosition = new Vector2(0, -24);
            prompt.rectTransform.sizeDelta = new Vector2(420, 30);

            var help = NewText(hud.transform, "ControlsHelp", font, 13, TextAnchor.LowerLeft, new Color(1, 1, 1, 0.5f));
            help.rectTransform.anchorMin = help.rectTransform.anchorMax = help.rectTransform.pivot = Vector2.zero;
            help.rectTransform.anchoredPosition = new Vector2(16, 12);
            help.rectTransform.sizeDelta = new Vector2(1100, 22);

            var promptUi = hud.AddComponent<InteractionPromptUI>();
            GreyboxKit.Set(promptUi, "interaction", interaction);
            GreyboxKit.Set(promptUi, "promptText", prompt);
            GreyboxKit.Set(promptUi, "crosshair", cross);
            GreyboxKit.Set(promptUi, "controlsHelpText", help);

            // Objective tracker (top right). Filled in by the level's MissionManager.
            var objective = NewText(hud.transform, "Objective", font, 16, TextAnchor.UpperRight, new Color(1, 1, 1, 0.85f));
            objective.rectTransform.anchorMin = objective.rectTransform.anchorMax = objective.rectTransform.pivot = new Vector2(1, 1);
            objective.rectTransform.anchoredPosition = new Vector2(-20, -16);
            objective.rectTransform.sizeDelta = new Vector2(420, 60);

            BuildReadPanel(hud.transform, font, input);
            var dialogueUi = BuildDialoguePanel(hud.transform, font);

            // Dialogue manager + radio
            var radio = hud.AddComponent<AudioSource>();
            radio.playOnAwake = false;
            radio.spatialBlend = 0f;
            var dialogue = hud.AddComponent<DialogueManager>();
            GreyboxKit.Set(dialogue, "input", input);
            GreyboxKit.Set(dialogue, "ui", dialogueUi);
            GreyboxKit.Set(dialogue, "radioSource", radio);
            GreyboxKit.Set(dialogue, "radioSquelch", AudioAssetGenerator.Load(AudioAssetGenerator.RadioSquelch));

            // Sam's own voice: centred, 2D.
            var samVoice = new GameObject("PlayerVoice");
            samVoice.transform.SetParent(hud.transform, false);
            var samSource = samVoice.AddComponent<AudioSource>();
            samSource.playOnAwake = false;
            samSource.spatialBlend = 0f;
            samSource.volume = 0.85f;
            GreyboxKit.Set(dialogue, "playerVoice", samSource);

            // Radio voice: band-limited to a radio's 300-3400 Hz with light distortion.
            var radioVoiceGo = new GameObject("RadioVoice");
            radioVoiceGo.transform.SetParent(hud.transform, false);
            var radioVoice = radioVoiceGo.AddComponent<AudioSource>();
            radioVoice.playOnAwake = false;
            radioVoice.spatialBlend = 0f;
            radioVoiceGo.AddComponent<AudioHighPassFilter>().cutoffFrequency = 320f;
            radioVoiceGo.AddComponent<AudioLowPassFilter>().cutoffFrequency = 3300f;
            radioVoiceGo.AddComponent<AudioDistortionFilter>().distortionLevel = 0.32f;
            GreyboxKit.Set(dialogue, "radioVoice", radioVoice);

            // Consequence feedback (sting + pulse + rumble + PS light bar)
            var stingGo = new GameObject("ConsequenceFeedback");
            stingGo.transform.SetParent(hud.transform, false);
            var stingSource = stingGo.AddComponent<AudioSource>();
            stingSource.playOnAwake = false;
            stingSource.spatialBlend = 0f;
            var feedback = stingGo.AddComponent<ConsequenceFeedback>();
            GreyboxKit.Set(feedback, "dialogue", dialogue);
            GreyboxKit.Set(feedback, "audioSource", stingSource);
            GreyboxKit.Set(feedback, "sting", AudioAssetGenerator.Load(AudioAssetGenerator.ConsequenceSting));
            GreyboxKit.Set(feedback, "screenPulse", pulse);

            hud.AddComponent<DevStatsOverlay>();
        }

        static void BuildReadPanel(Transform hud, Font font, InputReader input)
        {
            var panel = NewUI<Image>(hud, "ReadPanel");
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
            hint.text = "Interact / Esc  close";

            var readUi = hud.gameObject.AddComponent<ReadableUI>();
            GreyboxKit.Set(readUi, "panel", panel.gameObject);
            GreyboxKit.Set(readUi, "titleText", title);
            GreyboxKit.Set(readUi, "bodyText", body);
            GreyboxKit.Set(readUi, "input", input);
        }

        static DialogueUI BuildDialoguePanel(Transform hud, Font font)
        {
            // Bottom-centre subtitle panel
            var panel = NewUI<Image>(hud, "DialoguePanel");
            panel.color = new Color(0.03f, 0.04f, 0.05f, 0.78f);
            var rt = panel.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0, 44);
            rt.sizeDelta = new Vector2(860, 190);

            var speaker = NewText(panel.transform, "Speaker", font, 20, TextAnchor.UpperLeft, Color.white);
            speaker.fontStyle = FontStyle.Bold;
            speaker.rectTransform.anchoredPosition = new Vector2(0, 66);
            speaker.rectTransform.sizeDelta = new Vector2(800, 30);

            var line = NewText(panel.transform, "Line", font, 19, TextAnchor.UpperLeft, new Color(0.95f, 0.95f, 0.93f));
            line.rectTransform.anchoredPosition = new Vector2(0, -6);
            line.rectTransform.sizeDelta = new Vector2(800, 110);
            line.horizontalOverflow = HorizontalWrapMode.Wrap;
            line.lineSpacing = 1.1f;

            var hint = NewText(panel.transform, "ContinueHint", font, 13, TextAnchor.LowerRight, new Color(1, 1, 1, 0.5f));
            hint.rectTransform.anchoredPosition = new Vector2(0, -76);
            hint.rectTransform.sizeDelta = new Vector2(800, 22);

            var choices = new Text[4];
            for (int i = 0; i < 4; i++)
            {
                var c = NewText(panel.transform, $"Choice{i + 1}", font, 18, TextAnchor.MiddleLeft, new Color(0.95f, 0.92f, 0.85f));
                c.rectTransform.anchoredPosition = new Vector2(0, 63 - i * 42);
                c.rectTransform.sizeDelta = new Vector2(800, 38);
                c.horizontalOverflow = HorizontalWrapMode.Wrap;
                choices[i] = c;
            }

            var ui = hud.gameObject.AddComponent<DialogueUI>();
            GreyboxKit.Set(ui, "root", panel.gameObject);
            GreyboxKit.Set(ui, "speakerText", speaker);
            GreyboxKit.Set(ui, "lineText", line);
            GreyboxKit.Set(ui, "continueHint", hint);
            GreyboxKit.Set(ui, "choiceTexts", choices);
            return ui;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
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
            t.supportRichText = true;
            return t;
        }
    }
}

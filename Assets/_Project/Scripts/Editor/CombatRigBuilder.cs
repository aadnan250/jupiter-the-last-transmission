using Jupiter.Combat;
using Jupiter.Player;
using Jupiter.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Jupiter.EditorTools
{
    /// <summary>
    /// Adds survival and combat to the PlayerRig prefab: health, inventory, the bolt driver (unequipped by default;
    /// levels where Sam is armed enable it), the stealth tools (hiding, throwing, the flare pistol), and the
    /// health/ammo/tool HUD with a damage flash.
    /// </summary>
    public static class CombatRigBuilder
    {
        public static void AddCombat(GameObject root, InputReader input, Transform camera)
        {
            var health = root.AddComponent<PlayerHealth>();
            var inventory = root.AddComponent<PlayerInventory>();
            GreyboxKit.Set(inventory, "input", input);
            GreyboxKit.Set(inventory, "health", health);

            // Muzzle flash light on the camera
            var flashGo = new GameObject("MuzzleFlash");
            flashGo.transform.SetParent(camera, false);
            flashGo.transform.localPosition = new Vector3(0.2f, -0.15f, 0.5f);
            var flash = flashGo.AddComponent<Light>();
            flash.type = LightType.Point;
            flash.range = 8f;
            flash.intensity = 4f;
            flash.color = new Color(0.8f, 0.9f, 1f);
            flash.shadows = LightShadows.None;
            flash.enabled = false;

            var weaponAudio = root.AddComponent<AudioSource>();
            weaponAudio.playOnAwake = false;
            weaponAudio.spatialBlend = 0f;

            var driver = root.AddComponent<BoltDriver>();
            GreyboxKit.Set(driver, "input", input);
            GreyboxKit.Set(driver, "inventory", inventory);
            GreyboxKit.Set(driver, "muzzle", camera);
            GreyboxKit.Set(driver, "audioSource", weaponAudio);
            GreyboxKit.Set(driver, "fireClip", CombatAudioGenerator.Load(CombatAudioGenerator.BoltShot));
            GreyboxKit.Set(driver, "reloadClip", CombatAudioGenerator.Load(CombatAudioGenerator.Reload));
            GreyboxKit.Set(driver, "flash", flash);

            // The arc beam rifle (unequipped by default; the landing party carries it) and Sam's arms holding it.
            var beam = AddBeamRifle(root, input, inventory, camera, weaponAudio);
            var pistol = AddSidearm(root, input, inventory, camera, weaponAudio);
            camera.gameObject.AddComponent<CameraShake>();

            // Stealth tools: hiding, one throwable in hand, Mara's flare pistol (unequipped until picked up).
            var concealment = root.AddComponent<PlayerConcealment>();
            GreyboxKit.Set(concealment, "input", input);
            var thrower = root.AddComponent<PlayerThrower>();
            GreyboxKit.Set(thrower, "input", input);
            GreyboxKit.Set(thrower, "aim", camera);
            var flarePistol = root.AddComponent<FlarePistol>();
            GreyboxKit.Set(flarePistol, "input", input);
            GreyboxKit.Set(flarePistol, "inventory", inventory);
            GreyboxKit.Set(flarePistol, "muzzle", camera);
            GreyboxKit.Set(flarePistol, "audioSource", weaponAudio);
            GreyboxKit.Set(flarePistol, "fireClip", CombatAudioGenerator.Load(CombatAudioGenerator.FlareShot));
            GreyboxKit.Set(flarePistol, "flareMaterial",
                GreyboxKit.Mat("FX_Flare", new Color(1f, 0.3f, 0.2f), new Color(1f, 0.25f, 0.12f) * 6f));

            // HUD: bottom-right health and ammo, full-screen damage flash.
            var hud = root.transform.Find("HUD");
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var flashImg = new GameObject("DamageFlash", typeof(RectTransform)).AddComponent<Image>();
            flashImg.transform.SetParent(hud, false);
            flashImg.transform.SetSiblingIndex(0);
            flashImg.raycastTarget = false;
            flashImg.color = new Color(0.6f, 0f, 0f, 0f);
            var frt = flashImg.rectTransform;
            frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one; frt.offsetMin = frt.offsetMax = Vector2.zero;

            var healthText = Corner(hud, "HealthText", font, new Vector2(-20, 44));
            var ammoText = Corner(hud, "AmmoText", font, new Vector2(-20, 16));
            var toolText = Corner(hud, "ToolText", font, new Vector2(-20, 98));

            var combatHud = hud.gameObject.AddComponent<CombatHUD>();
            GreyboxKit.Set(combatHud, "health", health);
            GreyboxKit.Set(combatHud, "weapon", driver);
            GreyboxKit.Set(combatHud, "beam", beam);
            GreyboxKit.Set(combatHud, "pistol", pistol);
            GreyboxKit.Set(combatHud, "inventory", inventory);
            GreyboxKit.Set(combatHud, "healthText", healthText);
            GreyboxKit.Set(combatHud, "ammoText", ammoText);
            GreyboxKit.Set(combatHud, "damageFlash", flashImg);
            GreyboxKit.Set(combatHud, "thrower", thrower);
            GreyboxKit.Set(combatHud, "concealment", concealment);
            GreyboxKit.Set(combatHud, "toolText", toolText);

            // On Europa's surface: the suit (integrity, radiation dose) and sound through the suit only.
            var suit = root.AddComponent<PlayerSuit>();
            GreyboxKit.Set(suit, "health", health);
            GreyboxKit.Set(suit, "player", root.GetComponent<PlayerController>());
            GreyboxKit.Set(combatHud, "suit", suit);
            var vacuum = root.AddComponent<Jupiter.Audio.VacuumAudio>();
            GreyboxKit.Set(vacuum, "player", root.GetComponent<PlayerController>());
        }

        /// <summary>
        /// The beam rifle, its visible beam and impact light, and the first-person view model: suited forearms and
        /// gloves holding a sleek rifle with glowing coils and a muzzle emitter, parented to the camera.
        /// </summary>
        static BeamRifle AddBeamRifle(GameObject root, InputReader input, PlayerInventory inventory, Transform camera, AudioSource oneShots)
        {
            var beamMat = BeamMaterial("FX_ArcBeam", new Color(0.5f, 1.8f, 2.6f));
            var suit = GreyboxKit.Mat("VM_SuitWhite", new Color(0.86f, 0.87f, 0.85f), smoothness: 0.35f);
            var glove = GreyboxKit.Mat("VM_Glove", new Color(0.22f, 0.23f, 0.25f), smoothness: 0.3f);
            var gunDark = GreyboxKit.Mat("VM_GunDark", new Color(0.09f, 0.1f, 0.11f), smoothness: 0.65f);
            var gunWhite = GreyboxKit.Mat("VM_GunWhite", new Color(0.78f, 0.8f, 0.82f), smoothness: 0.6f);
            var coil = GreyboxKit.Mat("VM_Coil", new Color(0.1f, 0.3f, 0.35f), new Color(0.3f, 0.85f, 1f) * 1.5f, 0.8f);
            var cellMat = GreyboxKit.Mat("VM_Cell", new Color(0.2f, 0.15f, 0.05f), new Color(1f, 0.6f, 0.15f) * 1.2f, 0.7f);

            var vm = new GameObject("ViewModel").transform;
            vm.SetParent(camera, false);
            vm.localPosition = new Vector3(0.2f, -0.23f, 0.42f);
            var visual = new GameObject("Visual").transform;
            visual.SetParent(vm, false);
            visual.localScale = Vector3.one * 0.78f;

            var glow = new System.Collections.Generic.List<Renderer>();
            // Rifle
            Part(visual, PrimitiveType.Cube, "Body", new Vector3(0f, 0f, 0.05f), new Vector3(0.065f, 0.085f, 0.5f), Vector3.zero, gunDark);
            Part(visual, PrimitiveType.Cube, "TopRail", new Vector3(0f, 0.052f, 0.02f), new Vector3(0.028f, 0.02f, 0.36f), Vector3.zero, gunWhite);
            Part(visual, PrimitiveType.Cylinder, "Shroud", new Vector3(0f, 0.005f, 0.36f), new Vector3(0.07f, 0.11f, 0.07f), new Vector3(90f, 0f, 0f), gunWhite);
            for (int i = 0; i < 3; i++)
                glow.Add(Part(visual, PrimitiveType.Cylinder, $"Coil{i}", new Vector3(0f, 0.005f, 0.28f + i * 0.05f), new Vector3(0.085f, 0.008f, 0.085f), new Vector3(90f, 0f, 0f), coil));
            var muzzleGo = Part(visual, PrimitiveType.Sphere, "Emitter", new Vector3(0f, 0.005f, 0.48f), Vector3.one * 0.045f, Vector3.zero, coil);
            glow.Add(muzzleGo);
            glow.Add(Part(visual, PrimitiveType.Cube, "Cell", new Vector3(0f, -0.075f, -0.02f), new Vector3(0.05f, 0.075f, 0.09f), Vector3.zero, cellMat));
            Part(visual, PrimitiveType.Cube, "Grip", new Vector3(0f, -0.08f, -0.15f), new Vector3(0.035f, 0.1f, 0.05f), new Vector3(15f, 0f, 0f), gunDark);
            Part(visual, PrimitiveType.Cube, "Stock", new Vector3(0f, -0.015f, -0.3f), new Vector3(0.05f, 0.07f, 0.16f), Vector3.zero, gunWhite);
            glow.Add(Part(visual, PrimitiveType.Cube, "HoloSight", new Vector3(0f, 0.085f, 0.0f), new Vector3(0.03f, 0.022f, 0.004f), Vector3.zero, coil));
            // Arms: the right hand on the grip, the left under the shroud; bulky white suit sleeves running back out of view.
            Part(visual, PrimitiveType.Sphere, "GloveR", new Vector3(0.005f, -0.085f, -0.15f), new Vector3(0.07f, 0.075f, 0.09f), Vector3.zero, glove);
            Part(visual, PrimitiveType.Capsule, "SleeveR", new Vector3(0.07f, -0.17f, -0.33f), new Vector3(0.11f, 0.2f, 0.11f), new Vector3(70f, -20f, 0f), suit);
            Part(visual, PrimitiveType.Sphere, "GloveL", new Vector3(-0.01f, -0.05f, 0.24f), new Vector3(0.06f, 0.055f, 0.08f), Vector3.zero, glove);

            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(muzzleGo.transform.parent, false);
            muzzle.localPosition = new Vector3(0f, 0.005f, 0.5f);

            // The beam itself, drawn in world space from the muzzle to whatever it hits.
            var beamGo = new GameObject("ArcBeam");
            beamGo.transform.SetParent(root.transform, false);
            var line = beamGo.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.sharedMaterial = beamMat;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.numCapVertices = 4;
            line.enabled = false;
            var impact = new GameObject("BeamImpactLight").AddComponent<Light>();
            impact.transform.SetParent(root.transform, false);
            impact.type = LightType.Point;
            impact.color = new Color(0.4f, 0.9f, 1f);
            impact.range = 5f;
            impact.shadows = LightShadows.None;
            impact.enabled = false;

            var hum = new GameObject("BeamHum").AddComponent<AudioSource>();
            hum.transform.SetParent(root.transform, false);
            hum.clip = CombatAudioGenerator.Load(CombatAudioGenerator.BeamHum);
            hum.loop = true;
            hum.playOnAwake = false;
            hum.spatialBlend = 0f;
            hum.volume = 0.55f;

            var rifle = root.AddComponent<BeamRifle>();
            GreyboxKit.Set(rifle, "input", input);
            GreyboxKit.Set(rifle, "inventory", inventory);
            GreyboxKit.Set(rifle, "aim", camera);
            GreyboxKit.Set(rifle, "muzzle", muzzle);
            GreyboxKit.Set(rifle, "beam", line);
            GreyboxKit.Set(rifle, "beamLight", impact);
            GreyboxKit.Set(rifle, "loop", hum);
            GreyboxKit.Set(rifle, "oneShots", oneShots);
            GreyboxKit.Set(rifle, "reloadClip", CombatAudioGenerator.Load(CombatAudioGenerator.Reload));

            var view = vm.gameObject.AddComponent<WeaponViewModel>();
            GreyboxKit.Set(view, "rifle", rifle);
            GreyboxKit.Set(view, "input", input);
            GreyboxKit.Set(view, "visual", visual.gameObject);
            GreyboxKit.Set(view, "glowParts", glow.ToArray());
            visual.gameObject.SetActive(false);
            return rifle;
        }

        /// <summary>
        /// The pistol and its first-person view model: a compact dark pistol with glowing sight dots in Sam's right
        /// glove, his left hand cupped under it; a brief tracer and muzzle flash on each shot.
        /// </summary>
        static Sidearm AddSidearm(GameObject root, InputReader input, PlayerInventory inventory, Transform camera, AudioSource audio)
        {
            var glove = GreyboxKit.Mat("VM_Glove", new Color(0.22f, 0.23f, 0.25f), smoothness: 0.3f);
            var sleeve = GreyboxKit.Mat("VM_Sleeve", new Color(0.3f, 0.33f, 0.36f), smoothness: 0.25f);
            var metal = GreyboxKit.Mat("VM_PistolMetal", new Color(0.07f, 0.075f, 0.08f), smoothness: 0.7f);
            var grip = GreyboxKit.Mat("VM_PistolGrip", new Color(0.13f, 0.12f, 0.11f), smoothness: 0.2f);
            var dots = GreyboxKit.Mat("VM_SightDots", new Color(0.2f, 0.5f, 0.2f), new Color(0.4f, 1f, 0.4f) * 2.5f);

            var vm = new GameObject("PistolViewModel").transform;
            vm.SetParent(camera, false);
            vm.localPosition = new Vector3(0.13f, -0.15f, 0.33f);
            var visual = new GameObject("Visual").transform;
            visual.SetParent(vm, false);

            Part(visual, PrimitiveType.Cube, "Slide", new Vector3(0f, 0.02f, 0.03f), new Vector3(0.032f, 0.036f, 0.19f), Vector3.zero, metal);
            Part(visual, PrimitiveType.Cube, "Frame", new Vector3(0f, -0.005f, 0.02f), new Vector3(0.03f, 0.02f, 0.16f), Vector3.zero, metal);
            Part(visual, PrimitiveType.Cylinder, "Barrel", new Vector3(0f, 0.02f, 0.13f), new Vector3(0.014f, 0.006f, 0.014f), new Vector3(90f, 0f, 0f), grip);
            Part(visual, PrimitiveType.Cube, "Grip", new Vector3(0f, -0.055f, -0.045f), new Vector3(0.03f, 0.1f, 0.045f), new Vector3(-15f, 0f, 0f), grip);
            Part(visual, PrimitiveType.Cube, "TriggerGuard", new Vector3(0f, -0.025f, 0.015f), new Vector3(0.008f, 0.025f, 0.04f), Vector3.zero, metal);
            Part(visual, PrimitiveType.Cube, "SightFront", new Vector3(0f, 0.041f, 0.11f), new Vector3(0.006f, 0.008f, 0.006f), Vector3.zero, dots);
            Part(visual, PrimitiveType.Cube, "SightRearL", new Vector3(-0.009f, 0.041f, -0.055f), new Vector3(0.005f, 0.008f, 0.006f), Vector3.zero, dots);
            Part(visual, PrimitiveType.Cube, "SightRearR", new Vector3(0.009f, 0.041f, -0.055f), new Vector3(0.005f, 0.008f, 0.006f), Vector3.zero, dots);
            // Right hand wrapped round the grip, left hand cupping it; sleeves running back out of view.
            Part(visual, PrimitiveType.Sphere, "GloveR", new Vector3(0.006f, -0.06f, -0.05f), new Vector3(0.055f, 0.075f, 0.065f), Vector3.zero, glove);
            Part(visual, PrimitiveType.Sphere, "GloveL", new Vector3(-0.022f, -0.075f, -0.03f), new Vector3(0.05f, 0.06f, 0.07f), new Vector3(0f, 0f, 20f), glove);
            Part(visual, PrimitiveType.Capsule, "SleeveR", new Vector3(0.05f, -0.13f, -0.2f), new Vector3(0.07f, 0.13f, 0.07f), new Vector3(65f, -15f, 0f), sleeve);
            Part(visual, PrimitiveType.Capsule, "SleeveL", new Vector3(-0.07f, -0.14f, -0.18f), new Vector3(0.07f, 0.13f, 0.07f), new Vector3(65f, 25f, 0f), sleeve);

            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(visual, false);
            muzzle.localPosition = new Vector3(0f, 0.02f, 0.14f);

            var flashGo = new GameObject("PistolFlash");
            flashGo.transform.SetParent(muzzle, false);
            var flash = flashGo.AddComponent<Light>();
            flash.type = LightType.Point;
            flash.range = 7f;
            flash.intensity = 3.5f;
            flash.color = new Color(1f, 0.8f, 0.5f);
            flash.shadows = LightShadows.None;
            flash.enabled = false;

            var tracerGo = new GameObject("PistolTracer");
            tracerGo.transform.SetParent(root.transform, false);
            var tracer = tracerGo.AddComponent<LineRenderer>();
            tracer.useWorldSpace = true;
            tracer.positionCount = 2;
            tracer.startWidth = 0.012f;
            tracer.endWidth = 0.004f;
            tracer.sharedMaterial = BeamMaterial("FX_Tracer", new Color(2.4f, 1.9f, 1.2f));
            tracer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tracer.enabled = false;

            var sidearm = root.AddComponent<Sidearm>();
            GreyboxKit.Set(sidearm, "input", input);
            GreyboxKit.Set(sidearm, "inventory", inventory);
            GreyboxKit.Set(sidearm, "aim", camera);
            GreyboxKit.Set(sidearm, "muzzle", muzzle);
            GreyboxKit.Set(sidearm, "tracer", tracer);
            GreyboxKit.Set(sidearm, "flash", flash);
            GreyboxKit.Set(sidearm, "audioSource", audio);
            GreyboxKit.Set(sidearm, "shotClip", CombatAudioGenerator.Load(CombatAudioGenerator.PistolShot));
            GreyboxKit.Set(sidearm, "reloadClip", CombatAudioGenerator.Load(CombatAudioGenerator.Reload));

            var view = vm.gameObject.AddComponent<PistolViewModel>();
            GreyboxKit.Set(view, "sidearm", sidearm);
            GreyboxKit.Set(view, "input", input);
            GreyboxKit.Set(view, "visual", visual.gameObject);
            visual.gameObject.SetActive(false);
            return sidearm;
        }

        static Renderer Part(Transform parent, PrimitiveType type, string name, Vector3 pos, Vector3 scale, Vector3 euler, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return r;
        }

        /// <summary>An unlit HDR-bright material for beams: bloom turns it into a glow.</summary>
        public static Material BeamMaterial(string name, Color hdr)
        {
            string path = $"{GreyboxKit.MaterialFolder}/{name}.mat";
            var m = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                UnityEditor.AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", hdr);
            UnityEditor.EditorUtility.SetDirty(m);
            return m;
        }

        static Text Corner(Transform parent, string name, Font font, Vector2 pos)
        {
            var t = new GameObject(name, typeof(RectTransform)).AddComponent<Text>();
            t.transform.SetParent(parent, false);
            t.font = font;
            t.fontSize = 16;
            t.alignment = TextAnchor.LowerRight;
            t.color = new Color(0.9f, 0.93f, 0.95f, 0.9f);
            t.raycastTarget = false;
            t.supportRichText = true;
            t.verticalOverflow = VerticalWrapMode.Overflow; // the health line can grow upward with a low-health warning
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1, 0);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(560, 24);
            return t;
        }
    }
}

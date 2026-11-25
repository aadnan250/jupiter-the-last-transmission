using Jupiter.Combat;
using Jupiter.Player;
using Jupiter.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Jupiter.EditorTools
{
    /// <summary>
    /// Adds survival and combat to the PlayerRig prefab: health, inventory, the bolt driver (unequipped by default;
    /// levels where Sam is armed enable it), and the health/ammo HUD with a damage flash.
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

            var combatHud = hud.gameObject.AddComponent<CombatHUD>();
            GreyboxKit.Set(combatHud, "health", health);
            GreyboxKit.Set(combatHud, "weapon", driver);
            GreyboxKit.Set(combatHud, "inventory", inventory);
            GreyboxKit.Set(combatHud, "healthText", healthText);
            GreyboxKit.Set(combatHud, "ammoText", ammoText);
            GreyboxKit.Set(combatHud, "damageFlash", flashImg);
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
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1, 0);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(420, 24);
            return t;
        }
    }
}

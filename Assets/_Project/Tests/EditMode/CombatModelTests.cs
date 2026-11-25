using Jupiter.Audio;
using Jupiter.Combat;
using NUnit.Framework;

namespace Jupiter.Tests
{
    /// <summary>Unit tests for player health, the inventory and the bolt driver's ammo/reload state machine.</summary>
    public class CombatModelTests
    {
        // ---------- health ----------

        [Test]
        public void Health_DamagesHealsAndClamps()
        {
            var h = new HealthModel(100f, 30f);
            h.Damage(35f);
            Assert.AreEqual(65f, h.Current);
            Assert.AreEqual(35f, h.Heal(40f), "heal is capped at max");
            Assert.AreEqual(100f, h.Current);
            Assert.AreEqual(0f, h.Heal(10f));
        }

        [Test]
        public void Health_LowAndDeathStates()
        {
            var h = new HealthModel(100f, 30f);
            bool died = false;
            h.Died += () => died = true;
            h.Damage(70f);
            Assert.IsTrue(h.IsLow);
            h.Damage(500f);
            Assert.IsTrue(h.IsDead);
            Assert.IsFalse(h.IsLow, "dead is not 'low'");
            Assert.IsTrue(died);
            Assert.AreEqual(0f, h.Heal(40f), "no healing the dead");
        }

        [Test]
        public void Health_NoRegenerationOverTime()
        {
            // There is no Tick: health only changes through Damage and Heal (design: no regeneration).
            var h = new HealthModel();
            h.Damage(50f);
            Assert.AreEqual(50f, h.Current);
        }

        // ---------- inventory ----------

        [Test]
        public void Inventory_StacksThenUsesNewSlots()
        {
            var inv = new InventoryModel();
            Assert.AreEqual(5, inv.Add("med_patch", 5));
            Assert.AreEqual(5, inv.Count("med_patch"));
            Assert.AreEqual("med_patch", inv.SlotItem(0));
            Assert.AreEqual(InventoryModel.MaxStack, inv.SlotCount(0));
            Assert.AreEqual(2, inv.SlotCount(1));
        }

        [Test]
        public void Inventory_RejectsWhatDoesNotFit()
        {
            var inv = new InventoryModel();
            inv.Add("flare", 3); inv.Add("decoy", 3); inv.Add("sealant", 3);
            Assert.AreEqual(3, inv.Add("med_patch", 99), "only one slot (3) left");
            Assert.AreEqual(0, inv.Add("med_patch", 1), "full");
        }

        [Test]
        public void Inventory_ConsumeFreesEmptySlots()
        {
            var inv = new InventoryModel();
            inv.Add("med_patch", 1);
            Assert.IsTrue(inv.Consume("med_patch"));
            Assert.IsNull(inv.SlotItem(0));
            Assert.IsFalse(inv.Consume("med_patch"));
        }

        [Test]
        public void Inventory_OneWeaponOneTool()
        {
            var inv = new InventoryModel();
            inv.EquipWeapon("bolt_driver");
            inv.EquipTool("flare_pistol");
            Assert.AreEqual("bolt_driver", inv.Weapon);
            Assert.AreEqual("flare_pistol", inv.Tool);
        }

        // ---------- bolt driver ----------

        [Test]
        public void Weapon_FiresSixThenIsEmpty()
        {
            var w = new WeaponModel(magazine: 6, reserve: 0, shotInterval: 0.5f);
            for (int i = 0; i < 6; i++)
            {
                Assert.IsTrue(w.TryFire(), $"shot {i + 1}");
                Assert.IsFalse(w.TryFire(), "cooling between shots");
                w.Tick(0.5f);
            }
            Assert.AreEqual(WeaponState.Empty, w.State);
            Assert.IsFalse(w.TryReload(), "no reserve");
        }

        [Test]
        public void Weapon_ReloadTakesTimeAndMovesReserve()
        {
            var w = new WeaponModel(magazine: 6, reserve: 10, reloadSeconds: 2.2f, shotInterval: 0f);
            for (int i = 0; i < 4; i++) w.TryFire();
            Assert.AreEqual(2, w.Loaded);
            Assert.IsTrue(w.TryReload());
            Assert.AreEqual(WeaponState.Reloading, w.State);
            Assert.IsFalse(w.TryFire(), "cannot fire while reloading");
            w.Tick(2.1f);
            Assert.AreEqual(2, w.Loaded);
            w.Tick(0.2f);
            Assert.AreEqual(6, w.Loaded);
            Assert.AreEqual(6, w.Reserve);
            Assert.AreEqual(WeaponState.Ready, w.State);
        }

        [Test]
        public void Weapon_ReloadNotNeededWhenFull()
        {
            var w = new WeaponModel();
            Assert.IsFalse(w.TryReload());
        }

        [Test]
        public void Weapon_AmmoPickupsCapAtMaxReserve()
        {
            var w = new WeaponModel(reserve: 28, maxReserve: 30);
            Assert.AreEqual(2, w.AddAmmo(6));
            Assert.AreEqual(30, w.Reserve);
        }

        // ---------- combat audio ----------

        [Test]
        public void CombatSounds_AreDeterministicAndInRange()
        {
            CollectionAssert.AreEqual(CombatSynth.BoltShot(), CombatSynth.BoltShot());
            foreach (var clip in new[] { CombatSynth.BoltShot(), CombatSynth.Reload() })
                foreach (float s in clip)
                    Assert.LessOrEqual(System.Math.Abs(s), 1f);
        }
    }
}

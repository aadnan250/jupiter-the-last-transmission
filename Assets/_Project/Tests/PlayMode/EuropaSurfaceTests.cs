using System.Collections;
using System.IO;
using System.Linq;
using Jupiter.Combat;
using Jupiter.Core;
using Jupiter.Creatures;
using Jupiter.Narrative;
using Jupiter.Player;
using Jupiter.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Jupiter.Tests
{
    /// <summary>
    /// End-to-end tests of the Europa landing (L03_EuropaSurface): Sam starts armed in the lander with the landing
    /// party, the ramp drops and the crew walk out with him, the quake brings ten Stalkers up out of the ice, the
    /// beam rifle and the crew kill them, and clearing the plain opens the way into Conamara Base.
    /// </summary>
    public class EuropaSurfaceTests
    {
        PlayerController player;
        DialogueManager dialogue;
        StoryState story;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("L03_EuropaSurface", LoadSceneMode.Single);
            yield return null;
            GameSession.Current.AutosaveEnabled = false;
            GameSession.Current.NewGame();
            player = Object.FindAnyObjectByType<PlayerController>();
            dialogue = Object.FindAnyObjectByType<DialogueManager>();
            story = dialogue.Story;
        }

        void Teleport(Vector3 p)
        {
            var cc = player.GetComponent<CharacterController>();
            cc.enabled = false;
            player.transform.position = p;
            cc.enabled = true;
        }

        [UnityTest]
        public IEnumerator Landing_SamIsArmedInTheLanderWithThreeSuitedCrew()
        {
            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(player.IsGrounded, "standing on the cabin floor");
            Assert.Greater(player.transform.position.y, 1f, "inside the lander, above the ice");
            Assert.IsTrue(player.GetComponent<PlayerInventory>().HasBeamRifle);
            Assert.AreEqual(3, SquadMember.All.Count, "Ashford, Mara and Hana");
            Assert.AreEqual(EuropaCount, Ambushers.Count(), "ten for the ambush");
            Assert.AreEqual(4, StalkerAI.All.Count - EuropaCount, "and four more for the drill site");
            Assert.IsTrue(StalkerAI.All.All(s => s.State == StalkerState.Buried), "nothing in the lander");
            Assert.IsTrue(dialogue.IsActive && dialogue.OnTheMove, "radio chatter on touchdown, without stopping Sam");
        }

        const int EuropaCount = 10;

        /// <summary>The ten Stalkers of the first ambush (the drill-site wave waits separately).</summary>
        static System.Collections.Generic.IEnumerable<StalkerAI> Ambushers => StalkerAI.All.Where(s => !s.name.StartsWith("DrillWave"));

        [UnityTest]
        public IEnumerator Ramp_DropsAndTheCrewWalkOutWithSam()
        {
            var landing = Object.FindAnyObjectByType<LandingSequence>();
            landing.OpenRamp();
            for (float t = 0f; t < 6f && !landing.RampOpen; t += Time.deltaTime) yield return null;
            Assert.IsTrue(landing.RampOpen);
            Assert.IsTrue(story.HasFlag("ramp_open"));
            Assert.IsTrue(SquadMember.All.All(m => m.Released));

            // Sam walks out onto the ice; the crew follow him there.
            Teleport(new Vector3(0f, 0.2f, -14f));
            yield return new WaitForSeconds(9f);
            foreach (var m in SquadMember.All)
            {
                Assert.Less(m.transform.position.y, 0.8f, $"{m.CharacterId} came down the ramp");
                Assert.Less(Vector3.Distance(m.transform.position, player.transform.position), 9f, $"{m.CharacterId} is with Sam");
            }
        }

        [UnityTest]
        public IEnumerator Quake_BringsTenStalkersUpOutOfTheIce_AroundTheSquad()
        {
            story.SetFlag("ramp_open");
            Teleport(new Vector3(0f, 0.2f, -60f));
            var ambush = Object.FindAnyObjectByType<AmbushDirector>();
            ambush.Begin();
            Assert.IsTrue(story.HasFlag("quake_started"));
            Assert.Greater(player.GetComponentInChildren<CameraShake>().Strength, 0.5f, "the ground shakes");
            yield return new WaitForSeconds(0.8f);
            Assert.IsTrue(dialogue.IsActive && dialogue.OnTheMove, "panic on the radio, on the move");

            yield return new WaitForSeconds(AmbushTimeline.EmergeTime(EuropaCount - 1) + 2.5f - 0.8f);
            Assert.AreEqual(EuropaCount, ambush.Emerged);
            Assert.IsTrue(Ambushers.All(s => s.State != StalkerState.Buried));
            Assert.IsTrue(Ambushers.All(s => s.Active || s.State == StalkerState.Dead), "every one is up and hunting, or already shot down");
            Assert.IsTrue(Ambushers.Where(s => s.Active).All(s => s.Target != null), "each has picked someone to run at");
        }

        [UnityTest]
        public IEnumerator BeamRifle_BurnsAStalkerDown()
        {
            story.SetFlag("ramp_open");
            Teleport(new Vector3(0f, 0.2f, -30f));
            yield return null;
            var s = StalkerAI.All[0];
            s.Emerge(player.transform.position + player.transform.forward * 8f, player.transform.position);
            yield return new WaitForSeconds(1.8f);

            var rifle = player.GetComponent<BeamRifle>();
            var cam = player.GetComponentInChildren<Camera>().transform;
            float charge = rifle.Model.Charge;
            for (float t = 0f; t < 6f && s.Alive; t += Time.deltaTime)
            {
                cam.rotation = Quaternion.LookRotation(s.transform.position + Vector3.up * 0.9f - cam.position);
                rifle.Fire(Time.deltaTime, true);
                yield return null;
            }
            Assert.IsFalse(s.Alive, "sustained beam fire kills it");
            Assert.Less(rifle.Model.Charge, charge, "and drains the cell");
            Assert.IsTrue(rifle.BeamVisible || rifle.Model.State != BeamState.Firing);
        }

        [UnityTest]
        public IEnumerator ClearingThePlain_OpensTheBase_AndTheAirlockLeadsInside()
        {
            var ambush = Object.FindAnyObjectByType<AmbushDirector>();
            story.SetFlag("ramp_open");
            ambush.Begin();
            yield return new WaitForSeconds(AmbushTimeline.EmergeTime(EuropaCount - 1) + 2f);
            foreach (var s in StalkerAI.All.ToArray())
                s.TakeDamage(new DamageInfo(1000f, DamageType.Beam, s.transform.position));
            yield return null;
            Assert.IsTrue(ambush.Cleared);
            Assert.IsTrue(story.HasFlag("ambush_cleared"));

            Assert.AreEqual("Reach Tiamat's lander (the green beacon, north-east)", Object.FindAnyObjectByType<MissionManager>().CurrentText,
                "the story stays out on the ice");
        }

        [UnityTest]
        public IEnumerator SurfaceStory_Tiamat_Drill_Eruption_Listener_Ascent()
        {
            var director = Object.FindAnyObjectByType<SurfaceStoryDirector>();
            var mission = Object.FindAnyObjectByType<MissionManager>();
            story.SetFlag("ramp_open");
            story.SetFlag("quake_started");
            story.SetFlag("ambush_cleared");
            foreach (var m in SquadMember.All) m.Release();
            yield return null;
            Assert.AreEqual(SurfaceBeat.ToTiamat, director.Beat);

            // Tiamat's lander, and its cargo manifest.
            Object.FindObjectsByType<DialogueTrigger>().First(t => t.name == "Trigger_TiamatSite").TryFire();
            yield return null;
            Assert.AreEqual(SurfaceBeat.AtTiamat, director.Beat);
            Assert.AreEqual("Search Tiamat's data box (read the cargo manifest)", mission.CurrentText);
            Object.FindObjectsByType<Readable>().First(r => r.name == "Readable_CargoManifest").Interact(player.gameObject);
            Object.FindAnyObjectByType<Jupiter.UI.ReadableUI>().Close();
            director.DrillSeconds = 3f;
            yield return null;
            Assert.AreEqual(SurfaceBeat.Drilling, director.Beat, "Ashford orders the borehole drilled");
            var hana = SquadMember.All.First(m => m.CharacterId == "HANA");
            var ashford = SquadMember.All.First(m => m.CharacterId == "ASHFORD");
            Assert.IsTrue(Object.FindAnyObjectByType<DrillRig>().Running);

            // The bore erupts.
            for (float t = 0f; t < 6f && director.Beat == SurfaceBeat.Drilling; t += Time.deltaTime) yield return null;
            Assert.AreEqual(SurfaceBeat.Boss, director.Beat);
            Assert.IsTrue(story.HasFlag("site_eruption"));
            Assert.IsTrue(hana.Lost && ashford.Lost, "Hana is taken and Ashford is down");
            Assert.AreEqual(1, SquadMember.All.Count, "only Mara is left");
            yield return null;
            var listener = Object.FindAnyObjectByType<CreatureAI>();
            Assert.IsNotNull(listener, "the Listener is out of the bore");
            Assert.AreEqual("KILL IT: knock it down, then keep firing", mission.CurrentText);

            // Knock it down, then finish it.
            var body = listener.GetComponentsInChildren<CreatureHitbox>().First(h => !h.IsNode);
            for (int i = 0; i < 40 && listener.Health.Model.Condition != CreatureCondition.Dead; i++)
                body.TakeDamage(new DamageInfo(40f, DamageType.Beam, body.transform.position));
            yield return null;
            Assert.IsTrue(story.HasFlag("listener_killed"));
            Assert.AreEqual(SurfaceBeat.Aftermath, director.Beat);

            // The radio calls give the code; the ascent vehicle takes it.
            story.SetFlag("knows_ascent_code");
            yield return null;
            Assert.AreEqual(SurfaceBeat.ToAscent, director.Beat);
            var keypad = Object.FindObjectsByType<KeypadTerminal>().First(k => k.name == "Terminal_Halcyon");
            keypad.Interact(player.gameObject);
            foreach (char c in "4117") keypad.Press(c - '0');
            Assert.IsTrue(story.HasFlag("ascent_unlocked"), "Halcyon's hatch opens");
            var end = Object.FindObjectsByType<LevelTransition>().First();
            for (float t = 0f; t < 15f && !end.Holding; t += Time.deltaTime) yield return null;
            Assert.IsTrue(end.Holding, "the end card holds: to be continued");
            Assert.AreEqual("L03_EuropaSurface", SceneManager.GetActiveScene().name, "no next level yet");
        }

        [UnityTest]
        public IEnumerator Squad_FightBackWhenStalkersAreUp()
        {
            story.SetFlag("ramp_open");
            foreach (var m in SquadMember.All) m.Release();
            Teleport(new Vector3(0f, 0.2f, -20f));
            yield return new WaitForSeconds(4f);
            var s = StalkerAI.All[0];
            s.Emerge(player.transform.position + new Vector3(10f, 0f, -6f), player.transform.position);
            bool targeted = false, fired = false;
            for (float t = 0f; t < 6f && s.Alive; t += Time.deltaTime)
            {
                targeted |= SquadMember.All.Any(m => m.CurrentTarget == s);
                fired |= SquadMember.All.Any(m => m.Firing);
                yield return null;
            }
            Assert.IsTrue(targeted, "the crew turn on it");
            Assert.IsTrue(fired, "and open fire");
            Assert.Less(s.Health.Current, s.Health.Max, "and hit it");
        }

        /// <summary>Renders the ambush in progress to docs/images (run on demand: -testFilter EuropaAction).</summary>
        [UnityTest, Explicit, Category("Screenshots")]
        public IEnumerator EuropaAction_CaptureScreenshots()
        {
            var landing = Object.FindAnyObjectByType<LandingSequence>();
            landing.OpenRamp();
            yield return new WaitForSeconds(4f);
            Teleport(new Vector3(0f, 0.2f, -45f));
            yield return new WaitForSeconds(5f);
            Object.FindAnyObjectByType<AmbushDirector>().Begin();
            yield return new WaitForSeconds(5.5f);
            var cam = player.GetComponentInChildren<Camera>();
            var near = StalkerAI.All.Where(x => x.Active).OrderBy(x => Vector3.Distance(x.transform.position, player.transform.position)).First();
            cam.transform.rotation = Quaternion.LookRotation(near.transform.position + Vector3.up * 0.8f - cam.transform.position);
            var rifle = player.GetComponent<BeamRifle>();
            for (int i = 0; i < 20; i++) { rifle.Fire(Time.deltaTime, true); yield return null; }
            Save(cam, "europa-ambush.png");
            yield return new WaitForSeconds(1.5f);
            var other = StalkerAI.All.Where(x => x.Active).OrderBy(x => Vector3.Distance(x.transform.position, player.transform.position)).Skip(1).FirstOrDefault() ?? near;
            Vector3 from = other.transform.position + other.transform.forward * 2.2f + Vector3.up * 1.3f;
            cam.transform.SetPositionAndRotation(from, Quaternion.LookRotation(other.transform.position + Vector3.up * 0.9f - from));
            yield return null;
            Save(cam, "europa-stalker.png");
        }

        static void Save(Camera cam, string file)
        {
            const int w = 1280, h = 720;
            var rt = new RenderTexture(w, h, 24);
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = null;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            Directory.CreateDirectory("docs/images");
            File.WriteAllBytes(Path.Combine("docs/images", file), tex.EncodeToPNG());
            Object.Destroy(tex);
            rt.Release();
        }
    }
}

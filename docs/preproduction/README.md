# JUPITER: THE LAST TRANSMISSION — Pre-Production Design Document

> *The mission was supposed to take humanity closer to the stars.*
> *It was never supposed to bring something back.*

**Status:** Draft v1, awaiting approval. No implementation begins until this document is approved.

## 1. Document map

| # | Required section | Where |
|---|---|---|
| 1 | Final game concept | [01-story-bible.md §1](01-story-bible.md#1-final-game-concept) |
| 2 | Detailed story synopsis | [01-story-bible.md §2](01-story-bible.md#2-story-synopsis) |
| 3 | Full narrative timeline | [01-story-bible.md §3](01-story-bible.md#3-narrative-timeline) |
| 4 | Main character biographies | [01-story-bible.md §4](01-story-bible.md#4-main-characters) |
| 5 | Supporting crew biographies | [01-story-bible.md §5](01-story-bible.md#5-supporting-crew-proposal) |
| 6 | Relationship development | [01-story-bible.md §6](01-story-bible.md#6-relationship-development-sam-and-mara) |
| 7 | Central mystery and explanation | [01-story-bible.md §7](01-story-bible.md#7-the-central-mystery-and-its-explanation) |
| 8 | Creature biology and designs | [01-story-bible.md §8](01-story-bible.md#8-creature-biology-and-designs) |
| 9 | Chapter-by-chapter breakdown | [02-chapters-and-scenes.md §1](02-chapters-and-scenes.md#1-chapter-overview) |
| 10 | Scene-by-scene breakdown | [02-chapters-and-scenes.md §2–3](02-chapters-and-scenes.md#2-scene-by-scene-breakdown) |
| 11 | Full opening sequence screenplay | [03-screenplay.md §1](03-screenplay.md#1-full-opening-sequence--chapter-1) |
| 12 | Major dialogue examples | [03-screenplay.md §2](03-screenplay.md#2-major-dialogue-examples) |
| 13 | The companion's death sequence | [03-screenplay.md §3](03-screenplay.md#3-the-companions-death) |
| 14 | Final escape and Earth return | [03-screenplay.md §4](03-screenplay.md#4-final-escape-and-return-to-earth) |
| 15 | Gameplay mechanics | [04-gameplay-and-levels.md §1–8](04-gameplay-and-levels.md) |
| 16 | Level design | [04-gameplay-and-levels.md §9](04-gameplay-and-levels.md#9-level-design) |
| 17 | Sound design | [05-audio-and-visual.md Part A](05-audio-and-visual.md#part-a--sound-design) |
| 18 | Visual direction | [05-audio-and-visual.md Part B](05-audio-and-visual.md#part-b--visual-direction) |
| 19 | Unity architecture | [06-technical-plan.md §1–4](06-technical-plan.md) |
| 20 | Asset requirements | [06-technical-plan.md §5](06-technical-plan.md#5-asset-requirements) |
| 21 | Development schedule | [06-technical-plan.md §7](06-technical-plan.md#7-development-schedule) |
| 22 | Hardware optimisation plan | [06-technical-plan.md §6](06-technical-plan.md#6-hardware-optimisation-plan-intel-iris-xe-16-gb) |
| 23 | GitHub release strategy | [06-technical-plan.md §8](06-technical-plan.md#8-github-release-strategy) |

## 2. Established facts vs. creative proposals

**Established by the concept (unchanged):** title; genre; Unity/C#/PC; first-person with cinematic third-person moments; ~60 minutes; year 2183; Earth → Jupiter → Europa; Europa as the walkable environment; ten chapters with their stated purposes; two primary characters; the companion initially dislikes the protagonist, saves them, the creature rises again and kills the companion; the protagonist continues alone, attempts to contact Earth, escapes, and returns home; original creatures (tall predator, fast confined-space form, large form, environmental organism as directions); limited choices; modular architecture; the system list; the GitHub requirements; the hardware constraints.

**Proposed in this document (awaiting approval):** all names (characters, ship, base, mission); the *Tiamat* backstory and Feature 7; the sample-return secret; the reason for Mara's dislike (Tomas); the "That's one… That's five" tally; the creature designs and the "one organism" explanation; the vacuum sound pillar; specific choices and the two ending variants; all dialogue; the 11-month transit; level list; weapons.

## 3. Contradictions and missing information (with proposed solutions)

| # | Issue | Why it matters | Proposed solution |
|---|---|---|---|
| 1 | **Europa has no atmosphere**, but the concept lists "wind-like atmospheric effects" and creature screams on the surface. | Sound can't travel in a vacuum. Using it would break the scientific grounding the concept asks for. | Make it a pillar: outdoors you hear **only through your suit, boots and radio**. Creature sounds return indoors. Wind is removed from the surface. |
| 2 | **Light delay**: Earth is 33–53 light-minutes away, but the concept implies contact with Earth during emergencies. | Real-time conversation with Earth is impossible at Jupiter. | All Earth communication is **delayed recorded packets**. This becomes an emotional device (Grace's message arriving after the crew are dead). |
| 3 | **Travel time**: real missions take 5–6 years to reach Jupiter. | A multi-year transit with an awake crew strains believability. | A **[FICTION]** fusion drive allows an 11-month transit. Clearly labelled as fictional technology. |
| 4 | **Radiation**: Europa's surface receives extreme radiation; unshielded humans would be in serious danger within a day. | Long surface operations would be unrealistic. | Short EVAs, a shielded habitat (buried under ice), and the suit dosimeter as a soft gameplay timer. |
| 5 | **Gravity**: Chapter 2 needs natural movement aboard the ship, which zero-g makes very expensive to build. | Zero-g locomotion would multiply scope. | A **spin-gravity habitat ring** (0.6 g). Europa uses its real 0.134 g. |
| 6 | **Hearing the first sound in space** (Ch. 3). | Sound doesn't cross space. | It's **sonified seismometer data** from surface sensors left by *Tiamat*, played through ship speakers, which is how scientists really "listen" to seismic data. |
| 7 | **"Did you hear that?" / "Tell me that was the engines."** are quoted as examples. | The concept says not to use them verbatim. | Original lines are written in the screenplay; these examples are not used. |
| 8 | **Planet visibility** during the journey must match the orbital timeline, but no departure date is fixed. | Accuracy of what's visible (e.g., Mars) depends on dates. | Proposed launch Feb 2182; verify visible planets with NASA JPL Horizons once dates are locked. Mars sighting stays optional until checked. |
| 9 | **Who dies when?** The concept asks for crew deaths over the radio but doesn't specify which crew exist. | Needed for scripting and voice planning. | A five-person crew: Luca dies over the radio; Hana is taken; Ashford dies confessing; Mara dies in Ch. 8. |
| 10 | **"Gunfire should not immediately eliminate every threat"** vs. a scientific crew carrying weapons. | Why would scientists be armed? | Weapons are **repurposed tools** (pneumatic bolt driver, flare pistol, seismic charges), with no military rifles. |
| 11 | **Why the creature gets back up** (Ch. 8). | It needs a rule, or it feels cheap. | The **torpor/reseal biology**: they "killed" it the wrong way. Hana's notes teach the correct way afterwards, so the death is a tragic lesson rather than a trick. |
| 12 | **Ending mystery without a sequel tease.** | Fine balance. | The player's own choice (vent or keep the sample) decides the final image. Both endings feel complete; one leaves a quiet unease. |
| 13 | **Voice acting** for ~650 lines with no budget. | The largest production risk. | Volunteer actors with release forms, or a commercially licensed synthetic-voice service with original voices. Temp VO by the developer during development. |
| 14 | **Original creature models and animations** require 3D skills. | Second-largest risk. | Placeholder silhouettes first; the Listener is the only creature that needs full-quality art. The Keel is shown only as a back and a ridge; Needles are small and fast (cheap). |
| 15 | **Scope**: ~60 min of polished, voiced, cinematic horror for one developer. | Risk of never finishing. | Vertical slice first (Ch. 6–7), heavy reuse of the *Meridian* scene across four chapters, limited choices, three third-person shots in total. |
| 16 | **Storage** was listed as limited. | Unity projects grow large. | The machine has ~299 GB free, which is enough. Keep the `Library/` folder out of git and builds out of the repo. |
| 17 | **Asset Store assets in a public repo.** | The Asset Store EULA forbids redistributing them publicly. | Only redistributable assets are committed; any Asset Store assets go in a git-ignored folder (see technical plan §5.1). |

## 4. Decisions needed from you

1. **Approve or change the names**: Sam Okafor, Mara Lindqvist, Daniel Ashford, Hana Mori, Luca Ferreira, ship *Meridian*, base *Conamara*, mission *Halcyon*.
2. **Approve the central mystery** (one distributed organism; the sample-return secret).
3. **Approve the vacuum sound pillar**, which removes wind and surface screams.
4. **Protagonist**: keep Sam as written, or a different protagonist (name, gender, background)?
5. **Violence level**: the plan has one explicit moment (Mara's death) and controlled gore elsewhere. More or less?
6. **Voice plan**: real actors, licensed synthetic voices, or a mix?

Once approved, implementation starts with **Phase 1 — Project Foundation**: URP project, folder structure, input actions, first-person controller and a grey-box test room.

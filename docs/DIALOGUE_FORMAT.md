# Dialogue Script Format

Conversations are plain text files in `Assets/_Project/Narrative/Dialogue/`. Writers can edit them in any text editor;
`DialogueParser` turns them into a node graph and `DialogueRunner` plays them against the player's `StoryState`.
Errors report the file and line number, and an EditMode test parses every shipped file on every test run.

## Syntax

```text
# A comment

=== start                               Node header. The first node is the entry point.
MARA: Okafor.                           A line spoken in person.
LUCA (radio): Copy that.                A line heard over the radio (squelch + [RADIO] tag).
[if flag met_mara] MARA: You again?     Conditional line.
~ trust MARA +1                         Effect: change trust (clamped to -5..+5).
~ flag noticed_player                   Effect: set a story flag.
~ unflag noticed_player                 Effect: clear a story flag.
[if !flag told_truth] -> other_node     Conditional jump.
-> END                                  End the conversation.

* "Reply text" -> target                A reply (max 4 per node). Spoken aloud by Sam when picked.
  ~ trust MARA +1                       Indented effects belong to the reply above.
*! "Big decision" -> target             A significant reply: plays the consequence sting.
* "(Say nothing.)" -> target            Replies in parentheses are actions and are not spoken.
* [if trust MARA >= 3] "Old friend." -> target    A reply that only appears when earned.
```

### Conditions

| Condition | Meaning |
|---|---|
| `[if flag x]` | flag `x` is set |
| `[if !flag x]` | flag `x` is not set |
| `[if trust NAME >= n]` | trust comparison: `>=`, `<=`, `>`, `<`, `==` |

Several conditions can be chained: `[if flag a] [if trust MARA > 1] MARA: ...`

### Rules enforced by the parser and tests

- At most **4 replies** per node (one per controller face button).
- Every jump target must exist (or be `END`).
- Replies must come last in a node; only indented effects may follow them.
- **Every reply must have at least one effect.** Every decision changes the story (`ContentValidationTests`).
- Every non-radio conversation must contain at least one significant (`*!`) reply.
- Every speaker must be a known character (`Characters.cs`).

## Controls during dialogue

| Action | Keyboard / Mouse | PlayStation | Xbox |
|---|---|---|---|
| Continue | E / Space / Left click | X (Cross) | A |
| Reply 1 | 1 | ▲ Triangle | Y |
| Reply 2 | 2 | ● Circle | B |
| Reply 3 | 3 | X Cross | A |
| Reply 4 | 4 | ■ Square | X |

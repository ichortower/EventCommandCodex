# Event Command Codex - Player Control

This is the detailed documentation for the PlayerControl command, and in
particular its data model, which governs how the sequences will play out.

## Contents

* [Commands](#commands)
  * [PlayerControl](#playercontrol)
  * [PlayerControlUpdate](#playercontrolupdate)
  * [PlayerControlSetNextStage](#playercontrolsetnextstage)
  * [PlayerControlEnd](#playercontrolend)
* [Data Model](#data-model)
  * [Stages](#stages)
  * [Advance Conditions](#advance-conditions)
  * [Scripts](#scripts)



# Commands

Except for the command to start a sequence, these commands will cause an error
if a player control sequence is not currently running.


### `PlayerControl`

`ichortower.ECC_PlayerControl <id>`

This command takes one argument: the id of a player control sequence to run.
The id must match a key in the `ichortower.ECC/PlayerControl/Data` data asset,
and the value for that key will govern how the sequence plays out.

After loading the sequence, this command will proceed to an injected
`PlayerControlUpdate` command, which will block until the sequence finishes
and proceed afterward; so the first command in your event script following
this one will not execute until the player control sequence ends.


### `PlayerControlUpdate`

`ichortower.ECC_PlayerControlUpdate`

Not for client use. This command is automatically injected by `PlayerControl`
in order to block and handle updates while the sequence runs. It is documented
here in case it crops up in error messages.


### `PlayerControlSetNextStage`

`ichortower.ECC_PlayerControlSetNextStage <name> [immediate]`

Normally, when executing a player control sequence, each stage is handled in
order from the first to the last (see the [Data Model](#data-model) for more
information). This command lets you change the ordering by specifying which
stage should run *next*, i.e. when the current stage ends; it is intended for
use in Scripts, which are run at various points during the sequence.

The `name` argument must match the `Id` field of one of the stages specified
in the current player control sequence; that is the stage that will follow
the current one. If the id isn't found, you will see a warning in your console
and the sequence will end instead.

The optional argument should be the string `immediate`, and it tells the
sequence handler to move to the named stage *immediately* instead of waiting
for the current one to be completed.


### `PlayerControlEnd`

`ichortower.ECC_PlayerControlEnd`

This command immediately ends the current player control sequence. Similar to
`PlayerControlSetNextStage` if the target stage is not found and the
`immediate` argument is provided.



# Data Model

To define your sequences, your mod will need to edit the following data asset
using Content Patcher:

`ichortower.ECC/PlayerControl/Data`

The asset is a `string->object` dictionary. The string keys are IDs, and should
be [unique string IDs](https://stardewvalleywiki.com/Modding:Common_data_field_types#Unique_string_ID),
like most 1.6-era data items. The model (object) has the following fields:

<table>

<tr>
<th>Field</th>
<th>Type</th>
<th>Purpose</th>
</tr>

<tr>
<td><code>Stages</code></td>
<td>array(object)</td>
<td>

This array contains the definition of each *Stage* in your player control
sequence. By default, these stages will be executed in the order you list them,
but you can jump around with the `PlayerControlSetNextStage` command.

The format for the stage object is laid out under [Stages](#stages), below.

*Default*: `[]`

</td>
</tr>

<tr>
<td><code>Scripts</code></td>
<td>string&rarr;array(string) dict</td>
<td>

This dictionary defines [Scripts](#scripts) which are common to the entire
sequence and can be referenced by any script anywhere deeper in the sequence
data (but not by other sequences). See the Scripts section for details about
how referencing works.

The keys of this object are script names: they must be unique within the
object, but there is no need to make them globally unique. The values are
arrays of strings.

These scripts will never be run unless referenced by another script field.

*Default*: `{}`

</td>
</tr>

</table>


## Stages

Each stage object represents one discrete phase of your sequence. Haley's beach
bracelet scene, for example, can be considered to have two stages:

1. the player must find the bracelet and interact with its tile
2. the player must talk to Haley

Generally, you use stages to define the order that things should occur: the
first stage must be completed before the second one can begin, and so on down
the line.

Each stage has *advance conditions*, which define what the player has to do to
proceed, and several types of *scripts*, which define what occurs and when
within the stage.

Here's the data model.

<table>
<tr>
<th>Field</th>
<th>Type</th>
<th>Purpose</th>
</tr>

<tr>
<td><code>Id</code></td>
<td>string</td>
<td>

This field is **required**, and must be unique among stages in this sequence
(but it does not have to be globally unique). Its job is to serve as a target
for the `PlayerControlSetNextStage` command, as well as identify this stage
internally to the mod.

</td>
</tr>

<tr>
<td><code>ScriptOnBegin</code></td>
<td>array(string)</td>
<td>

This [script](#scripts) will be run when this stage begins. Although the player
will have control while it executes, no conditions for this stage will be
checked until it has finished.

*Default*: `[]`

</td>
</tr>

<tr>
<td><code>ScriptOnAdvance</code></td>
<td>array(string)</td>
<td>

This [script](#scripts) will be run when this stage completes, *before*
allowing flow to proceed to the next stage.

*Default*: `[]`

</td>
</tr>

<tr>
<td><code>Scripts</code></td>
<td>string&rarr;array(string) dict</td>
<td>

Just like the `Scripts` object at the sequence level, this one contains
[Scripts](#scripts) which can be referenced from any other script field within
this stage. The scripts will not be run unless referenced; see the Scripts
section for more on referencing.

*Default*: `{}`

</td>
</tr>

<tr>
<td><code>Dialogues</code></td>
<td>string&rarr;string dict</td>
<td>

This dictionary sets dialogue strings which will be assigned to actors during
this stage. The keys are actor internal names, and the values are either
translation keys to load from (format: `Asset\\Name:key`) or literal dialogue
strings.

These dialogues will be repeated endlessly as long as the player continues to
interact with the NPC (the idea being that they may be giving hints, and the
text should not be missable). However, you can safely break up the dialogue
with `#$e#`, and the dialogue will not be replenished until it's all exhausted;
this lets you (for example) cycle multiple dialogue options.

*Default*: `{}`

</td>
</tr>

<tr>
<td><code>AdvanceConditions</code></td>
<td>array(object)</td>
<td>

This array is **required** and contains the definitions of the conditions under
which the stage is satisfied and flow can proceed to the next stage.

The format for the advance condition object is laid out under
[Advance Conditions](#advance-conditions), below.

</td>
</tr>

<tr>
<td><code>SatisfiedCount</code></td>
<td>integer</td>
<td>

This integer gives the number of advance conditions which must be met in order
to satisfy (complete) the stage. By default (`-1`), all conditions must be met.

**Note**: this count only covers conditions which are not explicitly required,
and is meant to implement situations where you want the player to complete
any `n` of `x` conditions. So, for example, you might have one required
condition and three non-required ones, along with a SatisfiedCount of `1`; in
this case, the player must meet the required condition and any one of the
remaining three optional ones.

See the Advance Conditions section for more details.

*Default*: `-1`

</td>
</tr>

</table>


## Advance Conditions

Advance Conditions are the heart of the player control sequence: these define
what it is the player is expected to do with their control. Each stage has a
list of them, and when enough of them are satisfied, the stage ends and the
sequence proceeds to the next stage.

The data model for each condition is as follows:

<table>
<tr>
<th>Field</th>
<th>Type</th>
<th>Purpose</th>
</tr>

<tr>
<td><code>Type</code></td>
<td>enum</td>
<td>

This field is **required**, and must be one of the strings in the list below.
The value of this field dictates which of the other fields are required/parsed
and determines the behavior of the condition.

- `TileTouch`: the player must move over specific tiles.
- `TileInteract`: the player must interact (action button) with specific tiles.
- `NpcInteract`: the player must interact with specific NPCs.
- `TimePass`: an amount of time must pass.

</td>
</tr>

<tr>
<td><code>Required</code></td>
<td>boolean</td>
<td>

This field declares whether this condition is definitely required to complete
the stage, or merely soft-required (i.e. required by default, but may be
optional if the stage is using a `SatisfiedCount`).

Remember that any conditions listed as Required are exempt from the stage's
SatisfiedCount field and must be met to proceed.

*Default*: `false`

</td>
</tr>

<tr>
<td><code>Tiles</code></td>
<td>array(object)</td>
<td>

This field is **required** for the `TileTouch` and `TileInteract` condition
types, and lists the tiles which the player is expected to touch or interact
with. The objects' data format is given below, under [Tiles](#tiles).

This field is ignored when the condition type is `NpcInteract` or `TimePass`.

*Default*: `[]`

</td>
</tr>

<tr>
<td><code>Actors</code></td>
<td>array(object)</td>
<td>

This field is **required** for the `NpcInteract` condition type. It lists the
NPCs with whom the player must interact to satisfy the condition. The objects'
data format is given below, under [Actors](#actors).

This field is ignored by any other condition type.

*Default*: `[]`

</td>
</tr>

<tr>
<td><code>Duration</code></td>
<td>integer</td>
<td>

This field is **required** for the `TimePass` condition type. It defines (in
milliseconds) how much time must pass before the condition is satisfied. For
example, `30000` would satisfy the condition after 30 seconds.

This field is ignored by any other condition type.

*Default*: `-1`

</td>
</tr>

<tr>
<td><code>SatisfiedCount</code></td>
<td>integer</td>
<td>

This field works just like the identical `SatisfiedCount` on the stage object:
it sets the number of non-Required Tiles or Actors that must be touched or
interacted with in order to satisfy the condition. For example, if your list
of Tiles is 7 items long, 2 of them are Required, and your SatisfiedCount is 2,
then the player must hit the two required tiles and any two of the remaining
five tiles in order to proceed.

This field is ignored when the condition type is `TimePass`.

*Default*: `-1`

</td>
</tr>

<tr>
<td><code>ScriptOnSatisfy</code></td>
<td>array(string)</td>
<td>

This field is a [Script](#scripts) and will be executed when the condition
is satisfied in full. Any script for the individual Tile or Actor that
completed the requirements will be allowed to run first, after which this one
will run (and this one will, in turn, prevent the stage's `ScriptOnAdvance`
from running until it is completed).

This is the generally-recommended place to put `PlayerControlSetNextStage`
commands, if you wish to use them.

*Default*: `[]`

</td>
</tr>

</table>

Conditions can be combined in various ways to achieve different goals. For
example, you might have one `TileTouch` condition and one `TimePass` condition
in a stage with a `SatisfiedCount` of 1. This effectively implements a time
limit on the tile-touching condition: if the TimePass condition is satisfied
first, the stage will end even if the required tiles were not touched. You
might then combine this with `PlayerControlSetNextStage` in the
`ScriptOnSatisfy` scripts to change the sequence's behavior (by proceeding to
different stages) based on whether the player was fast enough.


### Tiles

The `TileTouch` and `TileInteract` condition types both use this data model
for their arrays of tiles. The objects look like this:

<table>
<tr><th>Field</th><th>Type</th><th>Purpose</th></tr>

<tr><td><code>X</code></td>
<td>integer</td>
<td>

(**required**) The X coordinate of the target tile.

</td></tr>

<tr><td><code>Y</code></td>
<td>integer</td>
<td>

(**required**) The Y coordinate of the target tile.

</td></tr>

<tr><td><code>Required</code></td>
<td>boolean</td>
<td>

Just like the condition's `Required` field, this declares whether this tile
is definitely required or merely soft-required to satisfy the condition.

*Default*: `false`

</td></tr>

<tr><td><code>HintCursor</code></td>
<td>enum</td>
<td>

This string tells the sequence handler which mouse cursor to display when the
player hovers the mouse over the tile. It must be one of the following strings
and indicates the appropriate cursor (descriptions below are vanilla's icons):

- `None`: do nothing (allow vanilla to handle it).
- `Default`: the default arrow cursor.
- `Wait`: the "no input allowed, please wait" cursor (hourglass).
- `Grab`: the open-hand cursor.
- `Gift`: the cursor with the gift parcel icon on it.
- `Talk`: the cursor with the speech bubble.
- `Look`: the inspect cursor (magnifying glass).
- `Harvest`: the cursor with the green `+`.
- `Heart`: (unused in vanilla?) a cursor with a heart on it.
- `GamepadPointer`: hand cursor with pointing index finger (used in gamepad mode).

The names are derived from the "official" cursor names found in the game's
decompiled source code. They correspond, in the order listed above, to the
icons found at the top-left of the `LooseSprites/Cursors` tilesheet (with the
exception of `GamepadPointer`, which is on the second row).

*Default*: `None`

</td></tr>

<tr><td><code>Script</code></td>
<td>array(string)</td>
<td>

A [Script](#scripts) to run when the player touches or interacts with the tile.

In the event that interacting with this tile satisfies the condition, this
script is guaranteed to finish executing before the condition's
`ScriptOnSatisfy` is allowed to run.

*Default*: `[]`

</td>
</tr>
</table>


### Actors

The `NpcInteract` condition type uses this data model for its array of actors.
The objects look like this:

<table>
<tr><th>Field</th><th>Type</th><th>Purpose</th></tr>

<tr><td><code>Name</code></td>
<td>string</td>
<td>

(**required**) The internal name of the NPC to interact with.

</td></tr>

<tr><td><code>Required</code></td>
<td>boolean</td>
<td>

This works just like the `Required` field on [Tiles](#tiles) and
[Advance Conditions](#advance-conditions). If true, this NPC is definitely
required to satisfy the condition; if false, they may or may not be, depending
on the condition's `SatisfiedCount`.

*Default*: `false`

</td></tr>

<tr><td><code>HintCursor</code></td>
<td>enum</td>
<td>

This is identical to the `HintCursor` field on [Tiles](#tiles), above: it sets
the mouse cursor to display when mousing over the NPC in question.

*Default*: `None`

</td></tr>

<tr><td><code>Script</code></td>
<td>array(string)</td>
<td>

This is identical to the `Script` field on [Tiles](#tiles), above: this script
will be executed when the player interacts with this NPC.

As with Tiles, this script is guaranteed to finish executing before the
condition's `ScriptOnSatisfy` is allowed to run.

*Default*: `[]`

</td></tr>

</table>


## Scripts

Script values can be found in these places in the player control data model:

1. In the sequence-level `Scripts` object, as values (the keys are script names).
2. At the stage-level `Scripts` object, identical to #1.
3. At the stage level, as `ScriptOnBegin` and `ScriptOnAdvance`.
4. At the condition level, as `ScriptOnSatisfy`.
5. At the condition level, as the per-tile `Script`.

No matter where they are found, their format is the same: an array of strings.
The strings can be regular event commands, or, if the first string starts with
the `@` character, the script is considered a *reference* and the sequence
handler will look through the sequence data for a script bearing that name
(see Following References, below).

Scripts are run during a player control sequence using
[Streams](author-guide.md#stream-control), which in general means that the
player will still be able to move around while they are running (notable
exception: the dialogue box opened by commands like `speak` and `message` will
block player movement). However, there are some ordering guarantees in place
for scripts:

- a stage's ScriptOnBegin must finish before any of the stage's conditions
  will begin checking their requirements
- any ongoing per-tile Scripts must finish before a condition's ScriptOnSatisfy
  will begin
- any ongoing condition's ScriptOnSatisfy must finish before a stage's
  ScriptOnAdvance will begin
- a stage's ScriptOnAdvance must finish before the sequence may proceed to
  the next stage

As usual for streams, be aware of commands that cause trouble or may collide.
And take another special note of the `speak` command, which has a hardcoded
delay of 500 milliseconds before the dialogue box opens and may cause your
sequence to feel laggy as a result (you can use ECC's replacement,
`ichortower.ECC_Speak`, to avoid this problem).

### Following References

When loading a script, if the first string in the array starts with `@`, the
rest of that string is treated as the name of a script to find (any subsequent
strings in the array are discarded). So, for instance, this script value:

`"Script": ["@SparkleTile"],`

... means "find the script called `SparkleTile` and use its contents here".

To find a script, the sequence handler checks first in the `Scripts` object
provided for the current stage, and then in the sequence-level `Scripts` object.
A found script may itself be a reference, in which case the search begins
again, except for two things:

1. The handler will detect loops and throw an error, and
2. Once the search has reached the sequence-level `Scripts`, it cannot return
   to the stage-level `Scripts`. Put another way, the search can get broader
   but can't get narrower.

### Special Variables

When a script is run by the sequence handler, several values are automatically
provided and can be referenced in the scripts by using special tokens. All of
these are case-insensitive.

- In a [Tile](#tiles) or [Actor](#actors) script, the tokens `${x}` and `${y}`
  will be replaced with the X and Y coordinates, respectively, of the tile or
  NPC that was just interacted with or talked to.
- In a Tile or Actor script, the token `${count}` will be replaced with the
  total number of satisfied tiles or actors in that condition so far; so the
  first interaction will use the value `1`, then `2`, and so on.
- In an Actor script, the token `${npc}` will be replaced with the internal
  name of the NPC.

These are useful for performing actions that depend on coordinates or NPC
names while also reusing the same script in multiple places (for example, if
you want to run the same script to e.g. change a map tile or display a temporary
sprite at each tile in a condition, you can put the script at the stage level
with these tokens in it and reference it `@ByName`, and the coordinates will
automatically be filled in for you. See
[the example PlayerControl event](../sample-events/PlayerControlData.json) for
a demonstration of this).

The substitution of these tokens takes place *before* the strings are handed
off to the game.

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
  * [AdvanceConditions](#advanceconditions)
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

*Default*: []

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

*Default*: {}

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

*Default*: null

</td>
</tr>

<tr>
<td><code>ScriptOnBegin</code></td>
<td>array(string)</td>
<td>

This script will be run when this stage begins. Although the player will have
control while it executes, no conditions for this stage will be checked until
it has finished.

*Default*: null

</td>
</tr>

<tr>
<td><code>ScriptOnAdvance</code></td>
<td>array(string)</td>
<td>

This script will be run when this stage completes, *before* allowing flow to
proceed to the next stage.

*Default*: null

</td>
</tr>

<tr>
<td><code>Scripts</code></td>
<td>string&rarr;array(string) dict</td>
<td>

Just like the `Scripts` object at the sequence level, this one contains scripts
which can be referenced from any other script field within this stage. The
scripts will not be run unless referenced; see the Scripts section for more
on referencing.

*Default*: {}

</td>
</tr>

<tr>
<td><code>Dialogues</code></td>
<td>string&rarr;string dict</td>
<td>

This dictionary sets dialogue strings which will be assigned to actors during
this stage. The keys are actor internal names, and the values are either
translation keys to load from (i.e. `Asset\\Name:key`) or literal dialogue
strings.

These dialogues will be repeated endlessly as long as the player continues to
interact with the NPC (the idea being that they may be giving hints, and the
text should not be missable). However, you can safely break up the dialogue
with `#$e#`, and the dialogue will not be replenished until it's all exhausted;
this lets you (for example) cycle multiple dialogue options.

*Default*: {}

</td>
</tr>

</table>

## AdvanceConditions

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
exception: the dialogue box opened by the `speak` command will block player
movement). However, there are some ordering guarantees in place for certain
script interactions.

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

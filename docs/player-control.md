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

</td>
</tr>

<tr>
<td><code>Scripts</code></td>
<td>string->array(string) dict</td>
<td>

This dictionary defines [Scripts](#scripts) which are common to the entire
sequence and can be referenced by any script anywhere deeper in the sequence
data (but not by other sequences). See the Scripts section for details about
how referencing works.

The keys of this object are script names: they must be unique within the
object, but there is no need to make them globally unique. The values are
arrays of strings.

These scripts will never be run unless referenced by another script field.

</td>
</tr>


## Stages

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
through the stage-level and sequence-level `Scripts`
dictionaries to find a script whose name matches the rest of the string, and
use that script instead (any subsequent strings in the array are discarded).
This script may itself be a reference, and the chain may be as long as
desired, but loops will cause an error, and references at the sequence level
will not find scripts at the stage level.

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

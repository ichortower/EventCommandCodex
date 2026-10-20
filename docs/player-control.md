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

## Stages

## Scripts

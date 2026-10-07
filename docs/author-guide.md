# Event Command Codex - Author Guide

This document explains how to use the event commands added by this mod.


## Contents

* [General Notes](#general-notes)
* [Stream Control](#stream-control)
  * [StreamStart](#streamstart)
  * [StreamEnd](#streamend)
  * [StreamAwait](#streamawait)
  * [StreamHalt](#streamhalt)
  * [StreamSuspend](#streamsuspend)
  * [StreamResume](#streamresume)
  * [StreamRestart](#streamrestart)
  * [StreamLoop](#streamloop)
* [Stream-Safe Command Replacements](#stream-safe-command-replacements)
  * [Emote](#emote)
  * [FaceDirection](#facedirection)
  * [Message](#message)
  * [Pause](#pause)
  * [Speak](#speak)
* [Simultaneous Commands](#simultaneous-commands)
  * [Merge](#merge)
  * [MergeEnd](#mergeend)
* [Actor Control](#actor-control)
  * [ActorPathfind](#actorpathfind)
  * [ActorAwaitMovement](#actorawaitmovement)
  * [ActorAwaitAnimation](#actorawaitanimation)
  * [ActorHalt](#actorhalt)
* [Viewport Control](#viewport-control)
  * [ViewportMove](#viewportmove)
  * [ViewportShake](#viewportshake)
  * [ViewportAwait](#viewportawait)
  * [ViewportHalt](#viewporthalt)
* [Ambient Light Control](#ambient-light-control)
  * [AmbientLightShift](#ambientlightshift)
  * [AmbientLightReset](#ambientlightreset)
  * [AmbientLightAwait](#ambientlightawait)
  * [AmbientLightHalt](#ambientlighthalt)
* [Displaying CGs](#displaying-cgs)
  * [CGShow](#cgshow)
  * [CGHide](#cghide)
* [World Control](#world-control)
  * [WorldAdvanceTime](#worldadvancetime)
  * [TemporaryMapTiles](#temporarymaptiles)
  * [TemporaryMapOverride](#temporarymapoverride)
* [Conditional Execution](#conditional-execution)
  * [If/ElseIf/Else/EndIf](#ifelseifelseendif)
* [Repeat Blocks](#repeat-blocks)
  * [RepeatCount](#repeatcount)
  * [RepeatTime](#repeattime)
  * [RepeatEnd](#repeatend)
* [Event Variables](#event-variables)
  * [VarSet](#varset)
  * [VAR_QUERY](#var_query)
  * [VarEval](#vareval)
* [Vanilla Command Notes](#vanilla-command-notes)



# General Notes

Here are a few terms and things to know to help you read this document.

If you are here for the beta, please check out the example text files in
the beta zip! There are some (fairly simple) scripts in there that may help
you understand a particular command more clearly than my prose here.

Each one is formatted correctly to be used as a test event, so simply copy
the `.txt` file to your game directory, rename it `test_event.txt`, and
run `debug rte` in your SMAPI console to run it directly. There is no need
to patch reload or anything else; it will be read live.

When a command says that it "blocks" (verb) (or talks about another command
"blocking"), that means that it will pause at that point in the command
list and prevent the stream from continuing until some condition is met.
For example, `move <npc> 3 0 2` blocks, because the optional `true`
argument was not given, so the stream will wait until the move completes
before running the next command.

**Note**: Unlike vanilla, Codex commands that have a behavior toggle argument
(e.g. `ViewportMove`) are set up to expect a particular literal string instead
of the boolean strings `false`/`true`. I made this choice for ideological
reasons: I prefer when a boolean argument makes its purpose more clear at the
call site, since for me it reduces the burden of remembering what "true"
stands for in a particular command.

For example, `ViewportMove` has an optional argument `wait`. It expects to
find the string "wait" there, not the string "true".



# Stream Control

"Streams" are the headline feature of the Codex. They allow you to implement
parallel execution of multiple command lists, giving you a lot more control
over timing than a single command list with only limited support for
simultaneous commands. At its most basic, this allows you to run almost any
commands simultaneously, although there are some perhaps-unintuitive
restrictions (see [Vanilla Command Notes](#vanilla-command-notes) for details).

The quick overview is that you declare a stream and give it a list of commands.
At that time, the stream begins executing, but does not block the main command
list, which continues from the command following the stream. Since a stream is
independent of any other command list, you can use blocking and non-blocking
commands as you desire within it, as if it were its own main event script.

When a stream exhausts its command list, it becomes idle. You can restart it
if desired, or overwrite it with new commands, or issue a command from any
other stream that will wait for it to reach this state.

The power this gives you might not be obvious, so let's consider an example:

```
Saloon
continue
12 21
farmer 11 20 0 Elliott 12 20 0 Gus 12 18 2
pause 1200
speak Elliott "... and that will be all. Thank you, Gus.$h"
-- the stream starts here
ichortower.ECC_StreamStart gusfetching
pause 820
emote Gus 40
advancedMove Gus false 2 0 0 -1 4 10
pause 2800
textAboveHead Gus "What? I'm out?"
pause 2100
advancedMove Gus false 1 0 4 1800 -2 0 4 3520
ichortower.ECC_ActorAwaitMovement Gus
ichortower.ECC_StreamEnd
-- meanwhile, the event continues here
faceDirection Elliott 3
faceDirection farmer 1
speak Elliott "I, for one, am looking forward to this dish. I haven't had it in some time!"
emote farmer 32
speak Elliott "Yes, there's nothing quite like it. A symphony of flavors!"
faceDirection Elliott 0
faceDirection farmer 0
-- wait for the stream to end
ichortower.ECC_StreamAwait gusfetching
advancedMove Gus false 0 1 -1 0 2 10
pause 220
speak Elliott "Ah, this must be our food now.$h"
speak Gus "Here you go! Fresh as you like!$h"
emote Elliott 8
pause 250
emote Elliott 12
speak Elliott "Gus! What is going on here? Have you made a substitution?$a"
emote Gus 16 true
emote farmer 28
end
```

... which looks like this:

https://github.com/user-attachments/assets/898260c3-f7a2-45de-9a6a-3803ec252805

The important thing to understand is that while the main event uses some
commands that block on player input (`speak`, in this case), the stream runs
independently and can manage its own commands without the need to have them
interleaved with the main script. This example is fairly simple (a few
`advancedMove`s with a single `textAboveHead` in between), but even that
would be difficult to approximate with vanilla commands.


### `StreamStart`

`ichortower.ECC_StreamStart <id>`\
`ichortower.ECC_StreamBegin <id>` (alias)

This command declares a stream. When it is encountered, it immediately scans
ahead through the command list looking for a matching `StreamEnd` command, and
gives the intervening commands over to the new stream to begin executing (they
will not actually execute until the stream's event loop reaches them).

The id value can be any string, subject to two conditions:

1. it must not start with 'ECC' (case-insensitive; reserved for internal use)
2. it must not already be in use by another active stream

If the given id is invalid for one of the above reasons, the new stream will
be discarded.

Once a stream has completed or halted, it becomes inactive, and you may reuse
its id with a new `StreamStart` command to replace it; just make sure you don't
have any future commands that need to control the old stream!

After starting a stream, you use the same id you gave it in order to issue
commands from any other stream (`StreamAwait`, `StreamHalt`, etc.).


### `StreamEnd`

`ichortower.ECC_StreamEnd`

This command does not do anything on its own (in fact, executing it is an
error, since it means your Starts and Ends are not balanced). It is merely a
signal that the list of commands for a new stream is over.

There is no `id` argument: each stream takes the first balanced `StreamEnd` it
encounters, in order to allow nesting streams and prevent them from crossing
command list boundaries.


### `StreamAwait`

`ichortower.ECC_StreamAwait <id> [id...]`

This command takes one or more stream ids and blocks until they are all
complete (they have run out of commands and are sitting idle, or they have
been halted by the `StreamHalt` command).

Use this to guarantee that a particular stream (or streams) has finished before
proceeding. For example, if a stream is making an actor move to a particular
spot, you can await it in order to be sure that actor has arrived. Either they
will already be there, or you'll pause until they are.

A stream cannot await itself (this will cause an error); but there is not yet
any protection against circular awaiting, so be careful not to do that.


### `StreamHalt`

`ichortower.ECC_StreamHalt <id> [id...]`

This command terminates the streams specified in the arguments. A halted stream
has its command index set to beyond the end of its command list, and has its
state forcibly set to idle. As a result, it will immediately satisfy any other
stream that is awaiting it, and its id becomes available again for reuse.

A stream *can* halt itself, although I don't know what use case that has.


### `StreamSuspend`

`ichortower.ECC_StreamSuspend`\
`ichortower.ECC_StreamSleep` (alias)

This command causes the current stream to pause indefinitely at this point,
waiting until another stream restarts it with the `StreamResume` command. The
purpose is to allow synchronization without having to create lots of streams;
you can have a long-running stream that stops at specific points and will
resume only when another stream "catches up".

You can also use this command by its other name, `StreamSleep`.


### `StreamResume`

`ichortower.ECC_StreamResume <id> [id...]`\
`ichortower.ECC_StreamWake <id> [id...]` (alias)

This command tells the suspended streams specified in the arguments to resume
executing. If a given stream is not currently suspended, it cannot be resumed,
and **this command will block until that stream does suspend itself,**
whereupon it will immediately be told to resume.

Put another way, this command will block until it successfully resumes all
specified streams, so take care that all such streams will suspend, or you
may cause your event to softlock.

You can also use this command by its other name, `StreamWake`.


### `StreamRestart`

`ichortower.ECC_StreamRestart <id> [id...]`

This command restarts the specified streams. A restarted stream has its command
index set to 0 and its idle state forcibly unset.

This is intended for use on completed or halted streams, but it should work on
active ones as well.

A stream can restart itself (but see `StreamLoop`).


### `StreamLoop`

`ichortower.ECC_StreamLoop`

A shortcut version of `StreamRestart` which works only on the current stream
and restarts it by setting its command index to 0.

You can also use this in the main command list, but I don't see much use for
it until `goto` arrives in 1.6.16 (and when it arrives, you should just use
it instead of this).


# Stream-Safe Command Replacements

Some vanilla commands do not work as expected when used in streams (see
[Vanilla Command Notes](#vanilla-command-notes) for more details). For this
reason, the Codex includes replacements for some of them.

In general, you do not have to use these commands yourself: the Codex will
automatically replace the vanilla commands with the stream-safe versions when
a new stream contains them. However, they do sometimes have additional
features, so maybe you will find them useful.


### `Emote`

`ichortower.ECC_Emote <actor> <number> [wait]`

A replacement for `emote`, which can misbehave when used in a stream. It uses
the same arguments as the vanilla command, except for the last one: by default,
this command **does not** wait for the emote to finish before proceeding. To
obtain that behavior, give the optional argument `wait`.


### `FaceDirection`

`ichortower.ECC_FaceDirection <actor> <direction> [delay|duration]`

A replacement for `faceDirection`, which can misbehave when used in a stream.
The actor and direction arguments are the same as vanilla, but the optional
extra argument is different: it can be either the string `delay`, which causes
the default vanilla delay of 500 milliseconds, or an integer, which will cause
a delay of that many milliseconds. Like `Emote`, if the delay argument is
omitted, the default behavior is **not to block** after facing the actor.


### `Message`

`ichortower.ECC_Message <text>`

A replacement for `message`, which misbehaves when used in a stream. It works
exactly like the vanilla command, except for the misbehavior, and if you omit
the required parameter it will error once and proceed instead of softlocking
and spamming your SMAPI console.

**Note**: even though this version of `message` (and `speak`, below) is safe
to use in streams, it and its vanilla counterpart *will block* if there is
another dialogue box open in any other stream; the game does not support
having more than one open at once.


### `Pause`

`ichortower.ECC_Pause <int> [int...]`

A replacement for `pause`, which does not work in streams. Unlike `pause`,
this one accepts any number of integer arguments as pause durations, and will
choose one of them at random.

**Note**: the vanilla `pause` command uses a global timer. In addition to its
other properties which cause it to break streams, this timer *does not tick
down* when a menu or dialogue box is open, so a stream-safe Message or Speak
command will cause a `pause` in the main command list to block. In this
situation, you will need to manually use ECC's replacement.


### `Speak`

`ichortower.ECC_Speak <actor> <text-or-translation-key> [delay|duration]`

A replacement for `speak`, which misbehaves when used in a stream. The actor
and text/key arguments are the same as vanilla, but the optional extra argument
works like ECC's `faceDirection` replacement: you can use the string `delay`
to insert the standard delay of 500 milliseconds, or you can use an integer to
delay for a custom number of milliseconds, or omit it to have no delay at all.
Like with regular `speak`, the delay occurs *before* the dialogue box opens.

**Note**: Like `Message`, above, only one dialogue box can be open at once, so
this will block if another stream is displaying a speak or message box.



# Simultaneous Commands

If you're like me, you may have been excited to discover vanilla's
`beginSimultaneousCommand`/`endSimultaneousCommand` pairing, and pictured how
they might work, only to discover that they fulfill their promise only for a
narrow subset of event commands. Streams (see above) are my answer to that
problem in general, but I have also introduced these commands to provide a
way to accomplish what these commands sound like they should do.


### `Merge`

`ichortower.ECC_Merge`

This command takes no parameters, and declares the start of a simultaneous
command block. Starting with the next command and proceeding until the matching
`MergeEnd` command, every command will be automatically placed in its own
stream, and the event loop will then await all of the streams. Put another way,
each listed command will be independently executed, all in parallel, and when
they are all done, the event will continue.

As normal for parallel execution (see Streams, above), you should be aware of
behavior that might collide, and you must avoid commands that break streams
and do not have stream-safe replacements (e.g. `quickQuestion`).

**Note**: This command scans ahead for the matching `MergeEnd` and does not
have special handling for other types of block command (`Repeat`, `Stream`,
`Merge`, etc.), so do not attempt to define any type of block inside of a
Merge block: your event *will* break.

However, it is totally fine to use a Merge block inside of a stream. And if
you need to put a stream inside a merge for some reason, you can define it
elsewhere and use `StreamAwait` as one of your merge commands.


### `MergeEnd`

`ichortower.ECC_MergeEnd`

This command takes no parameters, and declares the end of a simultaneous
command block.

Executing it is an error, just like `StreamEnd`.



# Actor Control

These commands give you more flexibility when controlling actors (characters).


### `ActorPathfind`

`ichortower.ECC_ActorPathfind <actor> <x> <y> <facing> [wait]`

This command tells any actor (farmer or NPC) to move to the given map
coordinates, by using the pathfinder to figure out how to get there instead of
relying on you giving them directions.

The `<x>` and `<y>` arguments are expressed in map tiles, just like with `move`
and `advancedMove`, and will accept a few different formats:

* a plain positive integer (e.g. `12`, `38`): absolute coordinate
* plain `0`, or `+` or `-` with an integer (e.g. `0`, `+2`, `-8`): relative coordinate
* `a` with an integer (e.g. `a42`, `a0`, `a-1000`): absolute coordinate

So, for example, `ichortower.ECC_ActorPathfind Emily +2 +6 2` tells Emily to
find her way to the spot 2 tiles right and 6 tiles down from where she is
currently, then face down. Likewise, `ichortower.ECC_ActorPathfind farmer 22 15 1`
tells the farmer to go to the absolute coordinates (22, 15) and face right.

The `a` prefix is unlikely to be a common need, but it allows you to specify an
absolute zero or negative coordinate, since those would otherwise be interpreted
as relative.

This is very useful if you have been using streams and an actor has been
halted at an unknowable point along a looping `advancedMove`, or any similar
situation where you don't know where they are exactly but want them to reach
a fixed point. Or if you just don't want to do the math yourself!

Note that while the farmer will consider NPCs to be obstacles and will
navigate around them (using the positions they were in when the route was
calculated), NPCs will walk through each other. I may address this in the
future, if I can figure out how.


### `ActorAwaitMovement`

`ichortower.ECC_ActorAwaitMovement <actor> [actor...]`

This command blocks until all named event actors have completed their current
movements. This works a lot like vanilla's `waitForAllStationary` (all actors)
and `proceedPosition` (one actor only), but it allows any number of actors,
and it checks for ongoing movement a bit differently.

In particular, this command does not consider a character in a pause step
during an advancedMove to have stopped (`waitForAllStationary` and
`proceedPosition` both do this). This means that using this command to wait for
a looping `advancedMove` will block forever, so do not do this without a plan to
call `ActorHalt` from some other stream.


### `ActorAwaitAnimation`

`ichortower.ECC_ActorAwaitAnimation <actor> [actor...]`

This command is just like `ActorAwaitMovement`, except instead of blocking to
wait for movement to finish, it waits for animations. This accounts for sprite
animations using the `animate` command, as well as `emote`.

Note that just like `ActorAwaitMovement`, this will block forever if it is
awaiting a looping animation, so be careful not to do that without a way to
use `ActorHalt`.


### `ActorHalt`

`ichortower.ECC_ActorHalt [next|waitnext] <actor> [actor...]`

This command stops the movement and animation of all named actors, and removes
any NPCControllers that may have been puppeting them.

The first argument is optional and can be one of two special strings in order
to change the behavior (both case-insensitive):

* `next`: actors will be allowed to finish the current leg of their movement
    before halting.
* `waitnext`: as `next`, but this command will also block until the movements
    complete. this is done by inserting an `ActorAwaitMovement` command.

In general, it is best to use the optional argument, in order to have your
characters stop squarely on tiles, instead of stopping in between them (being
"off the grid" can cause problems with positioning).

Likewise, in general I advise using `waitnext` over `next`, since some moves
may not halt correctly without `ActorAwaitMovement` to help unstick them.
But if you are already awaiting the movement in another stream, `next` will
suffice.



# Viewport Control

These commands are intended to replace `viewport move` with a version that I
find more sensible: it uses tile units instead of pixels per frame, and you
can queue movements as well as wait for them to complete.

The other forms of vanilla's `viewport` command should still serve; they work
well.

### `ViewportMove`

`ichortower.ECC_ViewportMove <x> <y> <duration> [override] [wait]`

This command sets up a viewport movement.

`x` and `y` are given in map tiles, and can accept a few different formats,
exactly the same as [`ActorPathfind`](#actorpathfind).

* a plain positive integer (e.g. `12`, `38`): absolute coordinate
* plain `0`, or `+` or `-` with an integer (e.g. `0`, `+2`, `-8`): relative coordinate
* `a` with an integer (e.g. `a42`, `a0`, `a-1000`): absolute coordinate

So you could move the viewport to (14, 20) by giving `14 20`, or you could move
it 3 tiles down from its current position by giving `0 +3`.

`duration` is in milliseconds and determines how long the move will take to
complete.

By default, a viewport move will be queued behind any ongoing moves, and the
command will not block. The optional arguments `override` and `wait` can be
given to change this behavior: `override` will cause the existing queue to be
emptied before starting this move, and `wait` will cause the command to block
until the queue has been finished.

**Note:** the movements set up by this command are totally separate from how
vanilla's `viewport move` moves the viewport. Do not mix and match them.


### `ViewportShake`

`ichortower.ECC_ViewportShake <intensity> <duration> [override] [wait]`

This command sets up a screenshake effect for a given time. In general, it works
just like `ViewportMove` but operates a separate queue; all moves go in the move
queue, and all shakes go in the shake queue. Just like with `ViewportMove`, the
optional `override` and `wait` arguments control whether to queue the shake and
whether to block until the queue is empty.

`intensity` is an integer and determines the maximum distance in screen pixels
that the viewport can move away from its normal position in either direction; for
example, `4` means each coordinate will be adjusted by between -4 and +4 pixels
on each frame.

`duration` is an integer and gives the screen shake time, in milliseconds.


### `ViewportAwait`

`ichortower.ECC_ViewportAwait [move] [shake]`

This command blocks until one or both of the viewport queues have completed.

With no arguments, this will block until both queues (moves and shakes) are empty.
Specify just one type (`move` or `shake`, both case-insensitive) to wait for just
that queue and leave the other one to run. Specifying both is equivalent to the plain
no-arguments version, but is more explicit.


### `ViewportHalt`

`ichortower.ECC_ViewportHalt [move] [shake]`

This command immediately empties one or both of the viewport queues, preventing
them from continuing.

The `move` and `shake` arguments are parsed just like `ViewportAwait`; leaving
them out will default to stopping both queues.



# Ambient Light Control

Although vanilla has the `ambientLight` command which lets you set the ambient
light color and intensity at any time, it is merely immediate and there is no
way to transition smoothly. These commands give you the ability to fade it
gradually and queue the operations, just like with the viewport control
commands; you can use this to help simulate things like sunsets.


### `AmbientLightShift`

`ichortower.ECC_AmbientLightShift <red> <green> <blue> <duration> [override] [wait]`

This command sets up a gradual ambient light shift.

`red`, `green`, and `blue` should be integers from 0 to 255, representing the
RGB values of the color that will be **subtracted** from white to generate the
game tint, just as it is with vanilla's `ambientLight` command.

**Note:** The game's draw code has special treatment for the value `255 255 255`
(pure white), so using it may cause confusion: while you might expect this
value to generate pure darkness, instead it is treated as "disable ambient
light", and will cause the game to fall back to its outdoor lighting value (if
applicable) or to `0 0 0` (pure black, meaning full light with no tinting).
This mod attempts to handle these situations gracefully, but it is difficult,
so in general I recommend not using `255 255 255` unless you know what you're
doing.

`duration` is in milliseconds and determines how long the shift will take to
complete.

Like with `ViewportMove`, by default, the shift will be queued behind any
ongoing shifts, and the command will not block. Just like that command, you can
give the optional argument `override` to first empty the queue before starting,
and you can give the optional argument `wait` to block until the queue empties.


### `AmbientLightReset`

`ichortower.ECC_AmbientLightReset <duration> [override] [wait]`

This command sets up a shift, just like `AmbientLightShift`, except the values
used for the shift are automatically set to the ambient light values that were
in place when the event started. This allows you to easily undo whatever
ambient light changes you made during your event without needing to know what
they were ahead of time.

The `duration`, `override`, and `wait` arguments work just like above.


### `AmbientLightAwait`

`ichortower.ECC_AmbientLightAwait`

This command blocks until all queued ambient light shifts have completed.


### `AmbientLightHalt`

`ichortower.ECC_AmbientLightHalt`

This command immediately halts all ongoing ambient light shifts and empties
the light shift queue.



# Displaying CGs


### `CGShow`

`ichortower.ECC_CGShow <texture> [sourceRect:x,y,w,h] [transition:type,duration[,x,y]] [scaling:type[,i]] [color:#color] [letterbox:#color] [wait]`

This command displays an image of your choosing on top of the world layer and
below the UI layer, intended to be used for full-screen or otherwise dramatic
representations of special or important moments. There are a lot of arguments,
most of which are optional and can be given in any order. They are all
**case-insensitive**.

Most of the arguments expect a specific keyword followed by a colon `:` and
then the parameters for the argument separated by commas `,`. See the argument
descriptions for details, but in all cases please take care not to use spaces.

- `texture` is the one non-optional argument and must be given first. It should
  be a *game asset path* identifying which image to display, like
  `Portraits/Abigail`. As is usual for asset names provided in slash-delimited
  JSON, you will need to escape your quotes `\"` and use double backslashes
  `\\` in the asset name, so a correct texture argument might look like this:
  `\"Portraits\\Abigail\"`.
- `sourceRect` uses its four parameters to specify the region of the texture
  to display (by default, CGShow will use the entire texture).
- `transition` specifies how the image should make its initial appearance. Two
  parameters are required: the type of transition to use and how long it should
  take (in milliseconds). The type must be one of the following:
  - `zoom`: the image will zoom in from 0x0 to full size. This transition type
    supports two additional parameters `,x,y`: two integers giving tile
    coordinates to specify a tile that should serve as the origin for the zoom.
  - `fade`: the image will fade from 0% opacity to 100%.
  - `pan`: the image will slide into view from offscreen. This transition type
    *requires* two additional parameters `,x,y`: two integers giving a direction
    vector pointing to the initial offscreen position. For example, `,-1,-1`
    means equal amounts left and up, while `,2,5` means two parts right to five
    parts down.
- `scaling` specifies how the image should be displayed once the transition is
  over. The required type parameter must be one of the following:
  - `fit`: the image will fit entirely on screen, preserving aspect ratio
    (most likely, some space will be left uncovered).
  - `cover`: the image will cover the screen, preserving aspect ratio
    (most likely, the image will be cropped/cut off).
  - `stretch`: fill the entire screen, disregarding aspect ratio
    (most likely, the image will be stretched or squashed).
  - any floating-point number: use an absolute scaling factor

  The optional parameter `i` means to use integer scaling only; for example, if
  you set an image to `fit` and that requires a scale of 3.221, it will be
  displayed at 3x instead.

  So, for example, you might give the argument `scaling:fit,i`, which would
  scale the image as much as possible so that it fits entirely on screen, while
  also using an integer scaling factor.
- `color` is used to tint the image by setting the draw color (as opposed to
  the default white). The color value parameter can be an RGB or RGBA hex code
  `#rrggbb` or `#rrggbbaa`, or it can be
  [any valid MonoGame color name](https://github.com/ichortower/FontSmasher/blob/prod/docs/color-table.md).
  At this time, decimal rgb(a) values are not supported (they would require
  spaces).
- `letterbox` takes a color value just like `color`, except it sets the color
  used to fill in any unused space around the image, filling the remainder of
  the screen with a solid color. By default, this is transparent, so there is
  no visible letterbox.
- `wait` takes no parameters, but tells ECC to block the event loop until the
  transition completes and the CG is fully displayed.

You can show multiple CGs if you wish. Each subsequent one will be displayed
on top of all the previous ones (see `CGHide` for how to hide them).


### `CGHide`

`ichortower.ECC_CGHide [index] [transition:type,duration[,x,y]] [wait]`

This command hides an image that was previously displayed using `CGShow`. All
of its arguments are optional: if none are given, this simply hides the most
recently shown CG, reversing its transition (if applicable) so that it plays
the reverse of the original animation. If you want to use a different
transition, you can specify it here in exactly the same format as given above
for `CGShow` in order to override the one it used before.

The `index` argument is 0-based and tells ECC which CG to hide, if you desire
to hide them in a different order: `0` is always the bottom-most CG, and the
index counts up from there. **Note**: when a CG is hidden and its hiding
transition completes, it will be removed from the list *and the indexes of all
remaining CGs will decrease accordingly*. Make sure you plan for this if you
want to get fancy with your ordering.

Finally, like with `CGShow`, the `wait` argument takes no parameters and tells
ECC to block the event loop until the outro transition completes and the CG is
fully hidden.



# World Control


### `WorldAdvanceTime`

`ichortower.ECC_WorldAdvanceTime <hhmm>`

This command causes world time to pass when the event finishes. In addition to
advancing the game clock, machines are given processing time and all NPCs are
automatically advanced along their daily schedules to be ready for their next
move (spouses with no schedule will be warped to bed if the target time is
after 2200).

Give the time as an integer between 600 and 2600, but it must be after the
current game time or you will get an error.

In multiplayer, this command has no effect: events don't freeze time in
multiplayer, so time will pass just by watching it.

**Note:** the machine processing and NPC advancing will happen immediately when
this command executes, but the time change will be delayed until the event
actually ends.

**Note:** If possible, you should find a way to warn players ahead of time
that you plan to use this command, and ideally give them a chance to avoid it.
They are likely accustomed to events taking no time, and may have plans for
their day which you may ruin if you surprise them with this.

This command is also available as a trigger action, using the same name, and
should work anywhere actions are accepted (except in multiplayer, as above).
When triggered outside of an event, the time change will be immediate.


### `TemporaryMapTiles`

`ichortower.ECC_TemporaryMapTiles (<layer> <x> <y> <sheet> <index>)+`

This command temporarily replaces map tiles on the current map, reverting them
to their previous state (by reloading the map) when the event ends.

Use layer, x, and y to specify what tile to change. Sheet and index are to
tell what to change that tile to. To remove a tile, use `-1` for index; in this
case, sheet will be ignored, so you can pass something meaningless like `-`.


### `TemporaryMapOverride`

`ichortower.ECC_TemporaryMapOverride (<asset> <x> <y>)+`

This command temporarily applies one or more map overrides to the current
location. The overrides will be removed (and the map reloaded) when the event
ends.

The asset name is expected to be under `Maps/`, so to apply the map asset at
`Maps/foo/bar`, you should specify just `foo/bar`. The x and y values are map
tile coordinates of where to overlay it (the top-left corner, just like you
would specify in e.g. a Content Patcher pack).



# Conditional Execution


### `If`/`ElseIf`/`Else`/`EndIf`

`ichortower.ECC_If <Game state query>`\
`ichortower.ECC_ElseIf <Game state query>`\
`ichortower.ECC_Else`\
`ichortower.ECC_EndIf`

These commands define a set of conditional command blocks. Just like a
conventional programming language, the conditions are checked in order, and as
soon as one is satisfied, that block is executed and the others are discarded
without being evaluated.

You can put any number of event commands between the control commands, e.g.:

```
ichortower.ECC_If PLAYER_NPC_RELATIONSHIP Current Abigail married
emote Abigail 16
speak Abigail "That's so rude! You're talking about my ${husband^wife^spouse}$!$a"
emote Pierre 40
ichortower.ECC_EndIf
```

In this example, the three commands (emote/speak/emote) are only executed if
the player is married to Abigail. But you can add more blocks, too:

```
ichortower.ECC_If PLAYER_NPC_RELATIONSHIP Current Abigail married
emote Abigail 16
speak Abigail "That's so rude! You're talking about my ${husband^wife^spouse}$!$a"
emote Pierre 40
ichortower.ECC_ElseIf PLAYER_NPC_RELATIONSHIP Current Abigail dating
emote Abigail 12
speak Abigail "Hey! I happen to like that person quite a lot!$a"
ichortower.ECC_Else
speak Abigail "Who? Farmer @?$u"
ichortower.ECC_EndIf
```

In this case, you'll get the first block (`If`..`ElseIf`) if married, the
second block (`ElseIf`..`Else`) if dating, and the third block
(`Else`..`EndIf`) for any other relationship.

You can have as many `ElseIf` blocks as you like, but only one `Else`, and
the `Else` must come last. Violations of this order will parse, but you will
get a warning about the blocks not being reachable.

You *should* be able to nest these commands, but I haven't tested that yet.

**Note:** the game state queries that drive this are evaluated when the
blocks are parsed for execution, so they are "real time" and may reflect
changes to game state that have occurred earlier in the event.



# Repeat Blocks

These commands let you repeat a section of your event some number of times,
or for a certain amount of time. **They cannot be nested**, so do not attempt
to do that, but they can safely be used in streams.


### `RepeatCount`

`ichortower.ECC_RepeatCount <number>`

This command declares the start of a block which should be repeated a certain
number of times. All of the commands between this and the next `RepeatEnd`
command will be executed repeatedly. The `number` argument should be an
integer.

For example:

```
ichortower.ECC_RepeatCount 5
pause 400
emote Abigail 16
speak Abigail "What? No way!$u"
ichortower.ECC_RepeatEnd
```

This is not quite equivalent to just listing those commands 5 times in a row,
since one frame will be consumed executing the `RepeatEnd` command each
iteration, but it's mostly equivalent.


### `RepeatTime`

`ichortower.ECC_RepeatTime <milliseconds>`

This works just like `RepeatCount`, except that the argument is a number of
milliseconds, and the commands will be repeated until at least that number of
milliseconds has elapsed since the block began.

For example:

```
ichortower.ECC_RepeatTime 12500
pause 400
emote Abigail 16
speak Abigail "What? No way!$u"
ichortower.ECC_RepeatEnd
```

This block will be repeated until 12.5 seconds (total) have elapsed. This may
turn out to be a variable number of times, since in this case player input
(`speak`) is involved.

Note that the time is checked at the end of the block, so the block will always
execute fully and will not stop partway through an iteration. This also means
the time argument is a *minimum* duration, and the actual runtime may
(significantly) exceed it.


### RepeatEnd

`ichortower.ECC_RepeatEnd`

Declares the end of a repeat block. Takes no arguments.



# Event Variables

This command and game state query are intended for use with the Conditional
Execution blocks (see above). You can use them to store and manipulate integer
or string values, then change your event's behavior accordingly.


### `VarSet`

`ichortower.ECC_VarSet <name> <value|expression>`

This command sets a user-defined variable to the value or expression that
follows it. You can use it to save a value for later (say, the player's choice
in some situation), or to keep track of a running total, or things like that.

When picking a variable name, there are a few restrictions:

1. it must not start with 'ECC' (case-insensitive; reserved for internal use)
2. it must be **alphanumeric only** ([a-zA-Z0-9])
3. it must contain at least one letter ([a-zA-Z])

Other than those, you can choose whatever you like. **Variable names are
case-sensitive**.

The variables are purely local to the event, and are purged when the event
ends, so you need not worry about collisions (except with yourself). However,
they are freely accessible from any stream, to both read and write.

The expression parsing and evaluation proceeds as you might expect, including
operator precedence, but note that it supports only integers and strings (no
floating-point numbers!): the `.` symbol is a low-priority concatenate operator
and may cause confusing results if used by mistake.

**Note**: every resulting value that this command generates is serialized to a
string, including during the parsing and evaluation steps. You might not need
to know this, but if something weird happens, maybe that will help you figure
it out.

See the following table for what symbols are supported. They are listed in
precedence order, although the variable and string notes are not ranked (so,
excluding those, parentheses are highest, then exponent, and so on).

<table>
<tr>
<th>Symbol</th><th>Examples</th><th>Explanation</th>
</tr>

<tr>
<td>

plain name

</td>
<td>

`myvar`

</td>
<td>

To reference a variable in an expression, just use its name as-is (but remember
that it is case-sensitive). When evaluated, it will be replaced with the value
you last stored in it.

A variable must be set before referencing it, or you will get an evaluation
error.

</td>
</tr>

<tr>
<td>

`' "`

</td>
<td>

`'foobar'`\
`"text value"`

</td>
<td>

Use `'` or `"` to enclose a string literal. Note that event scripts are given
as long delimited strings, and in typical Content Patcher use you will need to
escape your double-quotes (e.g. `myvar . \"my string\"`) in order for them to
work properly, since they would end the JSON string otherwise.

</td>
</tr>

<tr>
<td>

`()`

</td>
<td>

`(1 + 2) * 3`

</td>
<td>

Use parentheses to enclose operations and increase their precedence. In the
example, `1 + 2` is evaluated first to `3`, then `3 * 3` is evaluated to `9`.
Without the parentheses, `2 * 3` evaluates first to `6`, then `1 + 6` becomes
`7`.

</td>
</tr>

<tr>
<td>

`^`

</td>
<td>

`myvar ^ 2`

</td>
<td>

The exponent operator will work only on integers and will not accept strings.
Be careful not to exceed the positive integer limit of 2^31-1, or this will
return the negative integer limit.

</td>
</tr>

<tr>
<td>

`* \`

</td>
<td>

`myvar * 4`\
`myvar * 3 \ 2`

</td>
<td>

Multiply `*` and Divide `\` will work only on integers and will not accept
strings.

**Note**: Backslash `\` is also the JSON escape character, so in typical
Content Patcher use you will need to type two backslashes `\\` to represent
one, just like in vanilla's `quickQuestion`. The parser will also accept `/`,
but in an event context I'm not sure it's possible to provide one, since that
breaks to a new event command.

**Reminder**: floating-point is not supported, so `\` will drop any decimal
portion of its result. `9 \ 2` yields `4`.

</td>
</tr>

<tr>
<td>

`+ -`

</td>
<td>

`myvar + 4`\
`-1 * (myvar - 1)`

</td>
<td>

Add `+` and Subtract `-` will work only on integers and will not accept
strings. If you wish to add strings together, use Concatenate `.`.

In addition to binary operation, `-` will work to negate an integer literal
(so `-50` will behave as expected). But at this time, it will not work in
this way on variable names, strings, or anything else, so you may need to
be more explicit to avoid parse errors (e.g. `-1 * myvar` instead of `-myvar`).

</td>
</tr>

<tr>
<td>

`= !=`

</td>
<td>

`myvar = 4`\
`(myvar + 1) != 6`

</td>
<td>

Compare two values for equality (`=`) or inequality (`!=`). This will evaluate
to the string `true` or `false`, as appropriate. It accepts strings or integers
(and is a lexical comparison, even for integers, so e.g. `1 = '1'` will
evaluate to `true`).

**Note for programmers**: You can use double-equals `==` for equality if you
want. You're welcome.

</td>
</tr>

<tr>
<td>

`< <= > >=`

</td>
<td>

`myvar < 5`\
`myvar >= anothervar`

</td>
<td>

Like `=` and `!=`, but for
less-than/less-than-or-equal/greater-than/greater-than-or-equal, respectively.

</td>
</tr>

<tr>
<td>

`.`

</td>
<td>

`myvar . 'foobar'`\
`myvar . 23`

</td>
<td>

The Concatenate (`.`) operator smushes the string representations of the two
surrounding values together. This will happily convert numbers to strings, so
`1 . 'asdf'` yields `'1asdf'`, and likewise `myvar . 15` takes the contents of
`myvar` and appends the string `'15'` to it.

**Note**: this is the lowest-precedence operator, so deploy parentheses if
needed.

**Note**: if you try to use a decimal number (e.g. `3 * 1.5`), the parser will
interpret the dot as this operator. The previous example would yield `35`.

</td>
</tr>

</table>

For example, you could use:

`ichortower.ECC_VarSet myvalue 4`

... which would set a variable called "myvalue" to the value 4. Or:

`ichortower.ECC_VarSet myvalue myvalue + 1`

... to increment its current value. You can combine a lot of stuff:

`ichortower.ECC_VarSet myvalue (1+4) * (12-5) . ' points'`

... which does the math as you would expect (5 \* 7), then concatenates it
with the text to generate the string "35 points".


### `VAR_QUERY`

`ichortower.ECC_VAR_QUERY <value|expression>`

This game state query uses the same expression parsing as `VarSet`, above, but
instead of storing the result into a named variable, it evaluates the
expression and returns true or false: false if it yields the string "false"
(case-insensitive) or the value 0, and true otherwise. Note that this means
that an empty string `""` and maybe some other unintuitive results will
evaluate to true.

This is intended for use with `If`/`ElseIf`/`Else`/`EndIf`. You can use it in
other contexts if you like, but I doubt that it is useful to do so: outside of
an event, you won't be able to access any variables.


### `VarEval`

`[ichortower.ECC_VarEval <value|expression>]`

This is a token resolver, for use in
[tokenizable strings](https://stardewvalleywiki.com/Modding:Tokenizable_strings).
This lets you evaluate your event variables inside parsed text: at the moment,
the only use I am aware of is in the `speak` command (but remember to use that
only on the main command list).

I suppose you could use it to do basic math in dialogue elsewhere, but
accessing the event vars is the real purpose.



# Vanilla Command Notes

There are some vanilla commands which cause problems when used in streams
outside of the main command list. Whenever possible, the Codex will
automatically replace them with its own alternatives when they appear in a new
stream's command list, so in most cases you should not need to know about these
behaviors and can keep using vanilla commands the way you are used to; but note
that any errors you get in your log will reference the replacement command
string.

The most common problem is that something started by a particular command
directly advances the main command list index from somewhere else in the
codebase. This both causes the main list to jump forward at inappropriate times
and also leaves the stream softlocked, since its index remains where it was.

The commands and their behavior are documented here. The ones without adequate
substitutes are so noted.

### `emote`

When not passing the optional `true` to avoid blocking, this is hardcoded to
advance the main command list when the emote expires. In a stream, your
`emote` command will be replaced as follows:

`'emote <actor> <num>'      -> 'ichortower.ECC_Emote <actor> <num> wait'`\
`'emote <actor> <num> true' -> 'ichortower.ECC_Emote <actor> <num>'`

### `faceDirection`

When not passing the optional `true` to avoid blocking, this command uses the
global pause timer (see `pause`, below) to implement its delay. In a stream,
your `faceDirection` command will be replaced as follows:

`'faceDirection <actor> <dir>'      -> 'ichortower.ECC_FaceDirection <actor> <dir> delay'`\
`'faceDirection <actor> <dir> true' -> 'ichortower.ECC_FaceDirection <actor> <dir>'`

### `message`

To proceed after the dialogue box closes, this command relies on `DialogueBox`
being hardcoded to advance the main command list. In a stream, your `message`
command will be replaced as follows:

`'message <text>' -> 'ichortower.ECC_Message <text>'`

### `pause`

This command uses a global field (`Game1.pauseTime`) which is hardcoded to
advance the main command list when it expires. In a stream, your `pause`
command will be replaced as follows:

`'pause <ms>' -> 'ichortower.ECC_Pause <ms>'`

**Reminder**: the vanilla `pause` command's global timer *does not tick down*
while any menu or dialogue box is open, so you may need to use ECC's
replacement directly if you have dialogue boxes in your streams.

### `quickQuestion`

In order to execute the embedded scripts, this command injects them hardcodedly
into the main command list. There is no substitute available at this time.

### `speak`

Just like `message`, this uses `DialogueBox` and relies on its hardcoded index
advance. If used in a stream, it will be replaced as follows:

`'speak <actor> <dialogue>' -> 'ichortower.ECC_Speak <actor> <dialogue> delay'`

### `speed`

When used with NPC actors, this command behaves as expected in any stream.
When used with a farmer, the speed change is local to the stream, and movements
in other streams will not see the value. There is no substitute, so you must be
aware of the behavior when using this command.

### `splitSpeak`

This command works just like `speak` (relying on `DialogueBox`'s hardcoding),
but I have not implemented a replacement for it, since I have never encountered
it in the wild.

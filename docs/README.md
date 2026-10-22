# Event Command Codex

This Stardew Valley mod is a framework (of sorts) which adds a bunch of new
event commands to the game. These are designed to give you new capabilities
when writing events, particularly involving event flow and timing. If you
have ever been confused by vanilla event commands, or frustrated by their
limitations, or if you have simply *had it* with `viewport move`, then this
mod may be for you.

Here are some of the things you can do with these commands:

* Set up parallel command lists that run without blocking the event loop,
  and suspend, resume, loop, halt, restart, and/or wait for them to complete
* Define and run your own custom, complex player control sequences
* Tell an actor to move to or face a specific tile and let them figure it out
* Stop some actors' movements, either immediately or at their next waypoint,
  and without having to stop all of them
* Show event CGs for important or dramatic moments
* Control the viewport with reasonable units and queueing
* Control the scene's ambient light over time with gradual transitions and
  queueing
* Execute blocks of commands conditionally with If/ElseIf/Else/EndIf
* Execute blocks a certain number of times, or for a certain length of time
* Set, do arithmetic on, and query your own event variables (goes well with
  conditional execution)
* Cause an event to "take time" by advancing the world time when it completes
  (correctly processing machines and advancing NPCs along their schedules)
* Make map tile changes or apply map overrides that last only until the
  current event ends

If these things appeal to you, head over to the [author guide](author-guide.md)
for details!


## How to Use

As always, mod users should simply unzip and install the mod into their Mods
folder. For many mod authors, the story is the same: most of this mod's
features are simply new event commands, and you can use them without any
special effort. The PlayerControl command requires you to edit a data asset,
but that is still easily done with Content Patcher, just like the events you
are writing.

For authors, this mod also comes with some bundled sample events, which you
can look at with your favorite text editor or watch live in-game with a
console command.

Consult the [author guide](author-guide.md) for specifics.


## Building

If you are attempting to build this mod from source, you will need
[TowerCore](https://github.com/ichortower/TowerCore), my shared code repository.
Clone it alongside this repository and you should have no problems.

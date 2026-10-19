using ichortower.TowerCore;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Extensions;
using System.Collections.Generic;
using System.Linq;

using SEvent = StardewValley.Event;

namespace ichortower.ECC.PlayerControl;

internal class Commands
{
    public static void command_PlayerControl(SEvent evt, string[] args, EventContext context)
    {
        if (CurrentState.SequenceActive()) {
            context.LogErrorAndSkip("Cannot start a PlayerControl sequence when" +
                    " one is already running");
            return;
        }
        if (!Object.ReferenceEquals(evt, Game1.CurrentEvent)) {
            context.LogErrorAndSkip("Cannot be used in a stream");
            return;
        }
        if (!ArgUtility.TryGet(args, 1, out string sequenceId, out string error)) {
            context.LogErrorAndSkip(error);
            return;
        }
        if (!Data.Dict.TryGetValue(sequenceId, out Sequence sequence)) {
            context.LogErrorAndSkip($"No sequence found with id '{sequenceId}'");
            return;
        }
        if (!Sequence.Validate(sequence, out error)) {
            context.LogErrorAndSkip(error);
            return;
        }

        CurrentState.Sequence = sequence;
        CurrentState.Id = sequenceId;
        CurrentState.State = SequenceState.WaitNext;
        evt.playerControlSequenceID = sequenceId;
        evt.playerControlSequence = true;
        evt.farmer.CanMove = true;
        evt.farmer.canOnlyWalk = false;
        Game1.viewportFreeze = false;
        Game1.forceSnapOnNextViewportUpdate = true;
        Game1.globalFade = false;
        evt.doingSecretSanta = false;

        // as always, register on the main event to guarantee cleanup, although
        // this just references 'evt' since we know it's the main event
        evt.onEventFinished += delegate {
            CurrentState.Clear();
            StopInputHandler();
        };

        StartInputHandler();
        evt.InsertNextCommand($"{Main.ModId}_PlayerControlUpdate");
        ++evt.CurrentCommand;
    }

    /*
     * Not for client use. Automatically injected to handle updates during
     * player control sequences.
     */
    public static void command_PlayerControlUpdate(SEvent evt, string[] args, EventContext context)
    {
        if (!CurrentState.SequenceActive() || CurrentState.State == SequenceState.Finished) {
            evt.EndPlayerControlSequence();
            evt.OnPlayerControlSequenceEnd(null);
            CurrentState.Clear();
            StopInputHandler();
            ++evt.CurrentCommand;
            return;
        }
        // we need to force this back to true whenever weird fuckery unsets it,
        // or else we get speedwalking and other side effects from streams.
        // but it's (probably) cheaper to just blast it every frame instead of
        // trying to be clever
        evt.farmer.CanMove = true;

        string[] script;
        string err;
        string streamId;

        switch (CurrentState.State) {
        case SequenceState.AwaitingScript:
            string id = CurrentState.AwaitingScriptId;
            SequenceState NextState = SequenceState.WaitNext;
            if (id.EndsWithIgnoreCase("Begin")) {
                NextState = SequenceState.Running;
            }
            if (!Streams.OpenStreams.TryGetValue(id, out SEvent target)) {
                Log.Warn($"tried to wait for missing stream '{id}'");
                CurrentState.State = NextState;
                break;
            }
            if (!target.IsStatus(StreamStatus.Ended)) {
                break;
            }
            CurrentState.State = NextState;
            CurrentState.AwaitingScriptId = null;
            break;
        case SequenceState.WaitNext:
            int f = CurrentState.NextStageIndex;
            CurrentState.CurrentStageIndex = f;
            if (f == -1) {
                CurrentState.State = SequenceState.Finished;
                break;
            }
            CurrentState.State = SequenceState.Running;
            CurrentState.SetUpConditions();
            CurrentState.SetStageDialogue(evt);
            if (CurrentState.Stage?.ScriptOnBegin is null ||
                    CurrentState.Stage.ScriptOnBegin.Count == 0) {
                break;
            }
            if (!CurrentState.Sequence.GetScript(CurrentState.CurrentStageIndex,
                    CurrentState.Stage.ScriptOnBegin.ToArray(),
                    out script, out err)) {
                Log.Warn(err);
                break;
            }
            streamId = $"ECC_{CurrentState.Id}_Stage{CurrentState.CurrentStageIndex}Begin";
            if (!Streams.New(evt, streamId, script, out err)) {
                Log.Warn(err);
                break;
            }
            CurrentState.AwaitingScriptId = streamId;
            CurrentState.State = SequenceState.AwaitingScript;
            break;
        case SequenceState.Running:
        default:
            CurrentState.CheckTimePasses(Game1.currentGameTime);
            CurrentState.CheckTileType(evt.farmer.TilePoint, ConditionType.TileTouch);
            if (!CurrentState.IsStageSatisfied()) {
                break;
            }
            CurrentState.State = SequenceState.WaitNext;
            if (CurrentState.Stage?.ScriptOnAdvance is null ||
                    CurrentState.Stage.ScriptOnAdvance.Count == 0) {
                break;
            }
            if (!CurrentState.Sequence.GetScript(CurrentState.CurrentStageIndex,
                    CurrentState.Stage.ScriptOnAdvance.ToArray(),
                    out script, out err)) {
                Log.Warn(err);
                break;
            }
            streamId = $"ECC_{CurrentState.Id}_Stage{CurrentState.CurrentStageIndex}Advance";
            if (!Streams.New(evt, streamId, script, out err)) {
                Log.Warn(err);
                break;
            }
            CurrentState.AwaitingScriptId = streamId;
            CurrentState.State = SequenceState.AwaitingScript;
            break;
        }
    }

    public static void command_PlayerControlSetNextStage(SEvent evt, string[] args, EventContext context)
    {
        if (!CurrentState.SequenceActive()) {
            context.LogErrorAndSkip("No PlayerControl sequence is running");
            return;
        }
        if (!ArgUtility.TryGet(args, 1, out string stageId, out string error)) {
            context.LogErrorAndSkip(error);
            return;
        }
        CurrentState.NextStageId = stageId;
        if (ArgUtility.TryGet(args, 2, out string imm, out error) &&
                imm.EqualsIgnoreCase("immediate")) {
            CurrentState.State = SequenceState.WaitNext;
        }
        ++evt.CurrentCommand;
    }

    public static void command_PlayerControlEnd(SEvent evt, string[] args, EventContext context)
    {
        if (!CurrentState.SequenceActive()) {
            context.LogErrorAndSkip("No PlayerControl sequence is running");
            return;
        }
        CurrentState.State = SequenceState.Finished;
    }


    internal static System.EventHandler<ButtonsChangedEventArgs> inputHandler = null;

    internal static void StartInputHandler()
    {
        if (inputHandler is not null) {
            return;
        }
        Log.Debug("starting input handler");
        inputHandler = InputFunction;
        Main.Helper.Events.Input.ButtonsChanged += inputHandler;
    }

    internal static void InputFunction(object sender, ButtonsChangedEventArgs e)
    {
        if (Game1.eventOver || !Game1.eventUp || !CurrentState.SequenceActive()) {
            StopInputHandler();
            return;
        }
        if (Game1.dialogueUp) {
            return;
        }
        SEvent evt = Game1.CurrentEvent;
        Point loc = GetTargetTile(evt.farmer);
        if (e.Pressed.Any(b => b.IsActionButton())) {
            if (!CurrentState.TryPlayDialogue(evt, loc)) {
                CurrentState.CheckTileType(loc, ConditionType.TileInteract);
                CurrentState.CheckTileType(loc, ConditionType.NpcInteract);
            }
        }
    }

    // duplicates the grab tile logic in Game1.pressActionButton
    internal static Point GetTargetTile(Farmer who)
    {
        Vector2 ftile = new Vector2(Game1.getOldMouseX() + Game1.viewport.X,
                Game1.getOldMouseY() + Game1.viewport.Y) / 64f;
        Point ret = new((int)ftile.X, (int)ftile.Y);
        if (!Game1.wasMouseVisibleThisFrame || Game1.mouseCursorTransparency == 0f ||
                !Utility.tileWithinRadiusOfPlayer(ret.X, ret.Y, 1, who)) {
            Vector2 f = who.GetGrabTile();
            ret.X = (int)f.X;
            ret.Y = (int)f.Y;
        }
        return ret;
    }

    internal static void StopInputHandler()
    {
        if (inputHandler is null) {
            return;
        }
        Log.Debug("stopping input handler");
        Main.Helper.Events.Input.ButtonsChanged -= inputHandler;
        inputHandler = null;
    }
}

using ichortower.TowerCore;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

using SEvent = StardewValley.Event;

namespace ichortower.ECC.PlayerControl;

internal static class CurrentState
{
    internal static string Id = null;
    internal static Sequence Sequence = null;
    internal static SequenceState State = SequenceState.WaitNext;
    internal static int CurrentStageIndex = -1;
    internal static string NextStageId = null;
    internal static string AwaitingScriptId = null;
    internal static List<ConditionState> ConditionStates = new();

    internal static int NextStageIndex {
        get {
            if (NextStageId is not null) {
                int f = Sequence.Stages.FindIndex(s => s.Id.EqualsIgnoreCase(NextStageId));
                if (f == -1) {
                    Log.Warn($"PlayerControl sequence requested next stage '{NextStageId}'," +
                            " but that stage could not be found.");
                }
                return f;
            }
            if (CurrentStageIndex >= Sequence.Stages.Count - 1) {
                return -1;
            }
            return CurrentStageIndex + 1;
        }
    }

    internal static SequenceStage Stage {
        get {
            if (CurrentStageIndex < 0 || CurrentStageIndex >= Sequence.Stages.Count) {
                return null;
            }
            return Sequence.Stages[CurrentStageIndex];
        }
    }

    internal static string StreamPrefix => $"ECC_{Id}_Stage{CurrentStageIndex}";


    internal static void Clear()
    {
        Id = null;
        Sequence = null;
        State = SequenceState.WaitNext;
        CurrentStageIndex = -1;
        NextStageId = null;
        AwaitingScriptId = null;
        ConditionStates.Clear();
    }

    internal static bool SequenceActive()
    {
        return Sequence is not null || Game1.CurrentEvent?.playerControlSequence is true;
    }

    internal static bool IsStageSatisfied()
    {
        SequenceStage stage = Stage;
        if (stage is null) {
            return false;
        }
        int hardrequired = 0;
        int completed = 0;
        for (int i = 0; i < ConditionStates.Count; ++i) {
            if (stage.AdvanceConditions[i].Required) {
                ++hardrequired;
                if (!ConditionStates[i].Satisfied) {
                    return false;
                }
            }
            else if (ConditionStates[i].Satisfied) {
                ++completed;
            }
        }
        if (stage.SatisfiedCount == -1) {
            return completed >= ConditionStates.Count - hardrequired;
        }
        return completed >= stage.SatisfiedCount;
    }

    internal static void SetUpConditions()
    {
        ConditionStates.Clear();
        foreach (AdvanceCondition ac in Stage.AdvanceConditions) {
            int len = ac.Type switch {
                ConditionType.TileTouch => ac.Tiles.Count,
                ConditionType.TileInteract => ac.Tiles.Count,
                ConditionType.NpcInteract => ac.Actors.Count,
                _ => 0,
            };
            ConditionStates.Add(new ConditionState() {
                Satisfied = false,
                ItemsSatisfied = Enumerable.Repeat(false, len).ToList(),
                Milliseconds = 0,
            });
        }
    }

    internal static void SetStageDialogue(SEvent evt, NPC who = null)
    {
        List<NPC> actors = (who is null ? evt.actors : new(){who});
        foreach (NPC n in actors) {
            n.TemporaryDialogue = new Stack<Dialogue>();
            if (!Stage.Dialogues.TryGetValue(n.Name, out string textOrKey)) {
                continue;
            }
            Dialogue d;
            if (Game1.content.IsValidTranslationKey(textOrKey)) {
                d = new(n, textOrKey);
            }
            else {
                d = new(n, null, textOrKey);
            }
            n.TemporaryDialogue.Push(d);
        }
    }

    internal static bool TryPlayDialogue(SEvent evt, Point loc)
    {
        NPC who = evt.actors.FirstOrDefault((n) => {
            return n.TilePoint.X == loc.X && n.TilePoint.Y == loc.Y;
        }, null);
        if (who is null || who.CurrentDialogue.Count == 0) {
            return false;
        }
        // don't use faceTowardFarmerForPeriod, since its timer (movementPause)
        // blocks npc movement from event commands, which may arrive at any
        // time. just set facing and revert when the dialogue closes.
        int priorDirection = who.FacingDirection;
        who.faceGeneralDirection(evt.farmer.getStandingPosition(), 0,
                opposite:false, useTileCalculations:true);
        Game1.drawDialogue(who);
        Game1.afterDialogues += delegate {
            // i don't think this will ever be satisfied but just in case
            if (!Game1.isFestival() || !Game1.currentLocation.currentEvent.canMoveAfterDialogue()) {
                --Game1.currentLocation.currentEvent.CurrentCommand;
            }
            if (who.CurrentDialogue.Count == 0) {
                SetStageDialogue(evt, who);
            }
            who.faceDirection(priorDirection);
        };
        return true;
    }

    internal static void CheckTileType(Point loc, ConditionType type)
    {
        if (type == ConditionType.TimePass || type == ConditionType.None) {
            return;
        }
        SequenceStage stage = Stage;
        string[] script;
        string error;
        string streamId;
        // these get lambdas are just to support getting the data from the
        // correct condition fields (between Tiles and Actors).
        var GetRequired = (int i, int j, ConditionType t) => {
            return t switch {
                ConditionType.TileTouch or
                ConditionType.TileInteract => stage.AdvanceConditions[i].Tiles[j].Required,
                ConditionType.NpcInteract => stage.AdvanceConditions[i].Actors[j].Required,
                _ => false,
            };
        };
        var GetPoint = (int i, int j, ConditionType t) => {
            return t switch {
                ConditionType.TileTouch or
                ConditionType.TileInteract => new Point(stage.AdvanceConditions[i].Tiles[j].X,
                                                        stage.AdvanceConditions[i].Tiles[j].Y),
                ConditionType.NpcInteract => Game1.CurrentEvent?.getActorByName(
                        stage.AdvanceConditions[i].Actors[j].Name)?.TilePoint ?? new Point(),
                _ => new Point(),
            };
        };
        var GetScript = (int i, int j, ConditionType t) => {
            return t switch {
                ConditionType.TileTouch or
                ConditionType.TileInteract => stage.AdvanceConditions[i].Tiles[j].Script?.ToArray(),
                ConditionType.NpcInteract => stage.AdvanceConditions[i].Actors[j].Script?.ToArray(),
                _ => null,
            };
        };
        var GetNpcName = (int i, int j, ConditionType t) => {
            return t switch {
                ConditionType.NpcInteract => stage.AdvanceConditions[i].Actors[j].Name,
                _ => null,
            };
        };
        for (int i = 0; i < ConditionStates.Count; ++i) {
            if (ConditionStates[i].Satisfied == true) {
                continue;
            }
            if (stage.AdvanceConditions[i].Type != type) {
                continue;
            }
            int requiredCount = 0;
            int requiredComplete = 0;
            int completeCount = 0;
            for (int j = 0; j < ConditionStates[i].ItemsSatisfied.Count; ++j) {
                bool currentRequired = GetRequired(i, j, type);
                if (currentRequired) {
                    ++requiredCount;
                }
                if (ConditionStates[i].ItemsSatisfied[j] == true) {
                    ++completeCount;
                    if (currentRequired) {
                        ++requiredComplete;
                    }
                    continue;
                }
                Point source = GetPoint(i, j, type);
                if (!(source.X == loc.X && source.Y == loc.Y)) {
                    continue;
                }
                ConditionStates[i].ItemsSatisfied[j] = true;
                ++completeCount;
                if (currentRequired) {
                    ++requiredComplete;
                }

                string[] initial = GetScript(i, j, type);
                if (!Sequence.GetScript(CurrentStageIndex, initial, out script, out error)) {
                    Log.Warn(error);
                    continue;
                }
                if (script is null) {
                    continue;
                }
                // npc name can be null, which is fine
                string npcName = GetNpcName(i, j, type);
                int count = ConditionStates[i].ItemsSatisfied.Count(i => i is true);
                script = script.Select(s => ReplaceTokens(s, source, npcName, count)).ToArray();
                streamId = StreamPrefix + $"_Condition{i}_Item{j}";
                if (!Streams.New(Game1.CurrentEvent, streamId, script, out error)) {
                    Log.Warn(error);
                }
            }
            if (requiredComplete < requiredCount) {
                continue;
            }
            int softTarget = stage.AdvanceConditions[i].GetSatisfiedCount();
            if (completeCount - requiredCount < softTarget) {
                continue;
            }

            Log.Debug($"Completing condition {i} ({type})");
            ConditionStates[i].Satisfied = true;
            if (!Sequence.GetScript(CurrentStageIndex,
                    Stage.AdvanceConditions[i].ScriptOnSatisfy?.ToArray(), out script, out error)) {
                Log.Warn(error);
                continue;
            }
            if (script is null) {
                continue;
            }

            string[] awaitCommand = new string[]{};
            // await any ongoing item scripts if needed
            List<string> openItems = Streams.OpenStreams.Keys.Where(key =>
                    key.StartsWithIgnoreCase(CurrentState.StreamPrefix) &&
                    key.ContainsIgnoreCase("_Item")).ToList();
            if (openItems.Count > 0) {
                awaitCommand = new string[]{$"{Main.ModId}_StreamAwait " +
                        string.Join(" ", openItems)};
            }
            script = awaitCommand.Concat(script).ToArray();
            streamId = CurrentState.StreamPrefix + $"_Condition{i}_Complete";
            if (!Streams.New(Game1.CurrentEvent, streamId, script, out error)) {
                Log.Warn(error);
            }
        }
    }

    internal static string ReplaceTokens(string cmd, Point source, string npcName, int count)
    {
        // lol don't confuse my token syntax with interpolated strings
        string ret = cmd
                .Replace("${x}", $"{source.X}", StringComparison.OrdinalIgnoreCase)
                .Replace("${y}", $"{source.Y}", StringComparison.OrdinalIgnoreCase)
                .Replace("${count}", $"{count}", StringComparison.OrdinalIgnoreCase);
        if (npcName is not null) {
            ret = ret.Replace("${npc}", npcName, StringComparison.OrdinalIgnoreCase);
        }
        return ret;
    }

    internal static void CheckTimePasses(GameTime time)
    {
        for (int i = 0; i < ConditionStates.Count; ++i) {
            if (ConditionStates[i].Satisfied == true) {
                continue;
            }
            if (Stage.AdvanceConditions[i].Type != ConditionType.TimePass) {
                continue;
            }
            ConditionStates[i].Milliseconds += time.ElapsedGameTime.Milliseconds;
            if (ConditionStates[i].Milliseconds >= Stage.AdvanceConditions[i].Duration) {
                Log.Debug($"Completing condition {i} (TimePass)");
                ConditionStates[i].Satisfied = true;
            }
        }
    }

    internal static HintCursorType CachedCursorValue = HintCursorType.None;
    internal static Point ProbedCursorTile = new();

    [TargetMethod(typeof(Game1), nameof(Game1.drawMouseCursor))]
    [PatchType(PatchTypes.Prefix)]
    internal static void Game1_drawMouseCursor_Prefix(Game1 __instance, ref CursorBucket __state)
    {
        if (!SequenceActive()) {
            return;
        }
        __state = new();
        if (!Game1.currentCursorTile.ToPoint().Equals(ProbedCursorTile)) {
            CachedCursorValue = ProbeForHintCursor(Game1.currentCursorTile.ToPoint());
            ProbedCursorTile = Game1.currentCursorTile.ToPoint();
        }
        if (CachedCursorValue == HintCursorType.None) {
            return;
        }
        __state.Action = Game1.isActionAtCurrentCursorTile;
        __state.Inspection = Game1.isInspectionAtCurrentCursorTile;
        __state.Speech = Game1.isSpeechAtCurrentCursorTile;
        Game1.isActionAtCurrentCursorTile = false;
        Game1.isInspectionAtCurrentCursorTile = false;
        Game1.isSpeechAtCurrentCursorTile = false;
        // we don't actually need to wrap Game1.mouseCursor, since running
        // drawMouseCursor ultimately resets it to cursor_default
        Game1.mouseCursor = (int)CachedCursorValue;
    }

    [TargetMethod(typeof(Game1), nameof(Game1.drawMouseCursor))]
    [PatchType(PatchTypes.Postfix)]
    internal static void Game1_drawMouseCursor_Postfix(Game1 __instance, ref CursorBucket __state)
    {
        if (!SequenceActive()) {
            return;
        }
        Game1.isActionAtCurrentCursorTile = __state.Action;
        Game1.isInspectionAtCurrentCursorTile = __state.Inspection;
        Game1.isSpeechAtCurrentCursorTile = __state.Speech;
    }

    internal static HintCursorType ProbeForHintCursor(Point loc)
    {
        for (int i = 0; i < ConditionStates.Count; ++i) {
            if (ConditionStates[i].Satisfied == true) {
                continue;
            }
            AdvanceCondition cond = Stage.AdvanceConditions[i];
            switch (cond.Type) {
            case ConditionType.NpcInteract:
                for (int j = 0; j < cond.Actors.Count; ++j) {
                    if (ConditionStates[i].ItemsSatisfied[j] == true) {
                        continue;
                    }
                    ActorPoint actor = cond.Actors[j];
                    NPC who = Game1.CurrentEvent?.getActorByName(actor.Name);
                    if (who is not null && who.TilePoint.X == loc.X && who.TilePoint.Y == loc.Y) {
                        return actor.HintCursor;
                    }
                }
                break;
            case ConditionType.TileTouch:
            case ConditionType.TileInteract:
            default:
                for (int j = 0; j < cond.Tiles.Count; ++j) {
                    if (ConditionStates[i].ItemsSatisfied[j]) {
                        continue;
                    }
                    TilePoint tile = cond.Tiles[j];
                    if (tile.X == loc.X && tile.Y == loc.Y) {
                        return tile.HintCursor;
                    }
                }
                break;
            }
        }
        return HintCursorType.None;
    }
}

internal enum SequenceState
{
    WaitNext,
    AwaitingScript,
    Running,
    Finished,
}

internal class ConditionState
{
    internal bool Satisfied = false;
    internal List<bool> ItemsSatisfied = new();
    internal int Milliseconds = 0;
}

internal class CursorBucket
{
    public bool Action = false;
    public bool Inspection = false;
    public bool Speech = false;
}

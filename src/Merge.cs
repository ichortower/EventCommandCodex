using StardewValley;
using StardewValley.Extensions;
using System.Collections.Generic;

using SEvent = StardewValley.Event;

using ichortower.TowerCore;

namespace ichortower.ECC;

internal class Merge
{
    internal static int GlobalMergeStreamIndex = 0;

    public static void command_Merge(SEvent evt, string[] args, EventContext context)
    {
        // snarf the command list. these are then put in streams and awaited.
        // FIXME? maybe check for known badness like nested merge or stream
        bool matched = false;
        int i = evt.CurrentCommand + 1;
        List<string> commands = new();
        for (; i < evt.eventCommands.Length; ++i) {
            if (evt.eventCommands[i].StartsWithIgnoreCase($"{Main.ModId}_MergeEnd")) {
                matched = true;
                break;
            }
            commands.Add(evt.eventCommands[i]);
        }
        evt.CurrentCommand = i;
        if (!matched) {
            context.LogErrorAndSkip("did not find a matching MergeEnd command");
            return;
        }
        List<string> autoIds = new();
        for (i = 0; i < commands.Count; ++i) {
            string id = $"ECC_Merge_{GlobalMergeStreamIndex}";
            if (!Streams.New(evt, id, new string[]{commands[i]})) {
                context.LogErrorAndSkip("could not create internal stream. This shouldn't happen");
                return;
            }
            autoIds.Add(id);
            ++GlobalMergeStreamIndex;
        }
        evt.InsertNextCommand($"{Main.ModId}_StreamAwait " + string.Join(" ", autoIds));
        ++evt.CurrentCommand;
    }

    public static void command_MergeEnd(SEvent evt, string[] args, EventContext context)
    {
        context.LogErrorAndSkip("this command was executed, which shouldn't" +
                " happen. Check your script and make sure every Merge is ended once");
    }
}

using StardewValley;

using Log = ichortower.TowerCore.Log;
using Main = ichortower.TowerCore.Main;
using SEvent = StardewValley.Event;

namespace ichortower.ECC;

internal class RepeatBlock
{

    public static void command_RepeatCount(SEvent evt, string[] args, EventContext context)
    {
        if (!ArgUtility.TryGetInt(args, 1, out int count, out string error)) {
            context.LogErrorAndSkip(error);
            return;
        }
        evt.SetRepeatAnchor(evt.CurrentCommand + 1);
        evt.SetRepeatCounter(count);
        ++evt.CurrentCommand;
    }

    public static void command_RepeatTime(SEvent evt, string[] args, EventContext context)
    {
        if (!ArgUtility.TryGetInt(args, 1, out int millis, out string error)) {
            context.LogErrorAndSkip(error);
            return;
        }
        evt.SetRepeatAnchor(evt.CurrentCommand + 1);
        evt.SetRepeatTimer(millis);
        ++evt.CurrentCommand;
    }

    public static void command_EndRepeat(SEvent evt, string[] args, EventContext context)
    {
        int targetIndex = evt.GetRepeatAnchor();
        if (targetIndex == -1) {
            //logerrorandskip
            return;
        }
        bool doneLooping = false;
        // this is a bit scuffed but it should sort of support both kinds of loop counter
        // at once (don't do that though)
        int timer = evt.GetRepeatTimer();
        if (timer > 0) {
            doneLooping = evt.CheckRepeatTimer(Game1.currentGameTime);
        }
        int counter = evt.GetRepeatCounter();
        if (!doneLooping && counter > 0) {
            doneLooping = evt.DecrementRepeatCounter();
        }

        if (doneLooping) {
            ++evt.CurrentCommand;
        }
        else {
            evt.CurrentCommand = targetIndex;
        }
    }

}

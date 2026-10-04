using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Extensions;
using System;
using System.Collections.Generic;

using Log = ichortower.TowerCore.Log;
using Main = ichortower.TowerCore.Main;
using SEvent = StardewValley.Event;

namespace ichortower.ECC;

internal class CG
{
    public static void command_CGShow(SEvent evt, string[] args, EventContext context)
    {
        // CGShow <texture> [sourceRect:x,y,w,h] [transition:dur,type] [scaling:type] [color:color] [letterbox:color] [wait]
        if (!ArgUtility.TryGet(args, 1, out string texture, out string error)) {
            context.LogErrorAndSkip(error);
            return;
        }
        Rectangle sourceRect = new(0, 0, 0, 0);
        Color drawColor = Color.White;
        CGScaling scaling = new();
        CGTransition transition = new();
        Color letterboxColor = Color.Transparent;
        bool waitForTransition = false;

        for (int i = 2; i < args.Length; ++i) {
            string arg = args[i];
            if (arg.EqualsIgnoreCase("wait")) {
                waitForTransition = true;
            }
            else if (arg.StartsWithIgnoreCase("color:")) {
                drawColor = Utility.StringToColor(arg[6..]) ?? Color.White;
            }
            else if (arg.StartsWithIgnoreCase("letterbox:")) {
                letterboxColor = Utility.StringToColor(arg[10..]) ?? Color.Transparent;
            }
            else if (arg.StartsWithIgnoreCase("scaling:")) {
                if (!ParseScalingArg(arg, ref scaling, out error)) {
                    Log.Warn(error);
                }
            }
            else if (arg.StartsWithIgnoreCase("transition:")) {
                if (!ParseTransitionArg(arg, ref transition, out error)) {
                    Log.Warn(error);
                }
            }
            else if (arg.StartsWithIgnoreCase("sourcerect:")) {
                if (!ParseSourceRectArg(arg, ref sourceRect, out error)) {
                    Log.Warn(error);
                }
            }
            else {
                Log.Warn($"Unknown argument '{arg}'");
            }
        }

        // add to list, start ticker, etc.
        CGItem item = new() {
            Texture = Game1.content.Load<Texture2D>(texture),
            Transition = transition,
            DrawColor = drawColor,
            LetterboxColor = letterboxColor,
            Scaling = scaling,
        };
        item.CalculateRects();
        ActiveCGs.Add(item);
        StartCGTicker();

        if (waitForTransition && transition.Duration > 0) {
            evt.InsertNextCommand($"pause {transition.Duration}");
        }
        ++evt.CurrentCommand;
    }

    public static void command_CGHide(SEvent evt, string[] args, EventContext context)
    {
        if (ActiveCGs.Count == 0) {
            context.LogErrorAndSkip($"Skipped command '{args[0]}': no active CGs left to hide");
            return;
        }
        int start = 2;
        if (args.Length < 2 || !ArgUtility.TryGetInt(args, 1, out int index, out _)) {
            index = ActiveCGs.FindLastIndex((item) => {
                return item.Transition.Direction != CGTransitionDirection.Hide;
            });
            if (index == -1) {
                context.LogErrorAndSkip($"Skipped command '{args[0]}': no active CGs left to hide");
                return;
            }
            start = 1;
        }
        CGTransition transition = ActiveCGs[index].Transition;
        bool waitForTransition = false;
        //ActiveCGs[index].Transition.Direction = CGTransitionDirection.Hide;
        transition.Direction = CGTransitionDirection.Hide;

        for (int i = start; i < args.Length; ++i) {
            string arg = args[i];
            if (arg.EqualsIgnoreCase("wait")) {
                waitForTransition = true;
            }
            else if (arg.StartsWithIgnoreCase("transition:")) {
                if (!ParseTransitionArg(arg, ref transition, out string error)) {
                    Log.Warn(error);
                    continue;
                }
                ActiveCGs[index].CalculateRects();
            }
            else {
                Log.Warn($"Unknown argument '{arg}'");
            }
        }

        ActiveCGs[index].Timer = 0;
        StartCGTicker();

        if (waitForTransition && transition.Duration > 0) {
            evt.InsertNextCommand($"pause {transition.Duration}");
        }
        ++evt.CurrentCommand;
    }


    private static bool ParseScalingArg(string arg, ref CGScaling scaling, out string error)
    {
        error = "";
        string[] pieces = arg[8..].Split(",");
        string val = pieces[0];
        if (val.EqualsIgnoreCase("fit")) {
            scaling.Type = CGScalingType.Fit;
        }
        else if (val.EqualsIgnoreCase("cover")) {
            scaling.Type = CGScalingType.Cover;
        }
        else if (val.EqualsIgnoreCase("stretch")) {
            scaling.Type = CGScalingType.Stretch;
        }
        else if (float.TryParse(val, out scaling.Scale)) {
            scaling.Type = CGScalingType.Absolute;
        }
        else {
            error = $"Argument '{arg}' could not be parsed: found value '{val}' but" +
                    " expected one of 'fit', 'cover', 'stretch', or a floating-point value.";
            return false;
        }
        if (pieces.Length > 1 && pieces[1].EqualsIgnoreCase("i")) {
            scaling.IntegerOnly = true;
        }
        return true;
    }

    private static bool ParseSourceRectArg(string arg, ref Rectangle sourceRect, out string error)
    {
        error = "";
        string[] pieces = arg[11..].Split(",");
        if (pieces.Length < 4) {
            error = $"Argument '{arg}' could not be parsed: expected 4 values" +
                    $" but got {pieces.Length}.";
            return false;
        }
        if (pieces.Length > 4) {
            Log.Warn($"Found more than 4 values in argument '{arg}': extra" +
                    " values will be discarded.");
        }
        if (!int.TryParse(pieces[0], out int x) ||
                !int.TryParse(pieces[1], out int y) ||
                !int.TryParse(pieces[2], out int w) ||
                !int.TryParse(pieces[3], out int h)) {
            error = $"Could not parse rectangle from '{arg[11..]}': could not" +
                    " convert to integers.";
            return false;
        }
        sourceRect = new(x, y, w, h);
        return true;
    }

    private static bool ParseTransitionArg(string arg, ref CGTransition transition, out string error)
    {
        error = "";
        string[] pieces = arg[11..].Split(",");
        if (pieces.Length < 2) {
            error = $"Argument '{arg}' could not be parsed: expected at least" +
                    " 'type,duration'.";
            return false;
        }
        if (pieces[0].EqualsIgnoreCase("zoom")) {
            if (!int.TryParse(pieces[1], out transition.Duration)) {
                error = $"Argument '{arg}' could not be parsed: expected integer" +
                        $" after '{pieces[0]}' but got '{pieces[1]}'.";
                return false;
            }
            transition.Type = CGTransitionType.Zoom;
            if (pieces.Length >= 4) {
                if (int.TryParse(pieces[2], out int fromX) &&
                        int.TryParse(pieces[3], out int fromY)) {
                    transition.X = fromX;
                    transition.Y = fromY;
                }
                else {
                    Log.Warn($"Found coordinate values in '{arg}' but could not convert" +
                            " them to integers.");
                }
            }
            return true;
        }
        if (pieces[0].EqualsIgnoreCase("fade")) {
            if (!int.TryParse(pieces[1], out transition.Duration)) {
                error = $"Argument '{arg}' could not be parsed: expected integer" +
                        $" after '{pieces[0]}' but got '{pieces[1]}'.";
                return false;
            }
            transition.Type = CGTransitionType.Fade;
            return true;
        }
        if (pieces[0].EqualsIgnoreCase("pan")) {
            if (pieces.Length < 4) {
                error = $"Argument '{arg}' could not be parsed: expected x, y values" +
                        $" for type '{pieces[0]}'.";
                return false;
            }
            if (!int.TryParse(pieces[1], out int duration)) {
                error = $"Argument '{arg}' could not be parsed: expected integer" +
                        $" after '{pieces[0]}' but got '{pieces[1]}'.";
                return false;
            }
            if (!int.TryParse(pieces[2], out int fromX) ||
                    !int.TryParse(pieces[3], out int fromY)) {
                error = $"Argument '{arg}' could not be parsed: expected integer values" +
                        $" for x and y parameters.";
                return false;
            }
            if (fromX == 0 && fromY == 0) {
                error = $"Argument '{arg}' not accepted: at least one of x, y must be nonzero.";
                return false;
            }
            transition.Type = CGTransitionType.Pan;
            transition.Duration = duration;
            transition.X = fromX;
            transition.Y = fromY;
            return true;
        }
        error = $"Unimplemented transition type '{pieces[0]}'.";
        return false;
    }

    // TODO bleh we need a window resize handler too

    internal static void StartCGTicker()
    {
        if (CGTransitionUpdate is not null) {
            return;
        }
        Log.Debug("Starting CG handlers");
        CGTransitionUpdate = TransitionHandler;
        CGDrawFunction = DrawHandler;
        Main.Helper.Events.GameLoop.UpdateTicked += CGTransitionUpdate;
        Main.Helper.Events.Display.RenderedWorld += CGDrawFunction;
    }

    internal static void StopCGTicker()
    {
        if (CGTransitionUpdate is null) {
            return;
        }
        Log.Debug("Stopping CG handlers");
        Main.Helper.Events.GameLoop.UpdateTicked -= CGTransitionUpdate;
        Main.Helper.Events.Display.RenderedWorld -= CGDrawFunction;
        CGTransitionUpdate = null;
        CGDrawFunction = null;
        ActiveCGs.Clear();
    }

    internal static void TransitionHandler(object sender, UpdateTickedEventArgs tickedArgs)
    {
        if (!ichortower.TowerCore.Game.IsActive()) {
            return;
        }
        if (Game1.eventOver || !Game1.eventUp) {
            StopCGTicker();
            return;
        }
        for (int i = 0; i < ActiveCGs.Count; ++i) {
            CGItem item = ActiveCGs[i];
            item.Timer = Math.Min(item.Timer + Game1.currentGameTime.ElapsedGameTime.Milliseconds,
                    item.Transition.Duration);
            if (item.Timer >= item.Transition.Duration &&
                    item.Transition.Direction == CGTransitionDirection.Hide) {
                ActiveCGs.RemoveAt(i);
                --i;
            }
        }

        if (ActiveCGs.Count == 0) {
            StopCGTicker();
        }
    }

    internal static void DrawHandler(object sender, RenderedWorldEventArgs renderedArgs)
    {
        SpriteBatch sb = renderedArgs.SpriteBatch;
        foreach (CGItem item in ActiveCGs) {
            Color drawColor = item.DrawColor;
            Color letterboxColor = item.LetterboxColor;
            float dist = (float)item.Timer / (float)item.Transition.Duration;
            // this 1 - dist inverts the direction of the lerp without having to swap
            // the arguments around
            if (item.Transition.Direction == CGTransitionDirection.Hide) {
                dist = 1f - dist;
            }
            // similarly, these other floats only alter the curve when doing a zoom
            float xydist = dist;
            float whdist = dist;
            if (item.Transition.Type == CGTransitionType.Zoom) {
                xydist = MathF.Sqrt(dist);
                whdist = dist * dist;
            }
            else if (item.Transition.Type == CGTransitionType.Fade) {
                drawColor *= dist;
                letterboxColor *= dist;
            }
            // remember this draw position is offset by width in order to (effectively)
            // "scale from center", which is key for the sqrt/square curves to look right
            Rectangle destRect = new(
                    (int)Utility.Lerp(item.AwayPosition.X, item.OutPosition.X, xydist),
                    (int)Utility.Lerp(item.AwayPosition.Y, item.OutPosition.Y, xydist),
                    (int)Utility.Lerp(item.AwayPosition.Width, item.OutPosition.Width, whdist),
                    (int)Utility.Lerp(item.AwayPosition.Height, item.OutPosition.Height, whdist));
            // letterbox should follow the cg so its width/height lerps are scaled by the
            // ratio of away size to displayed size
            Rectangle letterboxRect = new(
                    destRect.X, destRect.Y,
                    (int)Utility.Lerp(item.AwayPosition.Width * Game1.viewport.Width / item.OutPosition.Width,
                                      Game1.viewport.Width, whdist),
                    (int)Utility.Lerp(item.AwayPosition.Height * Game1.viewport.Height / item.OutPosition.Height,
                                      Game1.viewport.Height, whdist));
            destRect.X -= destRect.Width / 2;
            destRect.Y -= destRect.Height / 2;
            letterboxRect.X -= letterboxRect.Width / 2;
            letterboxRect.Y -= letterboxRect.Height / 2;

            sb.Draw(Game1.staminaRect, letterboxRect, null, letterboxColor);
            sb.Draw(item.Texture, destRect, null, drawColor);
        }
    }

    internal static System.EventHandler<UpdateTickedEventArgs> CGTransitionUpdate = null;
    internal static System.EventHandler<RenderedWorldEventArgs> CGDrawFunction = null;


    internal static List<CGItem> ActiveCGs = new();

}


internal class CGItem
{
    public Texture2D Texture = null;
    public CGTransition Transition = new();
    public CGScaling Scaling = new();
    public int Timer = 0;
    public Color DrawColor = Color.White;
    public Color LetterboxColor = Color.Transparent;
    public Rectangle OutPosition = new();
    public Rectangle AwayPosition = new();

    public void CalculateRects()
    {
        // first figure out the goal width/height
        float xFactor = (float)Game1.viewport.Width / (float)Texture.Width;
        float yFactor = (float)Game1.viewport.Height / (float)Texture.Height;
        if (Scaling.IntegerOnly) {
            xFactor = MathF.Floor(xFactor);
            yFactor = MathF.Floor(yFactor);
        }
        float matchFactor;
        switch (Scaling.Type) {
        case CGScalingType.Cover:
            matchFactor = MathF.Max(xFactor, yFactor);
            OutPosition.Width = (int)(matchFactor * Texture.Width);
            OutPosition.Height = (int)(matchFactor * Texture.Height);
            break;
        case CGScalingType.Stretch:
            OutPosition.Width = (int)(xFactor * Texture.Width);
            OutPosition.Height = (int)(yFactor * Texture.Height);
            break;
        case CGScalingType.Absolute:
            matchFactor = (Scaling.IntegerOnly ? MathF.Floor(Scaling.Scale) : Scaling.Scale);
            OutPosition.Width = (int)(matchFactor * Texture.Width);
            OutPosition.Height = (int)(matchFactor * Texture.Height);
            break;
        case CGScalingType.Fit:
        default:
            matchFactor = MathF.Min(xFactor, yFactor);
            OutPosition.Width = (int)(matchFactor * Texture.Width);
            OutPosition.Height = (int)(matchFactor * Texture.Height);
            break;
        }
        // goal x/y are always center, because we offset at draw time (scale from center)
        // to make zoom look right when querped
        OutPosition.X = Game1.viewport.Width / 2;
        OutPosition.Y = Game1.viewport.Height / 2;

        // now do away rect based on transition type
        switch (Transition.Type) {
            case CGTransitionType.Fade:
                AwayPosition = OutPosition;
                break;
            case CGTransitionType.Pan:
                AwayPosition = OutPosition;
                int factor = 1;
                if (Transition.X == 0) {
                    factor = Game1.viewport.Height / Math.Abs(Transition.Y) + 1;
                }
                else if (Transition.Y == 0) {
                    factor = Game1.viewport.Width / Math.Abs(Transition.X) + 1;
                }
                else {
                    factor = Math.Min(Game1.viewport.Height / Math.Abs(Transition.Y),
                                      Game1.viewport.Width / Math.Abs(Transition.X)) + 1;
                }
                AwayPosition.X += Transition.X * factor;
                AwayPosition.Y += Transition.Y * factor;
                break;
            case CGTransitionType.Zoom:
            default:
                AwayPosition.Width = 0;
                AwayPosition.Height = 0;
                if (Transition.X >= 0 && Transition.Y >= 0) {
                    AwayPosition.X = (32 + 64 * Transition.X) - Game1.viewport.X;
                    AwayPosition.Y = (32 + 64 * Transition.Y) - Game1.viewport.Y;
                }
                else {
                    AwayPosition.X = Game1.viewport.Width / 2;
                    AwayPosition.Y = Game1.viewport.Height / 2;
                }
                break;
        }
    }
}


internal class CGTransition
{
    public CGTransitionType Type = CGTransitionType.Zoom;
    public int X = -1;
    public int Y = -1;
    public int Duration = 0;
    public CGTransitionDirection Direction = CGTransitionDirection.Show;
}

internal enum CGTransitionType
{
    Zoom,
    Fade,
    Pan,
}

internal enum CGTransitionDirection
{
    Show,
    Hide,
}

internal class CGScaling
{
    public CGScalingType Type = CGScalingType.Fit;
    public float Scale = 0f;
    public bool IntegerOnly = false;
}

internal enum CGScalingType
{
    Fit,
    Cover,
    Stretch,
    Absolute,
}

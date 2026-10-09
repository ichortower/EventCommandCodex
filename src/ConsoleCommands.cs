using ichortower.TowerCore;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ichortower.ECC;

internal class ConsoleCommands
{

    internal const string SampleFolderName = "SampleEvents";

    #if DEBUG
    [ConsoleCommand("ast", "directly eval an event variable string to test AST")]
    public static void TestAst(string command, string[] args)
    {
        string input = string.Join(" ", args);
        Log.Debug(input);
        if (!ExprNode.EvalString(input, out string res, out string err)) {
            Log.Error(err);
            return;
        }
        Log.DebugWarn($"eval '{input}': result '{res}'");
    }
    #endif

    [ConsoleCommand("ecc_sample", "run a sample event from ECC's mod folder")]
    public static void RunSampleEvent(string command, string[] args)
    {
        if (Game1.gameMode != Game1.playingGameMode) {
            Log.Warn("Please load a save before running a sample event.");
            return;
        }
        string[] files;
        string dir = Path.Combine(Main.Helper.DirectoryPath, SampleFolderName);
        try {
            files = Directory.GetFiles(dir);
        }
        catch (DirectoryNotFoundException e) {
            Log.Warn(e.Message);
            return;
        }

        if (args.Length < 1 || args[0].EqualsIgnoreCase("list")) {
            if (files.Length == 0) {
                Log.Info("No sample events found.");
                return;
            }
            foreach (string path in files) {
                Log.Info(Path.GetFileName(path));
            }
            return;
        }

        if (files.Length == 0) {
            Log.Warn("No sample events found.");
            return;
        }

        string theOne = Utility.fuzzySearch(args[0], files);
        if (theOne is null) {
            Log.Warn($"No suitable match found for '{args[0]}'.");
            return;
        }

        Log.Info($"Loading sample event from '{Path.GetFileName(theOne)}'");
        List<string> lines = File.ReadAllLines(theOne)
                .Where(line => !line.StartsWith("--")).ToList();
        if (lines.Count < 5 || !lines[^1].StartsWithIgnoreCase("end")) {
            Log.Warn("At least 5 lines are required in a debug event script: " +
                    " location, music, camera, actors, and an 'end' command.");
            return;
        }
        string locationName = lines[0];
        lines.RemoveAt(0);
        string eventText = string.Join("/", lines);
        Log.Debug("sample event script: " + eventText);
        LocationRequest req = Game1.getLocationRequest(locationName);
        req.OnWarp += delegate {
            Game1.currentLocation.currentEvent = new Event(eventText);
            Game1.currentLocation.checkForEvents();
        };
        int x = 8;
        int y = 8;
        Utility.getDefaultWarpLocation(locationName, ref x, ref y);
        Game1.warpFarmer(req, x, y, Game1.player.FacingDirection);
    }

}

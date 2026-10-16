using ichortower.TowerCore;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ichortower.ECC;

internal class ConsoleCommands
{

    internal const string SampleFolderName = "SampleEvents";
    internal const string SampleJsonName = "PlayerControlData.json";
    internal static string SampleFolderPath => Path.Combine(Main.Helper.DirectoryPath, SampleFolderName);
    // this one is not absolute because ModContentHelper expects (forces) a relative path
    internal static string SampleSequenceFile => Path.Combine(SampleFolderName, SampleJsonName);

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
        try {
            files = Directory.GetFiles(SampleFolderPath).Where((path) => {
                return !Path.GetFileName(path).EqualsIgnoreCase(SampleJsonName);
            }).ToArray();
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

        // setting this flag here activates the sample data patch (see below)
        if (!SampleEventRequested) {
            Main.Helper.GameContent.InvalidateCache(PlayerControl.Data.AssetName);
            SampleEventRequested = true;
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

    internal static bool SampleEventRequested = false;

    /*
     * This is not a console command, but it patches in the appropriate
     * player control data from the sample folder so that the player control
     * sample event works when requested.
     * But this patch is only enabled once a sample event is requested.
     */
    [SmapiEvent]
    public static void AssetRequested(object sender, AssetRequestedEventArgs e)
    {
        if (!SampleEventRequested) {
            return;
        }
        if (e.Name.IsEquivalentTo(PlayerControl.Data.AssetName)) {
            try {
                var sampleData = Main.Helper.ModContent.Load
                        <Dictionary<string, PlayerControl.Sequence>>(SampleSequenceFile);
                e.Edit((asset) => {
                    var dict = asset.AsDictionary<string, PlayerControl.Sequence>();
                    foreach (var entry in sampleData) {
                        dict.Data[entry.Key] = entry.Value;
                    }
                });
            }
            catch (System.Exception err) {
                Log.Warn(err.ToString());
                return;
            }
        }
    }

}

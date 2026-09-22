using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Delegates;
using StardewValley.Internal;
using StardewValley.TokenizableStrings;
using StardewValley.Triggers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using ichortower.TowerCore;
using Main = ichortower.TowerCore.Main;

namespace ichortower.ECC;

internal sealed class ModMain : Mod
{
    public override void Entry(IModHelper helper)
    {
        Main.Init(this);

        Type[] types = Assembly.GetExecutingAssembly().GetTypes();
        foreach (Type t in types) {
            RegisterCommands(t);
        }
        TriggerActionManager.RegisterAction($"{Main.ModId}_WorldAdvanceTime",
                ichortower.ECC.World.traction_WorldAdvanceTime);
    }

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

    private static void RegisterCommands(Type t)
    {
        MethodInfo[] funcs = t.GetMethods(BindingFlags.Public | BindingFlags.Static);
        foreach (var func in funcs) {
            if (func.Name.StartsWith("command_")) {
                string key = func.Name.Replace("command_", $"{Main.ModId}_");
                StardewValley.Event.RegisterCommand(key,
                        (EventCommandDelegate) Delegate.CreateDelegate(
                        typeof(EventCommandDelegate), func));
                OtherNamesAttribute attr = func.GetCustomAttribute<OtherNamesAttribute>();
                if (attr is not null) {
                    Array.ForEach(attr.Aliases, (alias) => {
                        StardewValley.Event.RegisterCommandAlias($"{Main.ModId}_{alias}", key);
                    });
                }
            }
            else if (func.Name.StartsWith("gsq_")) {
                string key = func.Name.Replace("gsq_", $"{Main.ModId}_");
                GameStateQuery.Register(key,
                        (GameStateQueryDelegate) Delegate.CreateDelegate(
                        typeof(GameStateQueryDelegate), func));
            }
            else if (func.Name.StartsWith("token_")) {
                string key = func.Name.Replace("token_", $"{Main.ModId}_");
                TokenParser.RegisterParser(key,
                        (TokenParserDelegate) Delegate.CreateDelegate(
                        typeof(TokenParserDelegate), func));
            }
        }
    }
}

using ichortower.TowerCore;
using StardewModdingAPI.Events;
using StardewValley;
using System.Collections.Generic;

namespace ichortower.ECC.PlayerControl;

internal class Data
{
    internal static string AssetName = $"{Main.ModId}/PlayerControl/Data";

    private static Dictionary<string, Sequence> _data = null;
    internal static Dictionary<string, Sequence> Dict {
        get {
            _data ??= Game1.content.Load<Dictionary<string, Sequence>>(AssetName);
            return _data;
        }
    }

    [SmapiEvent]
    internal static void AssetRequested(object sender, AssetRequestedEventArgs e)
    {
        if (e.Name.IsEquivalentTo(AssetName)) {
            e.LoadFrom(() => new Dictionary<string, Sequence>(),
                    AssetLoadPriority.Exclusive);
        }
    }

    [SmapiEvent]
    internal static void AssetsInvalidated(object sender, AssetsInvalidatedEventArgs e)
    {
        foreach (var name in e.Names) {
            if (name.IsEquivalentTo(AssetName)) {
                _data = null;
            }
        }
    }
}

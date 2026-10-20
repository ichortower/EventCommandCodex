using System.Collections.Generic;
using System.Linq;

namespace ichortower.ECC.PlayerControl;

internal class AdvanceCondition
{
    public ConditionType Type = ConditionType.None;
    public bool Required = false;
    public List<TilePoint> Tiles = new();
    public List<ActorPoint> Actors = new();
    public int SatisfiedCount = -1;
    public int Duration = 0;
    public List<string> Script = new();

    internal int GetSatisfiedCount()
    {
        int ret = SatisfiedCount;
        if (ret == -1) {
            ret = Type switch {
                ConditionType.TileInteract or
                ConditionType.TileTouch => Tiles.Where(t => !t.Required).Count(),
                ConditionType.NpcInteract => Actors.Where(a => !a.Required).Count(),
                _ => 0,
            };
        }
        return ret;
    }
}

enum ConditionType {
    None,
    TileTouch,
    TileInteract,
    NpcInteract,
    TimePass,
}

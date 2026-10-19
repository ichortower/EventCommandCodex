using System.Collections.Generic;

namespace ichortower.ECC.PlayerControl;

internal class TilePoint
{
    public int X = -100;
    public int Y = -100;
    public List<string> Script = new();
    public bool Required = false;
    public HintCursorType HintCursor = HintCursorType.None;
}

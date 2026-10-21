using System.Collections.Generic;

namespace ichortower.ECC.PlayerControl;

internal class ActorPoint
{
    public string Name = "";
    public bool Required = false;
    public HintCursorType HintCursor = HintCursorType.None;
    public List<string> Script = new();
}

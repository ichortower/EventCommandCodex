using System.Collections.Generic;

namespace ichortower.ECC.PlayerControl;

internal class SequenceStage
{
    public string Id = null;
    public List<string> ScriptOnBegin = new();
    public List<string> ScriptOnAdvance = new();
    public Dictionary<string, List<string>> Scripts = new();
    public Dictionary<string, string> Dialogues = new();
    public List<AdvanceCondition> AdvanceConditions = new();
    public int SatisfiedCount = -1;
}

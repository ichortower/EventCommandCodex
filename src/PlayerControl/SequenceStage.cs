using System.Collections.Generic;

namespace ichortower.ECC.PlayerControl;

internal class SequenceStage
{
    public string Id = null;
    public List<string> ScriptOnBegin = null;
    public List<string> ScriptOnAdvance = null;
    public List<string> ScriptOnExpire = null;
    public Dictionary<string, List<string>> Scripts = new();
    public Dictionary<string, string> Dialogues = new();
    public List<AdvanceCondition> AdvanceConditions = new();
    public int SatisfiedCount = -1;
    public int TimeLimit = -1;
    public bool ShowTimer = false;
}

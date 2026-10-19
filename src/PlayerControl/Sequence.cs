using System.Collections.Generic;

namespace ichortower.ECC.PlayerControl;

internal class Sequence
{
    public List<SequenceStage> Stages = new();
    public Dictionary<string, List<string>> Scripts = new();

    public static bool Validate(Sequence item, out string error)
    {
        error = "";
        if (item is null) {
            error = "sequence is null";
            return false;
        }
        if (item.Stages is null || item.Stages.Count == 0) {
            error = "a valid sequence must have at least one stage";
            return false;
        }
        for (int i = 0; i < item.Stages.Count; ++i) {
            SequenceStage stage = item.Stages[i];
            if (stage.AdvanceConditions is null || stage.AdvanceConditions.Count == 0) {
                error = $"stage index {i} (id {stage.Id}) has no advance conditions." +
                        " every stage requires at least one";
                return false;
            }
        }
        return true;
    }

    public bool GetScript(int index, string[] source, out string[] script, out string error)
    {
        error = "";
        script = null;
        if (index < 0 || index >= Stages.Count) {
            error = $"Invalid index {index}: expected 0-{Stages.Count-1}";
            return false;
        }
        if (source is null || source.Length == 0) {
            error = $"Source argument was null or empty";
            return false;
        }
        bool outOfScope = false;
        HashSet<string> nodes = new();
        while (source?[0].StartsWith("@") is true) {
            string name = source[0][1..];
            if (!outOfScope && Stages[index].Scripts?.TryGetValue(name, out var list) is true) {
                if (!nodes.Add($"idx_{name}")) {
                    error = $"Script looped when trying to follow reference '{source[0]}'";
                    return false;
                }
                source = list.ToArray();
                continue;
            }
            outOfScope = true;
            if (Scripts?.TryGetValue(name, out list) is true) {
                if (!nodes.Add($"outer_{name}")) {
                    error = $"Script looped when trying to follow reference '{source[0]}'";
                    return false;
                }
                source = list.ToArray();
                continue;
            }
            error = $"Script tried to follow reference '{source[0]}', but it could not be found";
            return false;
        }
        if (source is null || source.Length == 0) {
            error = $"Script followed reference '{source[0]}', but the result was empty";
            return false;
        }
        script = source;
        return true;
    }
}

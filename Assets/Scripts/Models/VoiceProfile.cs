using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class VoiceLine
{
    /// <summary>
    /// Optional UI token to show when playing. Often unused in-game.
    /// </summary>
    public string Token = string.Empty;

    /// <summary>
    /// Audio clip path (e.g., sound/enemy/russian/soldier/there_he_is).
    /// </summary>
    public string Path = string.Empty;
}

[Serializable]
public class VoiceGroup<TCategory>
{
    public TCategory Category;
    public List<VoiceLine> Lines = new List<VoiceLine>();
}

/// <summary>
/// Voice grouping by design docs: Reports, Areas, Focus, Orders.
/// Each group holds multiple numbered variants per category (TargetAcquired_1..N, ...).
/// </summary>
public class VoiceProfile : ScriptableObject
{
    public List<VoiceGroup<VoiceReport>> Reports = new List<VoiceGroup<VoiceReport>>();
    public List<VoiceGroup<string>> Areas = new List<VoiceGroup<string>>();
    public List<VoiceGroup<string>> Focus = new List<VoiceGroup<string>>();
    public List<VoiceGroup<string>> Orders = new List<VoiceGroup<string>>();

    public bool TryGetRandomReport(VoiceReport report, out VoiceLine line)
    {
        for (int i = 0; i < Reports.Count; i++)
        {
            if (!EqualityComparer<VoiceReport>.Default.Equals(Reports[i].Category, report)) continue;
            List<VoiceLine> lines = Reports[i].Lines;
            if (lines == null || lines.Count == 0) break;
            line = lines[UnityEngine.Random.Range(0, lines.Count)];
            return true;
        }
        line = null;
        return false;
    }
}



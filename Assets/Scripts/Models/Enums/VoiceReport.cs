using System;
using System.Reflection;

/// <summary>
/// Reports are short callouts characters make in response to tactical events.
/// </summary>
public enum VoiceReport
{
    [Description("When a character sees an enemy")] TargetAcquired,

    [Description("When an enemy has been killed")] TargetEliminated,

    [Description("When the current focus of a character goes out of sight")] TargetLost,

    [Description("When a character sees a friend who has been killed")] ManDown,

    [Description("When a character has finished the current phase of a goal")] ObjectiveComplete,

    [Description("When commandos signal each other to advance")] PositionReached,

    [Description("When shot")] UnderFire,

    [Description("When throwing a grenade")] FireInTheHole,

    [Description("When a dead body is discovered or an ambush is detected")] SoundAlarm,

    [Description("When more than one character goes to examine a sound or footprint")] ExamineGroup,

    [Description("When only one character goes to examine")] ExamineAlone,

    [Description("When all suspected problems were looked at; ready to patrol again")] ExamineStop,

    [Description("When a character gets frustrated trying to get around someone else")] Blocked
}

/// <summary>
/// Extension method zum Abrufen der Description-Attribute.
/// </summary>
public static class VoiceReportExtensions
{
    public static string GetDescription(this VoiceReport rep)
    {
        var field = rep.GetType().GetField(rep.ToString());
        var attr = field.GetCustomAttribute<DescriptionAttribute>();
        return attr?.Text ?? rep.ToString();
    }
}




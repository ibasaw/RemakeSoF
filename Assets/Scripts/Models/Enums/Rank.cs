using System;
using System.Reflection;

public enum Rank
{
    [Description("Mostly used with the Tourist type")]
    Civilian,

    [Description("Mostly used with the Thug type")]
    Criminal,

    [Description(
        "Most common, pairs up into fire teams and squads, uses hand signals.\n" +
        "Privates should be spawned near each other so they can work together, communicate, " +
        "and give hand signals.")]
    Private,

    [Description("Runs a squad if scripted, otherwise fights with the team")]
    Sergeant
}

[AttributeUsage(AttributeTargets.Field)]
public class DescriptionAttribute : Attribute
{
    public string Text { get; }
    public DescriptionAttribute(string text) => Text = text;
}

public static class RankExtensions
{
    public static string GetDescription(this Rank rank)
    {
        var field = rank.GetType().GetField(rank.ToString());
        var attr = field.GetCustomAttribute<DescriptionAttribute>();
        return attr?.Text ?? rank.ToString();
    }
}

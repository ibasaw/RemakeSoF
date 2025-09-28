using System;
using System.Globalization;
using UnityEngine;

public static class NPCParsing
{
    public static bool TryParseVector3(string value, out Vector3 result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value)) return false;

        string[] parts = value.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3) return false;

        if (!TryParseFloat(parts[0], out float x)) return false;
        if (!TryParseFloat(parts[1], out float y)) return false;
        if (!TryParseFloat(parts[2], out float z)) return false;

        result = new Vector3(x, y, z);
        return true;
    }

    public static bool TryParseBoundsRange(string min, string max, out BoundsRange range)
    {
        range = default;
        if (!TryParseVector3(min, out Vector3 vMin)) return false;
        if (!TryParseVector3(max, out Vector3 vMax)) return false;
        range = BoundsRange.FromMinMax(vMin, vMax);
        return range.IsValid();
    }

    private static bool TryParseFloat(string value, out float result)
    {
        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
    }
}



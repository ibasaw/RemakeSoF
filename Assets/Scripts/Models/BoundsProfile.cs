using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct BoundsRange
{
    public Vector3 Min;
    public Vector3 Max;

    public bool IsValid()
    {
        return Min.x <= Max.x && Min.y <= Max.y && Min.z <= Max.z;
    }

    public Bounds ToBounds()
    {
        Vector3 size = Max - Min;
        Vector3 center = Min + size * 0.5f;
        return new Bounds(center, size);
    }

    public static BoundsRange FromMinMax(Vector3 min, Vector3 max)
    {
        return new BoundsRange { Min = min, Max = max };
    }
}

[Serializable]
public class StanceBounds
{
    public Stance Stance;
    public BoundsRange Range;
}

public class BoundsProfile : ScriptableObject
{
    public List<StanceBounds> BoundsByStance = new List<StanceBounds>();

    public bool TryGetRange(Stance stance, out BoundsRange range)
    {
        for (int i = 0; i < BoundsByStance.Count; i++)
        {
            if (BoundsByStance[i].Stance == stance)
            {
                range = BoundsByStance[i].Range;
                return true;
            }
        }
        range = default;
        return false;
    }
}



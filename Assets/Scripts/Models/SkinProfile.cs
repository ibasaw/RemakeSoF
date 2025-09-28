using System;
using UnityEngine;

/// <summary>
/// Scriptable skin entry with its own inventory, matching SoF2 skin definitions.
/// </summary>
public class SkinProfile : ScriptableObject
{
    /// <summary>
    /// Skin file key (e.g., "hk_ped_a1").
    /// for the underlying .g2skin file
    /// </summary>
    public string File = string.Empty;

    /// <summary>
    /// Optional per-skin inventory.
    /// </summary>
    public Inventory Inventory;
}



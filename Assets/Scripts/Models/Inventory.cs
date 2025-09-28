using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Inventory data for NPCs. Supports global inventory (on template) and
/// skin-specific inventory (on a skin). Merge skin over global at spawn.
/// </summary>
[Serializable]
public class Inventory
{
    /// <summary>
    /// Weapons assigned to the NPC.
    /// </summary>
    public List<WeaponEntry> Weapons = new List<WeaponEntry>();

    /// <summary>
    /// Items, bolt-ons, or surface toggles assigned to the NPC.
    /// </summary>
    public List<ItemEntry> Items = new List<ItemEntry>();

    /// <summary>
    /// Returns a new Inventory that contains entries from <paramref name="global"/>
    /// combined with <paramref name="skin"/> (skin entries appended after global).
    /// </summary>
    public static Inventory Merge(Inventory global, Inventory skin)
    {
        var merged = new Inventory();
        if (global != null)
        {
            if (global.Weapons != null) merged.Weapons.AddRange(global.Weapons);
            if (global.Items != null) merged.Items.AddRange(global.Items);
        }
        if (skin != null)
        {
            if (skin.Weapons != null) merged.Weapons.AddRange(skin.Weapons);
            if (skin.Items != null) merged.Items.AddRange(skin.Items);
        }
        return merged;
    }
}

/// <summary>
/// Weapon entry with optional holster/bolt information for secondary weapons.
/// Most weapon names match player weapon identifiers.
/// </summary>
[Serializable]
public class WeaponEntry
{
    /// <summary>
    /// Weapon name (e.g., "M4", "MSG90A1").
    /// </summary>
    public string Name = string.Empty;

    /// <summary>
    /// Bolt/tag to attach when equipped (e.g., "*hand_r", "*hip_r"). Optional.
    /// </summary>
    public string Bolt = string.Empty;

    /// <summary>
    /// If true, weapon can be holstered (e.g., pistols, grenades).
    /// </summary>
    public bool Secondary = false;

    /// <summary>
    /// Bolt/tag to use when holstered (e.g., pistol holster).
    /// </summary>
    public string HolsterBolt = string.Empty;

    /// <summary>
    /// Optional chance percentage as string (e.g., "70").
    /// </summary>
    public string Chance = string.Empty;
}

/// <summary>
/// Item entry for togglable surfaces (hood/backpack) or bolt-on models.
/// </summary>
[Serializable]
public class ItemEntry
{
    /// <summary>
    /// Item name/identifier (e.g., "backpack", "helmet_military", or surface name).
    /// </summary>
    public string Name = string.Empty;

    /// <summary>
    /// Bolt/tag to attach the item model to (if model-based).
    /// </summary>
    public string Bolt = string.Empty;

    /// <summary>
    /// Optional model path, if this item is a model rather than a pure surface toggle.
    /// </summary>
    public string Model = string.Empty;

    /// <summary>
    /// If true, the surface/model starts enabled (visible) at spawn time.
    /// </summary>
    public bool Enabled = true;

    /// <summary>
    /// Optional chance percentage as string (e.g., "50").
    /// </summary>
    public string Chance = string.Empty;

    /// <summary>
    /// Optional flag used by MP to mark on-back placement ("YES"/"NO").
    /// </summary>
    public string mp_onback = string.Empty;
}



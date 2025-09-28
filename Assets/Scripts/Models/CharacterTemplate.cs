using System;
using System.Collections.Generic;
using UnityEngine;

// ALL -1 default values are inherited by parent
//[CreateAssetMenu(fileName = "CharacterTemplate", menuName = "SoF2/Templates/CharacterTemplate")]
public class CharacterTemplate //: ScriptableObject
{

    //[Header("Hierarchy (GroupInfo)")]
    //[Tooltip("Parent template: Werte werden immer vererbt(akumuliert).")]
    public CharacterTemplate Parent;

    /// <summary>
    /// Unique internal character name. Default: string.Empty
    /// </summary>
    public string Name = string.Empty;

    /// <summary>
    /// Display name. Default: string.Empty
    /// </summary>
    public string FormalName = string.Empty;

    /// <summary>
    /// Free-form comments. Default: string.Empty
    /// </summary>
    public string comments = string.Empty;

    /// <summary>
    /// GLM model path. Default: string.Empty.
    /// Determines which model file the character uses.
    /// </summary>
    public string Model = string.Empty;

    /// <summary>
    /// GLA skeleton definition file. Default: string.Empty.
    /// Determines which skeleton file the character uses.
    //public string Skeleton = string.Empty;

    /// <summary>
    /// Health as range string "min max". Default: -1 -1
    /// </summary>
    public Vector2Int Health = new(-1, -1);

    /// <summary>
    /// Deathmatch availability ("yes"/"no"). 
    /// Means if player is available in Multiplayer or not.
    /// not set means yes. Default: string.Empty
    /// </summary>
    public string Deathmatch = string.Empty;

    /// <summary>
    /// This is the location on the model where
    /// the primary weapons will be held. Default: "*hand_r".
    /// </summary>
    public string DefaultReady = "*hand_r";

    /// <summary>
    /// Aggregated NPC stats container.
    /// </summary>
    public NPCStats Stats = new();

    /// <summary>
    /// Global inventory block (weapons and items).
    /// Global inventory is also inherited so global inventory in a parent
    /// template will be given to all guys in all derived groups no matter what skin they select.
    ///  There are two places, global inventory that will be given to
    /// all characters with that template, and skin specific inventory given to any guy who
    /// spawns with that skin. 
    /// Global inventory is cumulatively inherited. That means that a character derived from
    ///“Marine_Soldier” who ALSO defines his own global inventory set will just add to the
    ///items in a cumulative fashion and not replace the parent’s group. There is no
    ///redundancy checking either, so it is important to be careful with global inventory because
    ///it is added to all the characters and all the skins within the characters.
    /// </summary>
    public Inventory Inventory;

    /// <summary>
    /// at least one for the base parent have to be provided.
    /// One or more skins. Uses ScriptableObject profiles so multiple NPCs can share.
    /// Defaults to an empty list; assign profiles via Inspector.
    /// </summary>
    public List<SkinProfile> Skins = new();

    /// <summary>
    /// Shared bounds profile for various stances and states.
    /// Use a ScriptableObject so multiple NPCs can share the same bounds.
    /// </summary>
    public BoundsProfile BoundsProfile;

    /// <summary>
    /// Voice profile (Reports, Areas, Focus, Orders)
    /// </summary>
    public VoiceProfile Voice;
}
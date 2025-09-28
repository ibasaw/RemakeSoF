using System;

[Serializable]
public class NPCStats
{

    /// <summary>
    /// This determines the rank of the character.
    /// Default: Rank.Civilian
    /// </summary>
    public Rank Rank = Rank.Civilian;

    /// <summary>
    /// Occupation string. Default: "ScriptGuy".
    /// </summary>
    public Occupation Occupation = Occupation.ScriptGuy;

    /// <summary>
    /// Team affinity string. Default: string.Empty. This is which team the character will associate with.
    /// </summary>
    public string Team = string.Empty;

    /// <summary>
    /// Determines if the character will always use 3-round burst mode
    /// when firing an automatic weapon. Commonly used with weapons
    /// like the auto shotgun (USAS-12) and the micro uzi.
    /// Default: false.
    /// </summary>
    public bool Burst = false;

    /// <summary>
    /// Represents the bravery level of the character.
    /// This value can influence the character's behavior in risky or combat situations.
    /// Default: 1.0f.
    /// </summary>
    public float Bravery = 1.0f;

    /// <summary>
    /// Represents the agility level of the character.
    /// This value can influence movement speed, reaction time, or other agility-related behaviors.
    /// Default: 1.0f.
    /// </summary>
    public float Agility = 1.0f;

    /// <summary>
    /// Represents the character's shooting accuracy as a percentage.
    /// A value of 0.0 means no shots will hit, while 1.0 means all shots will hit.
    /// For example, an accuracy of 0.50 means about half the shots will hit a target at 1000 units away.
    /// Default: -1.0f (uninitialized state).
    /// </summary>
    public float Accuracy = -1.0f;

    /// <summary>
    /// This value is a multiplier applied to the damage dealt by the character.
    /// For example, if the player would normally deal 60 points of damage with a weapon
    /// and this character has a DamageScale of 0.5, the character would deal 30 points of damage.
    /// Default: -1.0f (uninitialized state).
    /// </summary>
    public float DamageScale = -1.0f;

    /// <summary>
    /// Set to 1 if this character is a boss abilities (ignore head shots, less pain animations, things like that).
    /// Default: 0.
    /// </summary>
    public int Boss = 0;

    /// <summary>
    /// Damage scale on Easy difficulty. Default: "0.040".
    /// </summary>
    public float DamageEasy = 0.040f;

    /// <summary>
    /// Damage scale on Medium difficulty. Default: "0.055".
    /// </summary>
    public float DamageMedium = 0.055f;

    /// <summary>
    /// Damage scale on Hard difficulty. Default: "0.075".
    /// </summary>
    public float DamageHard = 0.075f;

    /// <summary>
    /// Damage scale on SuperHard/Nightmare difficulty. Default: "0.085".
    /// </summary>
    public float DamageSuperHard = 0.085f;
}



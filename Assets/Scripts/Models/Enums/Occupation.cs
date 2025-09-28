using System;
using System.Reflection;

/// <summary>
/// Occupations, mit Beschreibungen basierend auf deinem SoF2-Dokument.
/// Dog und Osprey sind als Spezialfälle mit jeweils kurzer Notiz angelegt.
/// </summary>
public enum Occupation
{
    [Description(@"Assassins are quite different from soldiers. They are much more cautious about revealing themselves in the open and will back away if you are aiming at them. They can roll grenades and work well where nav points don't connect the grid.")]
    Assassin,

    [Description(@"Commandos work in pairs, advancing slowly from one cover point to the next. They use hand signals and report to each other when they have reached their next cover point. One provides cover while the other advances; slower and more cautious than normal soldiers.")]
    Commando,

    [Description(@"Demolitionists are similar to soldiers but are much more likely to throw grenades and (like assassins) can roll grenades. They are effective grenade users and often heavily armored or equipped with powerful weapons.")]
    Demolitionist,

    [Description(@"Emplaced Gunners don't patrol. They are placed close to an emplaced gun and will use it when possible. They usually carry only a pistol or grenade and will still perform normal combat behavior when needed.")]
    EmplacedGunner,

    [Description(@"Look Outs walk a much smaller patrol radius (≈100 units). They do not attempt to get to a target they are aware of but cannot see. Good for towers, walls, or positions where NPCs should stay put.")]
    LookOut,

    [Description(@"Scouts behave like soldiers but will try to pair up with other scouts and go first in scripted squad moves. Usually lightly armored and often carry communications gear.")]
    Scout,

    [Description(@"Script Guys are used only for cinematic sequences and wait for commands from ICARUS before doing anything.")]
    ScriptGuy,

    [Description(@"Snipers are hyper-accurate and armed with sniper rifles. They pause between shots and do not chase targets, patrol, investigate sounds, or throw grenades. They stand where spawned and wait for targets to enter their sight.")]
    Sniper,

    [Description(@"Soldiers patrol a large radius (≈1000 units). When idle they may pull out a cigarette. They investigate sounds, prints, blood, and dead bodies; run toward combat sounds and stop to shoot when they see a target; use cover when reloading or under grenades; use emplaced weapons if within ~200 units; jump/vault/ladder; lean around corners; pick up weapons and grenades; and will run away if unarmed and cannot find a weapon.")]
    Soldier,

    [Description(@"Soldier Elite is almost the same as Soldier but more aggressive in choosing cover and can run a bit faster. Soldier and Soldier Elite are the only types that can use lean points.")]
    SoldierElite,

    [Description(@"Soldier Cover behaves like a Soldier but its initial reaction to seeing an enemy is to first run for cover.")]
    SoldierCover,

    [Description(@"Tourists are usually civilians. By default they have no weapons and are not on a team. If shot, they may pick up nearby weapons and fight back. By default they 'Wander Area' and may interact with use-entities.")]
    Tourist,

    [Description(@"Thugs share many behaviors with Soldiers but do not stop to stand and fire — they run as close as possible to a target while firing. Typically armed with close-range weapons (shotguns) and low health; often fodder but dangerous when charging.")]
    Thug,

    [Description(@"Dog: completely different behavior set from humans. Runs toward a target and bites if able. When idle, they may sleep or mark/pee. (Special case)")]
    Dog,

    [Description(@"Osprey: deadly boss character. Does not use nav grid but uses flocking/seek/avoid behavior. Hovers and strafes, spawns troops to rappel, and when wounded uses high-speed strafe attacks and missiles. (Special case/boss)")]
    Osprey
}

/// <summary>
/// Extension method zum Abrufen der Description-Attribute.
/// </summary>
public static class OccupationExtensions
{
    public static string GetDescription(this Occupation occ)
    {
        var field = occ.GetType().GetField(occ.ToString());
        var attr = field.GetCustomAttribute<DescriptionAttribute>();
        return attr?.Text ?? occ.ToString();
    }
}

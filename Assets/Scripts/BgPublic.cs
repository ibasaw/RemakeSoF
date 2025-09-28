// Auto-converted and simplified Unity/C# version of bg_public.h
// This file maps the most commonly used constants, enums and simple structs
// from the original C header into C# types suitable for Unity.
// It is intended as a starting point; some complex types and functions are
// kept as placeholders and need to be adapted to your game's architecture.

using System;
using UnityEngine;

namespace RemakeSoF.BG.Constants
{
    /// <summary>
    /// Shared constants from the original bg_public.h header.
    /// </summary>
    public static class BgConstants
    {
        public const int DEFAULT_GRAVITY = 800;
        public const float ARMOR_PROTECTION = 0.55f;

        public const int MAX_ITEMS = 256;
        public const int MAX_HEALTH = 100;
        public const int MAX_ARMOR = 100;

        public const int BULLET_SPACING = 95;

        public const int DISMEMBER_HEALTH = -20;

        public const int RANK_TIED_FLAG = 0x4000;

        public const int ITEM_RADIUS = 15;// item sizes are needed for client side pickup detection

        public const int SCORE_NOT_PRESENT = -9999; // for the CS_SCORES[12] when only one player is present

        public const int DEFAULT_PLAYER_Z_MAX = 43;
        public const int CROUCH_PLAYER_Z_MAX = 18;
        public const int PRONE_PLAYER_Z_MAX = -12;
        public const int DEAD_PLAYER_Z_MAX = -30;

        public const float DUCK_ACCURACY_MODIFIER = 0.75f;
        public const float JUMP_ACCURACY_MODIFIER = 2.0f;

        public const int MINS_Z = -46;

        public const int DEFAULT_VIEWHEIGHT = 37;
        public const int CROUCH_VIEWHEIGHT = 8;
        public const int PRONE_VIEWHEIGHT = -22;
        public const int DEAD_VIEWHEIGHT = -38;

        public const int BODY_SINK_DELAY = 10000;
        public const int BODY_SINK_TIME = 1500;

        public const int LEAN_TIME = 250;
        public const int LEAN_OFFSET = 30;

        public const int MAX_ITEM_MODELS = 4;

        // You can extend this file with more constants as needed.

        // pmove->pm_flags (PMF_*)
        public const int PMF_DUCKED = 0x00000001;
        public const int PMF_BACKWARDS_JUMP = 0x00000002;
        public const int PMF_JUMPING = 0x00000004;
        public const int PMF_BACKWARDS_RUN = 0x00000008;
        public const int PMF_TIME_LAND = 0x00000010;
        public const int PMF_TIME_KNOCKBACK = 0x00000020;
        public const int PMF_TIME_WATERJUMP = 0x00000040;
        public const int PMF_RESPAWNED = 0x00000080;
        public const int PMF_CAN_USE = 0x00000100;
        public const int PMF_FOLLOW = 0x00000200;
        public const int PMF_SCOREBOARD = 0x00000400;
        public const int PMF_GHOST = 0x00000800;
        public const int PMF_LADDER = 0x00001000;
        public const int PMF_LADDER_JUMP = 0x00002000;
        public const int PMF_ZOOMED = 0x00004000;
        public const int PMF_ZOOM_LOCKED = 0x00008000;
        public const int PMF_ZOOM_REZOOM = 0x00010000;
        public const int PMF_ZOOM_DEFER_RELOAD = 0x00020000;
        public const int PMF_LIMITED_INVENTORY = 0x00040000;
        public const int PMF_CROUCH_JUMP = 0x00080000;
        public const int PMF_GOGGLES_ON = 0x00100000;
        public const int PMF_LEANING = 0x00200000;
        public const int PMF_AUTORELOAD = 0x00400000;
        public const int PMF_SIAMESETWINS = 0x00800000;
        public const int PMF_FOLLOWFIRST = 0x01000000;

        // Combined PMF masks
        public const int PMF_ALL_TIMES = (PMF_TIME_WATERJUMP | PMF_TIME_LAND | PMF_TIME_KNOCKBACK);
        public const int PMF_ZOOM_FLAGS = (PMF_ZOOMED | PMF_ZOOM_LOCKED | PMF_ZOOM_REZOOM | PMF_ZOOM_DEFER_RELOAD);

        // pmove->pm_debounce (PMD_*)
        public const int PMD_JUMP = 0x0001;
        public const int PMD_ATTACK = 0x0002;
        public const int PMD_FIREMODE = 0x0004;
        public const int PMD_USE = 0x0008;
        public const int PMD_ALTATTACK = 0x0010;
        public const int PMD_GOGGLES = 0x0020;

        // SETANIM_* and flags
        public const int SETANIM_TORSO = 1;
        public const int SETANIM_LEGS = 2;
        public const int SETANIM_BOTH = SETANIM_TORSO | SETANIM_LEGS; // 3

        public const int SETANIM_FLAG_NORMAL = 0;      // Only set if timer is 0
        public const int SETANIM_FLAG_OVERRIDE = 1;    // Override previous
        public const int SETANIM_FLAG_HOLD = 2;        // Set the new timer
        public const int SETANIM_FLAG_RESTART = 4;     // Allow restarting the anim if playing the same one
        public const int SETANIM_FLAG_HOLDLESS = 8;    // Set the new timer without hold

        // entityState_t->eFlags (EF_*)
        public const int EF_DEAD = 0x00000001;
        public const int EF_EXPLODE = 0x00000002;
        public const int EF_TELEPORT_BIT = 0x00000004;
        public const int EF_PLAYER_EVENT = 0x00000008;
        public const int EF_BOUNCE = 0x00000008;
        public const int EF_BOUNCE_HALF = 0x00000010;
        public const int EF_BOUNCE_SCALE = 0x00000020;
        public const int EF_NODRAW = 0x00000040;
        public const int EF_FIRING = 0x00000080;
        public const int EF_ALT_FIRING = 0x00000100;
        public const int EF_MOVER_STOP = 0x00000200;
        public const int EF_TALK = 0x00000400;
        public const int EF_CONNECTION = 0x00000800;
        public const int EF_VOTED = 0x00001000;
        public const int EF_ANGLE_OVERRIDE = 0x00002000;
        public const int EF_PERMANENT = 0x00004000;
        public const int EF_NOPICKUP = 0x00008000;
        public const int EF_NOSHADOW = 0x00008000;
        public const int EF_REDTEAM = 0x00010000;
        public const int EF_BLUETEAM = 0x00020000;
        public const int EF_INSKY = 0x00040000;
        public const int EF_GOGGLES = 0x00080000;
        public const int EF_DUCKED = 0x00100000;
        public const int EF_INVULNERABLE = 0x00200000;

        // Reward/playerevent flags (stored in ps->persistant[PERS_PLAYEREVENTS])
        public const int PLAYEREVENT_DENIEDREWARD = 0x0001;
        public const int PLAYEREVENT_GAUNTLETREWARD = 0x0002;
        public const int PLAYEREVENT_HOLYSHIT = 0x0004;
    }

    // Simple enums converted to C# naming conventions
    public enum Gender
    {
        Male,
        Female,
        Neuter
    }

    public enum PmType
    {
        Normal, // can accelerate and turn
        Noclip,// noclip movement
        Spectator,// still run into walls
        Dead,// no acceleration or turning, but free falling
        Freeze,// stuck in place with no control
        Intermission// no movement or status bar
    }

    public enum WeaponState
    {
        Ready,
        Spawning,
        Raising,
        Dropping,
        Reloading,
        ReloadingAlt,
        Firing,
        FiringAlt,
        Charging,
        ChargingAlt,
        ZoomIn,
        ZoomOut
    }

    // Animation numbers (converted from animNumber_t)
    public enum AnimNumber
    {
        BOTH_DEATH_NORMAL,

        ANIM_START_DEATHS,

        BOTH_DEATH_NECK,
        BOTH_DEATH_CHEST_1,
        BOTH_DEATH_CHEST_2,
        BOTH_DEATH_GROIN_1,
        BOTH_DEATH_GROIN_2,
        BOTH_DEATH_GUT_1,
        BOTH_DEATH_GUT_2,
        BOTH_DEATH_HEAD_1,
        BOTH_DEATH_HEAD_2,
        BOTH_DEATH_SHOULDER_LEFT_1,
        BOTH_DEATH_SHOULDER_LEFT_2,
        BOTH_DEATH_ARMS_LEFT_1,
        BOTH_DEATH_ARMS_LEFT_2,
        BOTH_DEATH_LEGS_LEFT_1,
        BOTH_DEATH_LEGS_LEFT_2,
        BOTH_DEATH_LEGS_LEFT_3,
        BOTH_DEATH_THIGH_LEFT_1,
        BOTH_DEATH_THIGH_LEFT_2,
        BOTH_DEATH_ARMS_RIGHT_1,
        BOTH_DEATH_ARMS_RIGHT_2,
        BOTH_DEATH_LEGS_RIGHT_1,
        BOTH_DEATH_LEGS_RIGHT_2,
        BOTH_DEATH_LEGS_RIGHT_3,
        BOTH_DEATH_SHOULDER_RIGHT_1,
        BOTH_DEATH_SHOULDER_RIGHT_2,
        BOTH_DEATH_THIGH_RIGHT_1,
        BOTH_DEATH_THIGH_RIGHT_2,

        ANIM_END_DEATHS,

        TORSO_DROP,
        TORSO_DROP_ONEHANDED,
        TORSO_DROP_KNIFE,
        TORSO_RAISE,
        TORSO_RAISE_ONEHANDED,
        TORSO_RAISE_KNIFE,

        LEGS_IDLE,
        LEGS_IDLE_CROUCH,
        LEGS_WALK,
        LEGS_WALK_BACK,
        LEGS_WALK_CROUCH,
        LEGS_WALK_CROUCH_BACK,

        LEGS_RUN,
        LEGS_RUN_BACK,

        LEGS_SWIM,

        LEGS_JUMP,
        LEGS_JUMP_BACK,

        LEGS_TURN,

        LEGS_LEAN_LEFT,
        LEGS_LEAN_RIGHT,
        LEGS_LEAN_CROUCH_LEFT,
        LEGS_LEAN_CROUCH_RIGHT,

        LEGS_LEANLEFT_WALKLEFT,
        LEGS_LEANLEFT_WALKRIGHT,
        LEGS_LEANRIGHT_WALKLEFT,
        LEGS_LEANRIGHT_WALKRIGHT,

        LEGS_LEANLEFT_CROUCH_WALKLEFT,
        LEGS_LEANLEFT_CROUCH_WALKRIGHT,
        LEGS_LEANRIGHT_CROUCH_WALKLEFT,
        LEGS_LEANRIGHT_CROUCH_WALKRIGHT,

        TORSO_IDLE_KNIFE,
        TORSO_IDLE_PISTOL,
        TORSO_IDLE_RIFLE,
        TORSO_IDLE_MSG90A1,
        TORSO_IDLE_MSG90A1_ZOOMED,
        TORSO_IDLE_M4,
        TORSO_IDLE_M590,
        TORSO_IDLE_USAS12,
        TORSO_IDLE_RPG,
        TORSO_IDLE_M60,
        TORSO_IDLE_MM1,
        TORSO_IDLE_GRENADE,

        TORSO_ATTACK_KNIFE,
        TORSO_ATTACK_KNIFE_THROW,
        TORSO_ATTACK_PISTOL,
        TORSO_ATTACK_RIFLE,
        TORSO_ATTACK_MSG90A1_ZOOMED,
        TORSO_ATTACK_M4,
        TORSO_ATTACK_M590,
        TORSO_ATTACK_USAS12,
        TORSO_ATTACK_RIFLEBUTT,
        TORSO_ATTACK_RPG,
        TORSO_ATTACK_M60,
        TORSO_ATTACK_MM1,
        TORSO_ATTACK_GRENADE_START,
        TORSO_ATTACK_GRENADE_END,
        TORSO_ATTACK_BAYONET,
        TORSO_ATTACK_PISTOLWHIP,

        TORSO_RELOAD_M60,
        TORSO_RELOAD_PISTOL,
        TORSO_RELOAD_RIFLE,
        TORSO_RELOAD_MSG90A1,
        TORSO_RELOAD_RPG,
        TORSO_RELOAD_USAS12,

        TORSO_RELOAD_M590_START,
        TORSO_RELOAD_M590_SHELL,
        TORSO_RELOAD_M590_END,

        TORSO_RELOAD_MM1_START,
        TORSO_RELOAD_MM1_SHELL,
        TORSO_RELOAD_MM1_END,

        TORSO_USE,

        MAX_ANIMATIONS
    }

    // player_state->stats[] indexes
    public enum StatIndex
    {
        STAT_HEALTH,
        STAT_WEAPONS,                   // 16 bit fields
        STAT_ARMOR,
        STAT_DEAD_YAW,                  // look this direction when dead (FIXME: get rid of?)
        STAT_CLIENTS_READY,             // bit mask of clients wishing to exit the intermission (FIXME: configstring?)
        STAT_FROZEN,
        STAT_GOGGLES,                   // Which visual enhancing device they have
        STAT_GAMETYPE_ITEMS,            // Which gametype items they have	
        STAT_SEED,                      // seed used to keep weapon firing in sync
        STAT_OUTFIT_GRENADE,            // indicates which greande is chosen in the outfitting
        STAT_USEICON,                   // icon to display when able to use a trigger or item
        STAT_USETIME,                   // elased time for using 
        STAT_USETIME_MAX,               // total time required to use
        STAT_USEWEAPONDROP,				// value to drop weapon out of view when using
    }

    // player_state->persistant[] indexes
    public enum PersEnum
    {
        PERS_SCORE,                     // !!! MUST NOT CHANGE, SERVER AND GAME BOTH REFERENCE !!!
        PERS_RANK,                      // player rank or team rank
        PERS_TEAM,                      // player team
        PERS_SPAWN_COUNT,               // incremented every respawn
        PERS_PLAYEREVENTS,              // 16 bits that can be flipped for events
        PERS_ATTACKER,                  // clientnum of last damage inflicter

        PERS_RED_SCORE,                 // Blue team score
        PERS_BLUE_SCORE,                // red team score

        PERS_RED_ALIVE_COUNT,           // number of alive people on the red team
        PERS_BLUE_ALIVE_COUNT,			// number of alive people on the blue team
    }

    public enum GoggleType
    {
        GOGGLES_NONE,
        GOGGLES_NIGHTVISION,
        GOGGLES_INFRARED,
        GOGGLES_MAX
    }

    // voice_event_t
    public enum VoiceEvent
    {
        VEV_TALKSTART,
        VEV_TALKSTOP
    }

    // game_over_t
    public enum GameOverType
    {
        GAME_OVER_TIMELIMIT,
        GAME_OVER_SCORELIMIT
    }

    // global_team_sound_t
    public enum GlobalTeamSound
    {
        GTS_RED_CAPTURE,
        GTS_BLUE_CAPTURE,
        GTS_RED_RETURN,
        GTS_BLUE_RETURN,
        GTS_RED_TAKEN,
        GTS_BLUE_TAKEN,
        GTS_REDTEAM_SCORED,
        GTS_BLUETEAM_SCORED,
        GTS_REDTEAM_TOOK_LEAD,
        GTS_BLUETEAM_TOOK_LEAD,
        GTS_TEAMS_ARE_TIED
    }
    // respawnType_t
    public enum RespawnType
    {
        RT_NORMAL,
        RT_DELAYED,
        RT_INTERVAL,
        RT_NONE,
        RT_MAX
    }

    // WACT (weapon actions)
    public enum WACT
    {
        WACT_READY,
        WACT_IDLE,
        WACT_FIRE,
        WACT_FIRE_END,      // Special fire-end action for some weapons.
        WACT_ALTFIRE,
        WACT_ALTFIRE_END,   // Special altfire-end action for some weapons.
        WACT_RELOAD,
        WACT_ALTRELOAD,
        WACT_RELOAD_END,    // Special reload-end for M590 shotgun
        WACT_PUTAWAY,
        WACT_ZOOMIN,
        WACT_ZOOMOUT,
        WACT_CHARGE,
        WACT_ALTCHARGE
    }

    public enum ItemType
    {
        IT_BAD,
        IT_WEAPON,      // Weapon item
        IT_AMMO,        // Ammo item
        IT_ARMOR,       // Armor item
        IT_HEALTH,      // Healh item
        IT_GAMETYPE,    // Custom gametype related item
        IT_BACKPACK,    // replenish backpack item
        IT_PASSIVE,		// Passive items
    }

    public enum EntityEvent
    {
        None,
        Footstep,
        Footwade,
        Swim,
        Step4,
        Step8,
        Step12,
        Step16,
        FallShort,
        FallMedium,
        FallFar,
        Jump,
        WaterFootstep,
        WaterTouch,
        WaterLand,
        WaterClear,
        ItemPickup,
        ItemPickupQuiet,
        NoAmmo,
        ChangeWeapon,
        ChangeWeaponCancelled,
        ReadyWeapon,
        FireWeapon,
        AltFire,
        Use,
        ItemRespawn,
        ItemPop,
        PlayerTeleportIn,
        PlayerTeleportOut,
        GrenadeBounce,
        PlayEffect,
        GeneralSound,
        GlobalSound,
        EntitySound,
        GlassShatter,
        MissileHit,
        MissileMiss,
        BulletHitWall,
        BulletHitFlesh,
        Bullet,
        ExplosionHitFlesh,
        Pain,
        PainWater,
        Obituary,
        DestroyGhoul2Instance,
        WeaponCharge,
        WeaponChargeAlt,
        DebugLine,
        TestLine,
        StopLoopingSound,
        BodyQueueCopy,
        BotWaypoint,
        ProcGore,
        GametypeRestart,
        GameOver,
        Goggles,
        WeaponCallback
    }

    public enum EntityType
    {
        General,
        Player,
        Item,
        Missile,
        Mover,
        Beam,
        Portal,
        Speaker,
        PushTrigger,
        TeleportTrigger,
        Invisible,
        Grapple,// grapple hooked on wall
        Body,
        DamageArea,
        Terrain,
        DebugCylinder,
        GametypeTrigger,
        Wall,
        Events // any of the EV_* events can be added freestanding
               // by setting eType to ET_EVENTS + eventNum
               // this avoids having to set eFlags and eventNum
    }
}

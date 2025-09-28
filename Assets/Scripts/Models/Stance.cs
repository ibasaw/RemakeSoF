using System;

public enum Stance
{
    Pain,
    DeadPain,

    // Bewegungs-/Haltungen
    Stand,
    Crouch,
    Prone,
    Sit,
    Jump,
    Fall,
    Vault,
    Climb,
    Rappel,
    RappelSlide,
    Drop,
    Swim,
    Dive,

    // Spezialaktionen / Events
    SpawnTroops,
    GrenadePickup,
    RollGrenade,
    Stretch,

    // Liegende Zustände
    LayFaceUp,
    LayFaceDown,
    LayRightSide,
    LayLeftSide,

    // Tod als eigener Zustand
    Dead
}



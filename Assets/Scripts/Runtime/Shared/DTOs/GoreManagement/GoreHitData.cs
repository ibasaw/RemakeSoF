using Tolik.RemakeSoF.Runtime.Game.Characters.Shared;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.GoreManagement
{
    /// <summary>
    /// Event-Daten fuer einen Gore-Treffer. Wird vom Damage-System erzeugt und an den GoreManager uebergeben.
    /// </summary>
    public struct GoreHitData
    {
        /// <summary>Root-GameObject des getroffenen Charakters.</summary>
        public GameObject CharacterRoot;

        /// <summary>Getroffene Hitbox-Region (aus BoxCollider).</summary>
        public HitRegion HitRegion;

        /// <summary>SoF2 DamageLevel (0=Low .. 5=High Death). Blood FX bei 0-3, Dismemberment bei >= 4.</summary>
        public int DamageLevel;

        /// <summary>Richtung des Treffers (Schussrichtung) fuer Chunk-Force.</summary>
        public Vector3 HitDirection;

        /// <summary>Weltposition des Treffers fuer Effekt-Spawning.</summary>
        public Vector3 HitPoint;

        /// <summary>Waffen-ID des Angreifers fuer PGORE-Dispatch (z.B. "knife", "m4", "m590").</summary>
        public string WeaponId;

        /// <summary>Ob es sich um einen Alternativangriff handelt (z.B. Bayonett, Kolbenschlag, M203).</summary>
        public bool IsAltAttack;
    }
}

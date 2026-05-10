using System;

namespace Tolik.RemakeSoF.Runtime.AI.Personality
{
    /// <summary>
    /// Persoenlichkeitsprofil eines Bots. Wird per Zufalls-Pick aus
    /// <see cref="BotPersonalityLoader"/> beim Spawn jedem Bot zugewiesen
    /// und skaliert dessen Wahrnehmung, Aim und Engage-Verhalten.
    /// Plain-Class (kein ScriptableObject) — JsonUtility-kompatibel.
    /// </summary>
    [Serializable]
    public class BotPersonality
    {
        /// <summary>Eindeutiger Name (z.B. "Aggressive", "Sniper").</summary>
        public string name = "Default";

        /// <summary>Spawn-Gewichtung. 0 = nie, 1 = Standard.</summary>
        public float weight = 1f;

        /// <summary>Sekunden vom ersten Sichtkontakt bis der Bot reagiert.</summary>
        public float reactionTimeSec = 0.25f;

        /// <summary>0..1. 1 = perfektes Aim, 0 = sehr ungenau (Winkel-Streuung).</summary>
        public float accuracy = 0.85f;

        /// <summary>Multiplikator auf die Engage-Reichweite (m_WeaponRangeMeters).</summary>
        public float engageRangeMul = 1f;

        /// <summary>Radius in Metern, in dem feindliche Schuesse als LKP gewertet werden.</summary>
        public float audioHearRangeMeters = 30f;

        /// <summary>Ob Sichtungen an Teamkollegen geteilt werden (Squad-LKP-Publish).</summary>
        public bool shareSightingsToTeam = true;

        /// <summary>
        /// Trigger-Disziplin in Sekunden — Pause zwischen Trigger-Druecken bei
        /// Single-Fire-Waffen und zwischen Bursts. 0.10 = sehr schneller Tap-Fire,
        /// 0.35 = traeger / kontrollierter. Wird auf "auto"-Waffen nicht angewendet.
        /// </summary>
        public float triggerDisciplineSec = 0.18f;

        /// <summary>
        /// Anzahl Schuesse pro Burst-Trigger-Druck bei Burst-Fire-Waffen.
        /// 1 = wie Single-Fire, 3 = SoF2-Standard, 5 = aggressiv.
        /// </summary>
        public int burstShots = 3;

        /// <summary>
        /// 0..1. Skaliert wie weit der Bot beim Predictive Aiming vorhaelt.
        /// 1 = volle Vorhaltung (Veteran), 0 = keinerlei Vorhaltung (Rookie schiesst auf Aktuelle Pos).
        /// </summary>
        public float aimLeadCompensation = 1.0f;
    }
}

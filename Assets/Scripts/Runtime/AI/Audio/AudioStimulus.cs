using System;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.AI.Audio
{
    /// <summary>
    /// Server-seitiger Event-Hub fuer akustische Reize ("Sound-LKPs").
    /// Wenn eine Schussabgabe, Schritt oder Explosion stattfindet, kann der Verursacher
    /// <see cref="Emit"/> aufrufen — alle Bots auf dem Server entscheiden selbststaendig
    /// (basierend auf eigener Persoenlichkeit + Team), ob der Reiz fuer sie relevant ist.
    ///
    /// Bewusst statisch + simpel: Server-only, kein Netzwerk-Sync noetig
    /// (Bots leben in derselben Server-Instanz). Subscriber muessen sich
    /// zuverlaessig in OnDestroy abmelden, sonst Memory-Leak.
    /// </summary>
    public static class AudioStimulus
    {
        /// <summary>Reiz-Quellen — beeinflusst, welche Bots reagieren sollen.</summary>
        public enum SoundType
        {
            /// <summary>Schuss (laut, weite Reichweite).</summary>
            Gunshot = 0,
            /// <summary>Explosion (sehr laut).</summary>
            Explosion = 1,
            /// <summary>Schritte (leise, kurze Reichweite).</summary>
            Footstep = 2,
            /// <summary>Reload-Klick / sonstiges Geraeusch.</summary>
            Other = 3,
        }

        /// <summary>Daten eines Audio-Reizes.</summary>
        public struct Stimulus
        {
            /// <summary>Weltposition der Schallquelle.</summary>
            public Vector3 Position;
            /// <summary>Team-ID des Verursachers (0 = FFA / kein Team).</summary>
            public uint EmitterTeamId;
            /// <summary>Maximaler Hoerradius dieses Reizes in Metern (Waffe-spezifisch).</summary>
            public float MaxRangeMeters;
            /// <summary>Art des Reizes.</summary>
            public SoundType Type;
            /// <summary>Server-Zeitpunkt der Emission (Time.time).</summary>
            public float Time;
        }

        /// <summary>Wird gefeuert, wenn ein neuer Reiz emittiert wird. Server-only.</summary>
        public static event Action<Stimulus> OnEmitted;

        /// <summary>
        /// Emittiert einen Audio-Reiz. Subscriber (Bots) entscheiden selbststaendig
        /// ueber Hoerradius + Team, ob sie reagieren.
        /// </summary>
        public static void Emit(Vector3 position, uint emitterTeamId, float maxRangeMeters, SoundType type)
        {
            if (OnEmitted == null)
            {
                return;
            }

            Stimulus s = new()
            {
                Position = position,
                EmitterTeamId = emitterTeamId,
                MaxRangeMeters = maxRangeMeters,
                Type = type,
                Time = UnityEngine.Time.time,
            };

            OnEmitted.Invoke(s);
        }

        /// <summary>
        /// Setzt alle Subscriber zurueck. In Tests / Server-Restart aufrufen.
        /// </summary>
        public static void ClearSubscribers()
        {
            OnEmitted = null;
        }
    }
}

namespace Tolik.RemakeSoF.Runtime.GametypeManagement
{
    /// <summary>
    /// Ergebnis einer Gametype-Damage-Modifikation.
    /// Wird von IGametype.OnDamage zurueckgegeben um Schaden zu modifizieren
    /// oder Zusatzeffekte (Stun, Nachrichten) auszuloesen.
    /// </summary>
    public struct GametypeDamageResult
    {
        /// <summary>Modifizierter Schaden (0 = kein Schaden anwenden).</summary>
        public int ModifiedDamage;

        /// <summary>Ob ein Stun auf das Opfer angewendet werden soll.</summary>
        public bool ApplyStun;

        /// <summary>Dauer des Stuns in Sekunden.</summary>
        public float StunDuration;

        /// <summary>Nachricht die dem Angreifer angezeigt wird (null = keine).</summary>
        public string AttackerMessage;

        /// <summary>Nachricht die dem Opfer angezeigt wird (null = keine).</summary>
        public string VictimMessage;

        /// <summary>Erstellt ein Default-Ergebnis mit unveraendertem Schaden.</summary>
        /// <param name="originalDamage">Der urspruengliche Schaden.</param>
        public static GametypeDamageResult Default(int originalDamage)
        {
            return new GametypeDamageResult
            {
                ModifiedDamage = originalDamage,
                ApplyStun = false,
                StunDuration = 0f,
                AttackerMessage = null,
                VictimMessage = null
            };
        }
    }
}

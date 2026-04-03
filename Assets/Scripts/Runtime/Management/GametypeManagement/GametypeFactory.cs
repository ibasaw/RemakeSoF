using System.Collections.Generic;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.GametypeManagement
{
    /// <summary>
    /// Factory fuer Gametype-Instanzen. Erstellt die korrekte IGametype-Implementierung
    /// basierend auf dem Gametype-Identifier aus der Server-Konfiguration.
    /// </summary>
    public static class GametypeFactory
    {
        /// <summary>Registry aller bekannten Gametype-Implementierungen.</summary>
        static readonly Dictionary<string, System.Func<IGametype>> s_Registry = new()
        {
            { "hideandseek", () => new HideAndSeekGametype() },
            { "dm", () => new DeathmatchGametype() },
            { "tdm", () => new TeamDeathmatchGametype() },
        };

        /// <summary>
        /// Erstellt eine neue Gametype-Instanz fuer den angegebenen Identifier.
        /// </summary>
        /// <param name="gametypeId">Der Gametype-Identifier (z.B. "hideandseek").</param>
        /// <returns>Eine neue IGametype-Instanz oder null wenn unbekannt.</returns>
        public static IGametype Create(string gametypeId)
        {
            if (string.IsNullOrEmpty(gametypeId))
            {
                Debug.LogError("[GametypeFactory] Gametype-ID ist null oder leer.");
                return null;
            }

            if (s_Registry.TryGetValue(gametypeId, out System.Func<IGametype> factory))
            {
                return factory();
            }

            Debug.LogWarning($"[GametypeFactory] Unbekannter Gametype: '{gametypeId}'. Fallback auf TDM.");
            return new TeamDeathmatchGametype();
        }
    }
}

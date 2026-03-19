using System.Collections.Generic;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.Characters.Server
{
    /// <summary>
    /// Verwaltet team-basierte Player-Spawn-Points auf dem Server.
    /// Wird vom MapLoader mit generierten Spawn-Positionen befuellt.
    /// Spawn-Points werden nach Teams aufgeteilt (gegenueberliegende Kartenseiten).
    /// </summary>
    public class ServerPlayerSpawnPoints : MonoBehaviour
    {
        /// <summary>Singleton-Instanz fuer schnellen Zugriff vom Server.</summary>
        private static ServerPlayerSpawnPoints s_Instance;

        /// <summary>Verfuegbare Spawn-Positionen pro Team (werden beim Konsumieren entfernt).</summary>
        private readonly Dictionary<TeamId, List<Vector3>> m_AvailableSpawnPoints = new()
        {
            { TeamId.TeamOne, new() },
            { TeamId.TeamTwo, new() }
        };

        /// <summary>Alle Spawn-Positionen pro Team (unveraendert, fuer Round-Robin-Reset).</summary>
        private readonly Dictionary<TeamId, List<Vector3>> m_AllSpawnPoints = new()
        {
            { TeamId.TeamOne, new() },
            { TeamId.TeamTwo, new() }
        };

        /// <summary>Globale Singleton-Instanz.</summary>
        public static ServerPlayerSpawnPoints Instance => s_Instance;

        /// <summary>Anzahl der aktuell verfuegbaren Spawn-Points fuer ein Team.</summary>
        public int GetAvailableCount(TeamId team) => m_AvailableSpawnPoints[team].Count;

        /// <summary>Gesamtanzahl aller Spawn-Points (beide Teams).</summary>
        public int TotalCount => m_AllSpawnPoints[TeamId.TeamOne].Count + m_AllSpawnPoints[TeamId.TeamTwo].Count;

        private void Awake()
        {
            s_Instance = this;
        }

        private void OnDestroy()
        {
            if (s_Instance == this)
            {
                s_Instance = null;
            }
        }

        /// <summary>
        /// Befuellt die Spawn-Points fuer beide Teams.
        /// Wird vom MapLoader nach der Spawn-Point-Generierung aufgerufen.
        /// </summary>
        /// <param name="teamOnePoints">Spawn-Positionen fuer Team 1.</param>
        /// <param name="teamTwoPoints">Spawn-Positionen fuer Team 2.</param>
        public void SetSpawnPoints(List<Vector3> teamOnePoints, List<Vector3> teamTwoPoints)
        {
            m_AllSpawnPoints[TeamId.TeamOne] = new List<Vector3>(teamOnePoints);
            m_AllSpawnPoints[TeamId.TeamTwo] = new List<Vector3>(teamTwoPoints);

            m_AvailableSpawnPoints[TeamId.TeamOne] = new List<Vector3>(teamOnePoints);
            m_AvailableSpawnPoints[TeamId.TeamTwo] = new List<Vector3>(teamTwoPoints);

            Debug.Log($"[ServerPlayerSpawnPoints] Spawn-Points gesetzt: " +
                      $"TeamOne={teamOnePoints.Count}, TeamTwo={teamTwoPoints.Count}");
        }

        /// <summary>
        /// Gibt den naechsten verfuegbaren Spawn-Point fuer das angegebene Team zurueck.
        /// Wenn keine mehr verfuegbar sind, wird die Liste zurueckgesetzt (Round-Robin).
        /// </summary>
        /// <param name="team">Das Team fuer das ein Spawn-Point benoetigt wird.</param>
        /// <returns>Position und Rotation des Spawn-Points.</returns>
        public (Vector3 position, Quaternion rotation) ConsumeNextSpawnPoint(TeamId team)
        {
            List<Vector3> available = m_AvailableSpawnPoints[team];
            List<Vector3> all = m_AllSpawnPoints[team];

            if (available.Count == 0)
            {
                available.AddRange(all);

                if (available.Count == 0)
                {
                    Debug.LogWarning($"[ServerPlayerSpawnPoints] Keine Spawn-Points fuer {team}! Fallback auf Origin.");
                    return (Vector3.zero, Quaternion.identity);
                }

                Debug.Log($"[ServerPlayerSpawnPoints] Spawn-Points fuer {team} zurueckgesetzt (Round-Robin)");
            }

            int index = Random.Range(0, available.Count);
            Vector3 position = available[index];
            available.RemoveAt(index);

            return (position, Quaternion.identity);
        }

        /// <summary>
        /// Gibt den naechsten Spawn-Point zurueck und alterniert automatisch zwischen Teams.
        /// Kompatibilitaets-Methode fuer Code der keine Team-Zuordnung kennt.
        /// </summary>
        /// <returns>Position und Rotation des Spawn-Points.</returns>
        public (Vector3 position, Quaternion rotation) ConsumeNextSpawnPoint()
        {
            // Alterniert: Team mit mehr verfuegbaren Punkten bevorzugen
            TeamId team = m_AvailableSpawnPoints[TeamId.TeamOne].Count >= m_AvailableSpawnPoints[TeamId.TeamTwo].Count
                ? TeamId.TeamOne
                : TeamId.TeamTwo;

            return ConsumeNextSpawnPoint(team);
        }

        /// <summary>
        /// Gibt einen zufaelligen Spawn-Point fuer das angegebene Team zurueck ohne ihn zu konsumieren.
        /// </summary>
        /// <param name="team">Das Team.</param>
        /// <returns>Position und Rotation eines zufaelligen Spawn-Points.</returns>
        public (Vector3 position, Quaternion rotation) GetRandomSpawnPoint(TeamId team)
        {
            List<Vector3> all = m_AllSpawnPoints[team];

            if (all.Count == 0)
            {
                Debug.LogWarning($"[ServerPlayerSpawnPoints] Keine Spawn-Points fuer {team}! Fallback auf Origin.");
                return (Vector3.zero, Quaternion.identity);
            }

            Vector3 position = all[Random.Range(0, all.Count)];
            return (position, Quaternion.identity);
        }

        /// <summary>
        /// Gibt einen zufaelligen Spawn-Point zurueck (beliebiges Team) ohne ihn zu konsumieren.
        /// </summary>
        /// <returns>Position und Rotation eines zufaelligen Spawn-Points.</returns>
        public (Vector3 position, Quaternion rotation) GetRandomSpawnPoint()
        {
            TeamId team = Random.Range(0, 2) == 0 ? TeamId.TeamOne : TeamId.TeamTwo;

            if (m_AllSpawnPoints[team].Count == 0)
            {
                team = team == TeamId.TeamOne ? TeamId.TeamTwo : TeamId.TeamOne;
            }

            return GetRandomSpawnPoint(team);
        }
    }
}

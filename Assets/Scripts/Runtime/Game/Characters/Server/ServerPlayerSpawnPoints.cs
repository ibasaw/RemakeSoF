using System.Collections.Generic;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.Characters.Server
{
    /// <summary>
    /// Verwaltet Player-Spawn-Points auf dem Server.
    /// Wird automatisch vom MapLoader an das "PlayerSpawnPoints"-GameObject gehängt.
    /// Liest alle Child-Transforms als verfügbare Spawn-Positionen.
    /// </summary>
    public class ServerPlayerSpawnPoints : MonoBehaviour
    {
        /// <summary>
        /// Singleton-Instanz für schnellen Zugriff vom Server.
        /// </summary>
        private static ServerPlayerSpawnPoints s_Instance;

        /// <summary>
        /// Verfügbare Spawn-Positionen (werden beim Konsumieren entfernt).
        /// </summary>
        private List<Transform> m_SpawnPoints = new();

        /// <summary>
        /// Alle Spawn-Positionen (unverändert, für Reset/Respawn).
        /// </summary>
        private List<Transform> m_AllSpawnPoints = new();

        /// <summary>
        /// Globale Singleton-Instanz.
        /// </summary>
        public static ServerPlayerSpawnPoints Instance => s_Instance;

        /// <summary>
        /// Anzahl der aktuell verfügbaren Spawn-Points.
        /// </summary>
        public int AvailableCount => m_SpawnPoints.Count;

        private void Awake()
        {
            s_Instance = this;
            CollectSpawnPoints();
        }

        private void OnDestroy()
        {
            if (s_Instance == this)
            {
                s_Instance = null;
            }
        }

        /// <summary>
        /// Sammelt alle Child-Transforms als Spawn-Points.
        /// </summary>
        private void CollectSpawnPoints()
        {
            m_SpawnPoints.Clear();
            m_AllSpawnPoints.Clear();

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                m_SpawnPoints.Add(child);
                m_AllSpawnPoints.Add(child);
            }

            Debug.Log($"[ServerPlayerSpawnPoints] {m_SpawnPoints.Count} Spawn-Points gesammelt");
        }

        /// <summary>
        /// Gibt den nächsten verfügbaren Spawn-Point zurück und entfernt ihn aus der Liste.
        /// Wenn keine mehr verfügbar sind, wird die Liste zurückgesetzt (Round-Robin).
        /// </summary>
        /// <returns>Position und Rotation des Spawn-Points.</returns>
        public (Vector3 position, Quaternion rotation) ConsumeNextSpawnPoint()
        {
            if (m_SpawnPoints.Count == 0)
            {
                // Round-Robin: Alle Spawn-Points wieder verfügbar machen
                m_SpawnPoints.AddRange(m_AllSpawnPoints);

                if (m_SpawnPoints.Count == 0)
                {
                    Debug.LogWarning("[ServerPlayerSpawnPoints] Keine Spawn-Points vorhanden! Fallback auf Origin.");
                    return (Vector3.zero, Quaternion.identity);
                }

                Debug.Log("[ServerPlayerSpawnPoints] Spawn-Points zurückgesetzt (Round-Robin)");
            }

            int lastIndex = m_SpawnPoints.Count - 1;
            Transform spawnPoint = m_SpawnPoints[lastIndex];
            m_SpawnPoints.RemoveAt(lastIndex);

            return (spawnPoint.position, spawnPoint.rotation);
        }

        /// <summary>
        /// Gibt einen zufälligen Spawn-Point zurück ohne ihn zu konsumieren.
        /// Nützlich für Respawn.
        /// </summary>
        /// <returns>Position und Rotation eines zufälligen Spawn-Points.</returns>
        public (Vector3 position, Quaternion rotation) GetRandomSpawnPoint()
        {
            if (m_AllSpawnPoints.Count == 0)
            {
                Debug.LogWarning("[ServerPlayerSpawnPoints] Keine Spawn-Points vorhanden! Fallback auf Origin.");
                return (Vector3.zero, Quaternion.identity);
            }

            Transform spawnPoint = m_AllSpawnPoints[Random.Range(0, m_AllSpawnPoints.Count)];
            return (spawnPoint.position, spawnPoint.rotation);
        }
    }
}

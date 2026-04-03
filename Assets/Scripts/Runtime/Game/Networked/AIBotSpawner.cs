using System.Collections.Generic;
using Tolik.RemakeSoF.Runtime.Game.Characters.Networked;
using Unity.Netcode;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.Networked
{
    /// <summary>
    /// Server-seitige Komponente die AI-Bots spawnt und verwaltet.
    /// Wird am selben GameObject wie NetworkedGameState platziert.
    /// Spawnt Bots nach dem Map-Laden und verwaltet ihren Lebenszyklus.
    /// </summary>
    public class AIBotSpawner : NetworkBehaviour
    {
        /// <summary>
        /// Prefab fuer AI-Bot-Characters. Muss ein NetworkObject mit
        /// NetworkedAICharacter, ServerAICharacter, ClientAICharacter,
        /// NetworkedCharacterState und ClientCharacterSkinHandler enthalten.
        /// Muss in der NetworkManager NetworkPrefabs-Liste registriert sein.
        /// </summary>
        [SerializeField]
        private GameObject m_AIBotPrefab;

        /// <summary>
        /// Anzahl der Bots die beim Spielstart gespawnt werden sollen.
        /// </summary>
        [SerializeField]
        private int m_InitialBotCount = 1;

        /// <summary>
        /// Verfuegbare Bot-Namen (SoF2-authentisch).
        /// </summary>
        private static readonly string[] s_BotNames = new string[]
        {
            "Hawk", "Viper", "Ghost", "Snake", "Jackal",
            "Wolf", "Raven", "Cobra", "Falcon", "Panther",
            "Shadow", "Blade", "Storm", "Frost", "Thunder"
        };

        /// <summary>
        /// Verfuegbare Bot-Skins.
        /// </summary>
        private static readonly string[] s_BotSkins = new string[]
        {
            "col1_soldier1", "col1_soldier2", "col1_soldier3"
        };

        /// <summary>
        /// Liste aller aktuell gespawnten AI-Bot NetworkObjects.
        /// </summary>
        private readonly List<NetworkObject> m_SpawnedBots = new();

        /// <summary>
        /// Naechster Bot-Name-Index (Round-Robin).
        /// </summary>
        private int m_NextBotNameIndex;

        /// <summary>
        /// Spawnt die initiale Anzahl an AI-Bots.
        /// Wird vom RoundFlowStateMachine aufgerufen wenn die Map geladen ist.
        /// </summary>
        public void SpawnInitialBots()
        {
            if (!IsServer)
            {
                return;
            }

            if (m_AIBotPrefab == null)
            {
                Debug.LogWarning("[AIBotSpawner] Kein AI-Bot-Prefab zugewiesen. Keine Bots gespawnt.");
                return;
            }

            Debug.Log($"[AIBotSpawner] Spawne {m_InitialBotCount} AI-Bots...");

            for (int i = 0; i < m_InitialBotCount; i++)
            {
                SpawnBot();
            }
        }

        /// <summary>
        /// Spawnt einen einzelnen AI-Bot.
        /// </summary>
        /// <returns>Die NetworkedAICharacter-Referenz des gespawnten Bots, oder null bei Fehler.</returns>
        public NetworkedAICharacter SpawnBot()
        {
            if (!IsServer)
            {
                return null;
            }

            if (m_AIBotPrefab == null)
            {
                Debug.LogError("[AIBotSpawner] Kein AI-Bot-Prefab zugewiesen!");
                return null;
            }

            GameObject botInstance = Instantiate(m_AIBotPrefab);
            NetworkObject networkObject = botInstance.GetComponent<NetworkObject>();

            if (networkObject == null)
            {
                Debug.LogError("[AIBotSpawner] AI-Bot-Prefab hat kein NetworkObject!");
                Destroy(botInstance);
                return null;
            }

            // Server-owned Spawn (kein Owner-Client)
            networkObject.Spawn();

            NetworkedAICharacter aiCharacter = botInstance.GetComponent<NetworkedAICharacter>();
            if (aiCharacter == null)
            {
                Debug.LogError("[AIBotSpawner] AI-Bot-Prefab hat kein NetworkedAICharacter!");
                networkObject.Despawn();
                return null;
            }

            // Bot initialisieren: Name, Skin, Team
            string botName = GetNextBotName();
            string botSkin = s_BotSkins[m_SpawnedBots.Count % s_BotSkins.Length];
            uint teamId = (uint)(m_SpawnedBots.Count % 2); // Abwechselnd Team 0 und 1

            aiCharacter.InitializeBot(botName, botSkin, teamId);

            m_SpawnedBots.Add(networkObject);
            Debug.Log($"[AIBotSpawner] Bot gespawnt: {botName} (Team {teamId}, Skin: {botSkin})");

            return aiCharacter;
        }

        /// <summary>
        /// Despawnt alle AI-Bots (z.B. beim Map-Wechsel).
        /// </summary>
        public void DespawnAllBots()
        {
            if (!IsServer)
            {
                return;
            }

            foreach (NetworkObject bot in m_SpawnedBots)
            {
                if (bot != null && bot.IsSpawned)
                {
                    bot.Despawn();
                }
            }

            m_SpawnedBots.Clear();
            Debug.Log("[AIBotSpawner] Alle AI-Bots despawnt.");
        }

        /// <summary>
        /// Respawnt alle AI-Bots auf neue Spawn-Positionen.
        /// </summary>
        public void RespawnAllBots()
        {
            if (!IsServer)
            {
                return;
            }

            foreach (NetworkObject bot in m_SpawnedBots)
            {
                if (bot != null && bot.IsSpawned && bot.TryGetComponent(out NetworkedAICharacter aiCharacter))
                {
                    aiCharacter.RespawnAtNextSpawnPoint();
                }
            }

            Debug.Log($"[AIBotSpawner] {m_SpawnedBots.Count} AI-Bots respawnt.");
        }

        /// <summary>
        /// Anzahl aktuell gespawnter Bots.
        /// </summary>
        public int SpawnedBotCount => m_SpawnedBots.Count;

        /// <summary>
        /// Gibt den naechsten Bot-Namen zurueck (Round-Robin).
        /// </summary>
        private string GetNextBotName()
        {
            string name = s_BotNames[m_NextBotNameIndex % s_BotNames.Length];
            m_NextBotNameIndex++;
            return name;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            m_SpawnedBots.Clear();
        }
    }
}

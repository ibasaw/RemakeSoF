using System.Collections.Generic;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.Game.Characters.Networked;
using Tolik.RemakeSoF.Runtime.GametypeManagement;
using Tolik.RemakeSoF.Runtime.PrefabManagement;
using Unity.Netcode;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.Networked
{
    /// <summary>
    /// Server-seitige Komponente die AI-Bots spawnt und verwaltet.
    /// Wird am selben GameObject wie NetworkedGameState platziert.
    /// Spawnt Bots nach dem Map-Laden und verwaltet ihren Lebenszyklus.
    /// Laedt das AI-Bot-Prefab ueber den PrefabManager (Cache-first, Addressables).
    /// MonoBehaviour statt NetworkBehaviour, da per AddComponent dynamisch hinzugefuegt.
    /// </summary>
    public class AIBotSpawner : MonoBehaviour
    {
        /// <summary>
        /// Addressable-Key fuer das AI-Bot-Prefab.
        /// </summary>
        private const string k_AIBotPrefabKey = "AICharacter";

        /// <summary>
        /// Geladenes AI-Bot-Prefab (ueber PrefabManager).
        /// </summary>
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
            "mullins_jungle"
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
        /// Prueft ob dieser Prozess der Server ist.
        /// </summary>
        private bool IsServer => NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;

        /// <summary>
        /// Spawnt die initiale Anzahl an AI-Bots.
        /// Wird vom RoundFlowStateMachine aufgerufen wenn die Map geladen ist.
        /// Laedt das Prefab synchron ueber den PrefabManager (Cache-first).
        /// </summary>
        public void SpawnInitialBots()
        {
            Debug.Log($"[AIBotSpawner] SpawnInitialBots aufgerufen. IsServer={IsServer}, NetworkManager.Singleton={(NetworkManager.Singleton != null ? "vorhanden" : "NULL")}");

            if (!IsServer)
            {
                Debug.LogWarning("[AIBotSpawner] SpawnInitialBots abgebrochen: Nicht der Server.");
                return;
            }

            if (m_AIBotPrefab == null)
            {
                PrefabManager prefabManager = ServiceLocator.Get<PrefabManager>();
                if (prefabManager == null)
                {
                    Debug.LogError("[AIBotSpawner] PrefabManager nicht im ServiceLocator registriert!");
                    return;
                }

                m_AIBotPrefab = prefabManager.LoadPrefab<GameObject>(k_AIBotPrefabKey);
                if (m_AIBotPrefab == null)
                {
                    Debug.LogError($"[AIBotSpawner] Konnte AI-Bot-Prefab '{k_AIBotPrefabKey}' nicht ueber PrefabManager laden!");
                    return;
                }

                Debug.Log($"[AIBotSpawner] AI-Bot-Prefab '{k_AIBotPrefabKey}' geladen: {m_AIBotPrefab.name}");
            }

            Debug.Log($"[AIBotSpawner] Spawne {m_InitialBotCount} AI-Bots...");

            for (int i = 0; i < m_InitialBotCount; i++)
            {
                SpawnBot();
            }

            OnInitialBotsSpawned?.Invoke();
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

            // Team-Zuweisung ueber GametypeManager (respektiert Gametype-Regeln)
            GametypeManager gametypeManager = ServiceLocator.Get<GametypeManager>();
            uint teamId;
            if (gametypeManager != null)
            {
                // Aktuelle Team-Groessen zaehlen (inkl. Spieler und bereits gespawnte Bots)
                NetworkedGameState gameState = NetworkedGameState.Singleton;
                (int redCount, int blueCount) = gameState != null
                    ? CountAllTeamMembers(gameState)
                    : (0, 0);
                GametypeTeam assignedTeam = gametypeManager.AssignTeam(redCount, blueCount);
                teamId = (uint)assignedTeam;
            }
            else
            {
                teamId = (uint)(m_SpawnedBots.Count % 2 + 1);
            }

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
        /// Readonly-Zugriff auf die gespawnten Bot-NetworkObjects.
        /// Wird von RoundFlowStateMachine.CountTeams() benoetigt um AI-Bots in Team-Zaehlung einzubeziehen.
        /// </summary>
        public IReadOnlyList<NetworkObject> SpawnedBots => m_SpawnedBots;

        /// <summary>
        /// Callback der nach dem initialen Bot-Spawn aufgerufen wird.
        /// Ermoeglicht der RoundFlowStateMachine nach dem async Addressable-Laden
        /// erneut TryStartMatch aufzurufen.
        /// </summary>
        public event System.Action OnInitialBotsSpawned;

        /// <summary>
        /// Gibt den naechsten Bot-Namen zurueck (Round-Robin).
        /// </summary>
        private string GetNextBotName()
        {
            string name = s_BotNames[m_NextBotNameIndex % s_BotNames.Length];
            m_NextBotNameIndex++;
            return name;
        }

        /// <summary>
        /// Aufraeumen wenn die Komponente zerstoert wird.
        /// </summary>
        private void OnDestroy()
        {
            m_SpawnedBots.Clear();
            m_AIBotPrefab = null;
        }

        /// <summary>
        /// Zaehlt alle Team-Mitglieder (Spieler + Bots) fuer korrekte Team-Zuweisung.
        /// </summary>
        private (int redCount, int blueCount) CountAllTeamMembers(NetworkedGameState gameState)
        {
            int redCount = 0;
            int blueCount = 0;

            foreach (ulong clientId in gameState.NetworkManager.ConnectedClientsIds)
            {
                NetworkObject playerObject = gameState.NetworkManager.SpawnManager.GetPlayerNetworkObject(clientId);
                if (playerObject != null && playerObject.TryGetComponent(out NetworkedCharacterState state))
                {
                    uint team = state.TeamId;
                    if (team == (uint)GametypeTeam.Red)
                    {
                        redCount++;
                    }
                    else if (team == (uint)GametypeTeam.Blue)
                    {
                        blueCount++;
                    }
                }
            }

            // Bereits gespawnte Bots zaehlen
            foreach (NetworkObject bot in m_SpawnedBots)
            {
                if (bot != null && bot.IsSpawned && bot.TryGetComponent(out NetworkedCharacterState botState))
                {
                    uint team = botState.TeamId;
                    if (team == (uint)GametypeTeam.Red)
                    {
                        redCount++;
                    }
                    else if (team == (uint)GametypeTeam.Blue)
                    {
                        blueCount++;
                    }
                }
            }

            return (redCount, blueCount);
        }
    }
}

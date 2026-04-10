using System;
using System.Collections;
using System.Collections.Generic;
using Tolik.RemakeSoF.Runtime.AI;
using Tolik.RemakeSoF.Runtime.AI.GOAP;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.Core;
using Tolik.RemakeSoF.Runtime.DataManagement;
using Tolik.RemakeSoF.Runtime.Game.Characters.Networked;
using Tolik.RemakeSoF.Runtime.GametypeManagement;
using Tolik.RemakeSoF.Runtime.Management.MapManagement;
using Unity.AI.Navigation;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace Tolik.RemakeSoF.Runtime.Game.Networked
{
    /// <summary>
    /// Basistyp fuer alle Round-Flow-States des NetworkedGameState.
    /// States greifen ueber Manager.GameState auf Netzwerk-Logik zu.
    /// </summary>
    internal abstract class RoundFlowState : State<RoundFlowStateMachine>
    {
        /// <summary>Kurzreferenz auf das NetworkedGameState.</summary>
        protected NetworkedGameState GameState => Manager.GameState;

        /// <summary>Wird aufgerufen wenn eine MapLoadPhase eintrifft (nur Server).</summary>
        internal virtual void OnMapLoadPhase(MapLoadPhase phase) { }

        /// <summary>Wird aufgerufen wenn ein Client meldet, Gameplay ist sichtbar.</summary>
        internal virtual void OnClientReadyForRound() { }

        /// <summary>Wird aufgerufen wenn MinPlayers-Schwelle erreicht wird.</summary>
        internal virtual void OnMinPlayersReached() { }

        /// <summary>Wird aufgerufen wenn ein Client sich verbindet.</summary>
        internal virtual void OnClientConnected() { }

        /// <summary>Wird aufgerufen wenn ein Client sich trennt.</summary>
        internal virtual void OnClientDisconnected() { }

        /// <summary>Wird aufgerufen wenn der Gametype einen Runden-Restart anfordert.</summary>
        internal virtual void OnRoundRestartRequested(float delaySeconds) { }
    }

    /// <summary>
    /// StateMachine fuer den Match/Map-Lebenszyklus:
    /// Loading -> WaitingForReady -> StartingRound -> Running -> SwitchingMap -> Loading ...
    /// Wird ausschliesslich serverseitig betrieben.
    /// </summary>
    internal sealed class RoundFlowStateMachine : StateMachine<RoundFlowState, RoundFlowStateMachine>
    {
        internal readonly RoundFlowLoadingState LoadingState = new();
        internal readonly RoundFlowWaitingForReadyState WaitingForReadyState = new();
        internal readonly RoundFlowWarmupState WarmupState = new();
        internal readonly RoundFlowStartingRoundState StartingRoundState = new();
        internal readonly RoundFlowRunningState RunningState = new();
        internal readonly RoundFlowSwitchingMapState SwitchingMapState = new();

        /// <summary>Referenz auf das NetworkedGameState fuer Netzwerk-Operationen.</summary>
        internal NetworkedGameState GameState { get; private set; }

        /// <summary>Ob die MinPlayers-Schwelle aktuell erreicht ist.</summary>
        internal bool MinPlayersReached;

        /// <summary>Client-IDs die fuer die aktuelle Runde "Gameplay sichtbar" gemeldet haben.</summary>
        internal readonly HashSet<ulong> ClientsReadyForRound = new();

        /// <summary>Kumulierte Match-Zeit in Sekunden (ueber alle Runden). Fuer timelimit-basiertes Map-Switching bei roundlimit=0.</summary>
        internal float MatchElapsedTime;

        /// <summary>Delay in Sekunden zwischen "alle bereit" und Match-Start.</summary>
        internal const float RoundStartDelaySeconds = 3.1f;

        /// <summary>Name des aktuell aktiven States (Debug/HUD).</summary>
        internal string CurrentStateName => m_CurrentState?.GetType().Name ?? "None";

        bool m_Initialized;

        /// <summary>
        /// Aktiviert GOAP-Ziele fuer gespawnte Bots.
        /// Fuegt fehlende AgentBehaviour/GoapActionProvider-Komponenten hinzu,
        /// setzt den AgentType (Seeker/Hider) und fordert das entsprechende Ziel an.
        /// Optionaler Team-Filter: nur Bots eines bestimmten Teams aktivieren.
        /// Wird erst in RunningState aufgerufen, damit die GOAP-Planung erst mit Rundenstart beginnt.
        /// </summary>
        /// <param name="teamFilter">Wenn gesetzt, werden nur Bots dieses Teams aktiviert.</param>
        internal void ActivateBotGoals(GametypeTeam? teamFilter = null)
        {
            AIGoapSetup goapSetup = GameState.AIGoapSetup;
            if (goapSetup == null || GameState.AIBotSpawner == null)
            {
                return;
            }

            GametypeManager gametypeManager = ServiceLocator.Get<GametypeManager>();
            int activated = 0;

            foreach (NetworkObject bot in GameState.AIBotSpawner.SpawnedBots)
            {
                if (bot == null || !bot.IsSpawned)
                {
                    continue;
                }

                NetworkedCharacterState characterState = bot.GetComponent<NetworkedCharacterState>();
                if (characterState == null)
                {
                    continue;
                }

                bool isSeeker = gametypeManager != null
                    && characterState.TeamId == (uint)GametypeTeam.Blue;

                // Team-Filter anwenden
                if (teamFilter.HasValue)
                {
                    GametypeTeam botTeam = isSeeker ? GametypeTeam.Blue : GametypeTeam.Red;
                    if (botTeam != teamFilter.Value)
                    {
                        continue;
                    }
                }

                Game.Characters.Server.ServerAICharacter serverAI =
                    bot.GetComponent<Game.Characters.Server.ServerAICharacter>();
                if (serverAI?.AIController == null)
                {
                    continue;
                }

                // Rolle am Controller setzen
                serverAI.AIController.SetRole(isSeeker);

                // GOAP-Komponenten hinzufuegen (nur beim ersten Mal)
                CrashKonijn.Goap.Runtime.GoapActionProvider provider =
                    bot.GetComponent<CrashKonijn.Goap.Runtime.GoapActionProvider>();
                if (provider == null)
                {
                    // Provider zuerst hinzufuegen, dann AgentBehaviour.
                    // AgentBehaviour.Awake() ruft Initialize() auf — ActionProviderBase
                    // muss aber VOR Awake gesetzt sein, was bei AddComponent nicht moeglich
                    // ist. Deshalb setzen wir ActionProvider direkt nach AddComponent.
                    provider = bot.gameObject.AddComponent<CrashKonijn.Goap.Runtime.GoapActionProvider>();
                    CrashKonijn.Agent.Runtime.AgentBehaviour agent =
                        bot.gameObject.AddComponent<CrashKonijn.Agent.Runtime.AgentBehaviour>();

                    // Bidirektionales Wiring: agent.ActionProvider-Setter setzt auch
                    // provider.Receiver = agent, was den ValidateSetup()-Check befriedigt.
                    agent.ActionProvider = provider;
                }

                // AgentType setzen (kann sich bei Teamwechsel aendern)
                provider.AgentType = isSeeker ? goapSetup.SeekerAgentType : goapSetup.HiderAgentType;

                // Ziel anfordern — nur das Goal das im jeweiligen AgentType registriert ist
                if (isSeeker)
                {
                    provider.RequestGoal<HuntPlayerGoal>();
                }
                else
                {
                    provider.RequestGoal<SurviveGoal>();
                }

                activated++;
            }

            string filterStr = teamFilter.HasValue ? $" (Filter={teamFilter.Value})" : "";
            Debug.Log($"[RoundFlow] {activated} Bots GOAP-Ziele aktiviert{filterStr}.");
        }

        /// <summary>
        /// Deaktiviert GOAP-Ziele auf allen gespawnten Bots.
        /// Stoppt die laufende Aktion und loescht das aktuelle Ziel,
        /// damit Bots zwischen den Runden idle bleiben.
        /// Setzt ausserdem den Checkpoint-Fortschritt zurueck.
        /// </summary>
        internal void DeactivateBotGoals()
        {
            if (GameState.AIBotSpawner == null)
            {
                return;
            }

            int deactivated = 0;
            foreach (NetworkObject bot in GameState.AIBotSpawner.SpawnedBots)
            {
                if (bot == null || !bot.IsSpawned)
                {
                    continue;
                }

                CrashKonijn.Goap.Runtime.GoapActionProvider provider =
                    bot.GetComponent<CrashKonijn.Goap.Runtime.GoapActionProvider>();
                CrashKonijn.Agent.Runtime.AgentBehaviour agent =
                    bot.GetComponent<CrashKonijn.Agent.Runtime.AgentBehaviour>();

                if (agent != null)
                {
                    agent.StopAction(false);
                }

                if (provider != null)
                {
                    provider.ClearGoal();
                    deactivated++;
                }

                // Checkpoint-Fortschritt zuruecksetzen fuer neue Runde
                Game.Characters.Server.ServerAICharacter serverAI =
                    bot.GetComponent<Game.Characters.Server.ServerAICharacter>();
                serverAI?.AIController?.ResetCheckpointProgress();
            }

            Debug.Log($"[RoundFlow] {deactivated} Bots GOAP-Ziele deaktiviert.");
        }

        /// <summary>
        /// Gibt die Gesamtzahl aller Spieler zurueck (menschliche Clients + AI-Bots).
        /// </summary>
        internal int GetTotalPlayerCount()
        {
            int humanCount = GameState.NetworkManager.ConnectedClientsIds.Count;
            int botCount = GameState.AIBotSpawner != null ? GameState.AIBotSpawner.SpawnedBotCount : 0;
            return humanCount + botCount;
        }

        /// <summary>
        /// Aktualisiert playersConnected-NetworkVariable und MinPlayersReached-Flag
        /// unter Beruecksichtigung von menschlichen Clients und AI-Bots.
        /// </summary>
        internal void UpdatePlayerCounts()
        {
            int total = GetTotalPlayerCount();
            GameState.playersConnected.Value = total;
            MinPlayersReached = total >= GameState.MinPlayers;
        }

        /// <summary>
        /// Wird von NetworkedGameState.OnNetworkSpawn aufgerufen.
        /// Initialisiert States und betritt den LoadingState.
        /// </summary>
        internal void Initialize(NetworkedGameState gameState)
        {
            GameState = gameState;

            if (!m_Initialized)
            {
                List<RoundFlowState> states = new()
                {
                    LoadingState,
                    WaitingForReadyState,
                    WarmupState,
                    StartingRoundState,
                    RunningState,
                    SwitchingMapState
                };
                InitializeStates(states, LoadingState);
                m_Initialized = true;
            }

            UpdatePlayerCounts();
            ClientsReadyForRound.Clear();
            MatchElapsedTime = 0f;
            ChangeState(LoadingState);
        }

        // ----- Event-Delegation: aktualisiert shared data, dann delegiert an State -----

        /// <summary>Delegiert eine Map-Ladephase an den aktuellen State.</summary>
        internal void OnMapLoadPhase(MapLoadPhase phase)
        {
            m_CurrentState?.OnMapLoadPhase(phase);
        }

        /// <summary>Registriert einen Client als bereit und delegiert an den aktuellen State.</summary>
        internal void OnClientReadyForRound(ulong clientId)
        {
            ClientsReadyForRound.Add(clientId);
            m_CurrentState?.OnClientReadyForRound();
        }

        /// <summary>Aktualisiert MinPlayers-Flag und delegiert an den aktuellen State.</summary>
        internal void OnMinPlayersReached()
        {
            UpdatePlayerCounts();
            m_CurrentState?.OnMinPlayersReached();
        }

        /// <summary>Aktualisiert Spieleranzahl und delegiert an den aktuellen State.</summary>
        internal void OnClientConnected()
        {
            UpdatePlayerCounts();
            m_CurrentState?.OnClientConnected();
        }

        /// <summary>Aktualisiert Spieleranzahl, entfernt Ready-Eintrag und delegiert an den aktuellen State.</summary>
        internal void OnClientDisconnected()
        {
            ClientsReadyForRound.IntersectWith(GameState.NetworkManager.ConnectedClientsIds);
            UpdatePlayerCounts();
            m_CurrentState?.OnClientDisconnected();
        }

        /// <summary>Delegiert eine Runden-Restart-Anforderung an den aktuellen State.</summary>
        internal void OnRoundRestartRequested(float delaySeconds)
        {
            m_CurrentState?.OnRoundRestartRequested(delaySeconds);
        }

        // ----- Hilfsmethoden fuer States -----

        /// <summary>
        /// Prueft ob nach Rundenende ein Map-Wechsel stattfinden soll.
        /// Regeln:
        /// - roundlimit > 0 und erreicht → Map wechseln.
        /// - roundlimit == 0 und timelimit > 0 und MatchElapsedTime >= timelimit → Map wechseln.
        /// - roundlimit == 0 und timelimit == 0 → nie wechseln (unendlich).
        /// </summary>
        internal bool ShouldSwitchMap()
        {
            GametypeManager gametypeManager = ServiceLocator.Get<GametypeManager>();
            if (gametypeManager == null)
            {
                return false;
            }

            // Rundenlimit gesetzt und erreicht → Map wechseln
            if (gametypeManager.IsRoundLimitReached())
            {
                Debug.Log($"[RoundFlow] Round limit reached ({gametypeManager.GetCurrentRound()}/{gametypeManager.GetRoundLimit()}) → switch map");
                return true;
            }

            // Rundenlimit == 0: Timelimit entscheidet
            int roundLimit = gametypeManager.GetRoundLimit();
            if (roundLimit == 0)
            {
                int timelimit = gametypeManager.GetTimelimit();
                if (timelimit > 0 && MatchElapsedTime >= timelimit)
                {
                    Debug.Log($"[RoundFlow] Timelimit reached ({MatchElapsedTime:F0}s / {timelimit}s) → switch map");
                    return true;
                }

                // timelimit == 0 → unendlich, nie wechseln
                return false;
            }

            return false;
        }

        /// <summary>
        /// Prueft ob alle verbundenen Clients "Gameplay sichtbar" gemeldet haben.
        /// </summary>
        internal bool AreAllConnectedClientsReady()
        {
            if (GameState.NetworkManager.ConnectedClientsIds.Count < GameState.MinPlayers)
            {
                return false;
            }

            foreach (ulong clientId in GameState.NetworkManager.ConnectedClientsIds)
            {
                if (!ClientsReadyForRound.Contains(clientId))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Respawnt alle verbundenen Spieler auf neue Spawn-Points.
        /// </summary>
        internal void RespawnAllConnectedPlayers()
        {
            foreach (ulong clientId in GameState.NetworkManager.ConnectedClientsIds)
            {
                NetworkObject playerObject = GameState.NetworkManager.SpawnManager.GetPlayerNetworkObject(clientId);
                if (playerObject == null)
                {
                    continue;
                }

                if (playerObject.TryGetComponent(out NetworkedPlayerCharacter playerCharacter))
                {
                    // Invoke als Fallback weil der Analyzer die Methode sporadisch nicht aufloest.
                    playerCharacter.Invoke("RespawnAtNextSpawnPoint", 0f);
                }
            }
        }

        /// <summary>
        /// Sperrt oder entsperrt die Bewegung aller verbundenen Spieler auf dem Server.
        /// </summary>
        internal void SetAllPlayersMovementLocked(bool locked)
        {
            foreach (ulong clientId in GameState.NetworkManager.ConnectedClientsIds)
            {
                NetworkObject playerObject = GameState.NetworkManager.SpawnManager.GetPlayerNetworkObject(clientId);
                if (playerObject == null)
                {
                    continue;
                }

                if (playerObject.TryGetComponent(out NetworkedPlayerCharacter playerCharacter))
                {
                    playerCharacter.SetMovementLocked(locked);
                }
            }
        }

        /// <summary>
        /// Sperrt oder entsperrt die Bewegung eines bestimmten Teams auf dem Server.
        /// </summary>
        internal void SetTeamMovementLocked(uint teamId, bool locked)
        {
            foreach (ulong clientId in GameState.NetworkManager.ConnectedClientsIds)
            {
                NetworkObject playerObject = GameState.NetworkManager.SpawnManager.GetPlayerNetworkObject(clientId);
                if (playerObject == null)
                {
                    continue;
                }

                if (playerObject.TryGetComponent(out NetworkedPlayerCharacter playerCharacter))
                {
                    NetworkedCharacterState charState = playerObject.GetComponent<NetworkedCharacterState>();
                    if (charState != null && charState.TeamId == teamId)
                    {
                        playerCharacter.SetMovementLocked(locked);
                    }
                }
            }
        }

        /// <summary>
        /// Zaehlt die aktuelle Teamverteilung aller verbundenen Spieler.
        /// </summary>
        internal (int redCount, int blueCount) CountTeams()
        {
            int red = 0;
            int blue = 0;

            foreach (ulong clientId in GameState.NetworkManager.ConnectedClientsIds)
            {
                NetworkObject obj = GameState.NetworkManager.SpawnManager.GetPlayerNetworkObject(clientId);
                if (obj == null || !obj.TryGetComponent(out NetworkedCharacterState state))
                {
                    continue;
                }

                GametypeTeam team = (GametypeTeam)state.TeamId;
                if (team == GametypeTeam.Red)
                {
                    red++;
                }
                else if (team == GametypeTeam.Blue)
                {
                    blue++;
                }
            }

            // AI-Bots zaehlen (sind server-owned, nicht in GetPlayerNetworkObject)
            if (GameState.AIBotSpawner != null)
            {
                foreach (NetworkObject bot in GameState.AIBotSpawner.SpawnedBots)
                {
                    if (bot == null || !bot.IsSpawned || !bot.TryGetComponent(out NetworkedCharacterState botState))
                    {
                        continue;
                    }

                    GametypeTeam botTeam = (GametypeTeam)botState.TeamId;
                    if (botTeam == GametypeTeam.Red)
                    {
                        red++;
                    }
                    else if (botTeam == GametypeTeam.Blue)
                    {
                        blue++;
                    }
                }
            }

            return (red, blue);
        }
    }

    // =====================================================================
    //  Concrete States
    // =====================================================================

    /// <summary>
    /// State: Map wird geladen/gebaut.
    /// Wartet auf MapLoadPhase.Complete um in WaitingForReady zu wechseln.
    /// Despawnt vorhandene AI-Bots vor dem Laden einer neuen Map.
    /// </summary>
    internal sealed class RoundFlowLoadingState : RoundFlowState
    {
        public override void Enter()
        {
            Debug.Log("[RoundFlow] Enter LoadingState");
            Manager.ClientsReadyForRound.Clear();
            GameState.matchCountdown.Value = 0;

            // AI-Bots despawnen bevor die neue Map geladen wird
            if (GameState.AIBotSpawner != null)
            {
                GameState.AIBotSpawner.DespawnAllBots();
            }
        }

        public override void Exit() { }

        internal override void OnMapLoadPhase(MapLoadPhase phase)
        {
            if (phase == MapLoadPhase.Complete)
            {
                Manager.ChangeState(Manager.WaitingForReadyState);
            }
        }
    }

    /// <summary>
    /// State: Map ist geladen, es wird auf alle Client-Ready-Signale und MinPlayers gewartet.
    /// Prueft zusaetzlich die gametype-spezifischen Team-Anforderungen (z.B. HideAndSeek: mind. 1 Seeker + 1 Hider).
    /// Setzt waitingForPlayers-NetworkVariable fuer Client-UI ("Waiting for players...").
    /// Wechselt zu WarmupState sobald alle Bedingungen erfuellt sind.
    /// </summary>
    internal sealed class RoundFlowWaitingForReadyState : RoundFlowState
    {
        public override void Enter()
        {
            Debug.Log("[RoundFlow] Enter WaitingForReadyState");

            GameState.waitingForPlayers.Value = true;

            // Checkpoints aus geladener Map an AIBotController uebergeben (statisch, fuer alle Bots)
            Vector3[] checkpoints = ExtractCheckpointsFromMap();
            AIBotController.SetCheckpoints(checkpoints);

            // NavMesh zur Laufzeit baken (Map-Geometrie mit Collidern ist zu diesem Zeitpunkt geladen)
            BakeRuntimeNavMesh();

            // Checkpoint-Erreichbarkeit auf dem NavMesh validieren
            AIBotController.ValidateCheckpointsOnNavMesh();

            // AI-Bots spawnen (erstmalig) oder respawnen (nach Rundenwechsel)
            if (GameState.AIBotSpawner != null)
            {
                Debug.Log($"[RoundFlow] AIBotSpawner vorhanden, SpawnedBotCount={GameState.AIBotSpawner.SpawnedBotCount}");
                if (GameState.AIBotSpawner.SpawnedBotCount == 0)
                {
                    // Callback abonnieren fuer den Fall dass Addressable-Laden async ist
                    GameState.AIBotSpawner.OnInitialBotsSpawned += OnBotsSpawned;
                    GameState.AIBotSpawner.SpawnInitialBots();
                    Debug.Log($"[RoundFlow] Nach SpawnInitialBots: SpawnedBotCount={GameState.AIBotSpawner.SpawnedBotCount}");
                }
                else
                {
                    GameState.AIBotSpawner.RespawnAllBots();
                }
            }
            else
            {
                Debug.LogWarning("[RoundFlow] AIBotSpawner ist NULL!");
            }

            // Host-Mode: Server-Client hat kein Loading-Overlay, gilt sofort als bereit.
            if (GameState.IsHost)
            {
                Manager.ClientsReadyForRound.Add(GameState.NetworkManager.LocalClientId);
            }

            TryStartMatch();
        }

        public override void Exit()
        {
            GameState.waitingForPlayers.Value = false;

            // Callback abmelden
            if (GameState.AIBotSpawner != null)
            {
                GameState.AIBotSpawner.OnInitialBotsSpawned -= OnBotsSpawned;
            }
        }

        /// <summary>
        /// Wird aufgerufen wenn AI-Bots nach dem async Addressable-Laden gespawnt wurden.
        /// Prueft erneut ob das Match gestartet werden kann.
        /// </summary>
        void OnBotsSpawned()
        {
            Debug.Log("[RoundFlow] AI-Bots gespawnt — aktualisiere Spielerzahlen und pruefe Match-Start...");
            Manager.UpdatePlayerCounts();

            TryStartMatch();
        }

        internal override void OnClientReadyForRound()
        {
            TryStartMatch();
        }

        internal override void OnMinPlayersReached()
        {
            TryStartMatch();
        }

        internal override void OnClientConnected()
        {
            TryStartMatch();
        }

        void TryStartMatch()
        {
            if (!Manager.MinPlayersReached)
            {
                return;
            }

            if (!Manager.AreAllConnectedClientsReady())
            {
                return;
            }

            // Gametype-spezifische Team-Anforderungen pruefen (z.B. min. 1 Seeker + 1 Hider)
            GametypeManager gametypeManager = ServiceLocator.Get<GametypeManager>();
            if (gametypeManager != null)
            {
                (int redCount, int blueCount) = Manager.CountTeams();
                if (!gametypeManager.AreTeamsReady(redCount, blueCount))
                {
                    Debug.Log($"[RoundFlow] Teams nicht bereit: Rot={redCount}, Blau={blueCount}. Warte auf weitere Spieler...");
                    return;
                }
            }

            Manager.ChangeState(Manager.WarmupState);
        }

        /// <summary>
        /// Baked ein NavMesh zur Laufzeit basierend auf der geladenen Map-Geometrie.
        /// Erstellt ein temporaeres GameObject mit NavMeshSurface, baked, und zerstoert es wieder.
        /// Das NavMeshData bleibt im Speicher und wird von NavMesh.CalculatePath() genutzt.
        /// </summary>
        static void BakeRuntimeNavMesh()
        {
            GameObject navMeshGO = new("RuntimeNavMesh");
            NavMeshSurface surface = navMeshGO.AddComponent<NavMeshSurface>();

            // Alle aktiven Objekte erfassen, Physik-Collider als Quelle nutzen
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;

            surface.BuildNavMesh();

            Debug.Log("[AI·NavMesh] Runtime NavMesh gebaked.");
        }

        /// <summary>
        /// Durchsucht die aktive Szene nach GameObjects die als Navigations-Checkpoints dienen.
        /// Erkennt Objekte anhand des Namens-Praefixes: target_.
        /// Sortiert alphabetisch nach Name.
        /// </summary>
        static Vector3[] ExtractCheckpointsFromMap()
        {
            List<(string name, Vector3 position)> entries = new();
            Transform[] allTransforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);

            for (int i = 0; i < allTransforms.Length; i++)
            {
                string name = allTransforms[i].name;
                if (name.StartsWith("target_"))
                {
                    entries.Add((name, allTransforms[i].position));
                }
            }

            if (entries.Count == 0)
            {
                Debug.Log("[RoundFlow] 0 Checkpoints aus Map extrahiert.");
                return null;
            }

            // Sortierung: alphabetisch nach Name.
            entries.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));

            Vector3[] sorted = new Vector3[entries.Count];
            for (int i = 0; i < entries.Count; i++)
            {
                sorted[i] = entries[i].position;
            }

            Debug.Log($"[RoundFlow] {sorted.Length} Checkpoints aus Map extrahiert (target_location_ only). " +
                      $"Erste: {entries[0].name}, Letzte: {entries[entries.Count - 1].name}");
            return sorted;
        }
    }

    /// <summary>
    /// State: Genug Spieler vorhanden — Warmup-Countdown laeuft ("Round start in 10, 9, ...").
    /// Liest g_warmup aus ServerConfiguration (Default 10s).
    /// Beim Ablauf: Respawnt alle Spieler an Spawn-Points und wechselt zu StartingRoundState (3, 2, 1, GO).
    /// Bei Spieler-Disconnect unter Team-Anforderungen zurueck zu WaitingForReady.
    /// </summary>
    internal sealed class RoundFlowWarmupState : RoundFlowState
    {
        Coroutine m_WarmupRoutine;

        public override void Enter()
        {
            Debug.Log("[RoundFlow] Enter WarmupState");

            ServerConfigurationLoader configLoader = ServiceLocator.Get<ServerConfigurationLoader>();
            uint warmupSeconds = 10;
            if (configLoader?.Configuration != null && configLoader.Configuration.g_warmup > 0)
            {
                warmupSeconds = (uint)configLoader.Configuration.g_warmup;
            }

            GameState.warmupCountdown.Value = warmupSeconds;
            m_WarmupRoutine = Manager.StartCoroutine(WarmupCountdown());
        }

        public override void Exit()
        {
            if (m_WarmupRoutine != null)
            {
                Manager.StopCoroutine(m_WarmupRoutine);
                m_WarmupRoutine = null;
            }

            GameState.warmupCountdown.Value = 0;
        }

        internal override void OnClientDisconnected()
        {
            // Team-Anforderungen erneut pruefen bei Disconnect
            GametypeManager gametypeManager = ServiceLocator.Get<GametypeManager>();
            if (gametypeManager != null)
            {
                (int redCount, int blueCount) = Manager.CountTeams();
                if (!gametypeManager.AreTeamsReady(redCount, blueCount))
                {
                    Debug.Log("[RoundFlow] Warmup abgebrochen: Team-Anforderungen nicht mehr erfuellt.");
                    Manager.ChangeState(Manager.WaitingForReadyState);
                    return;
                }
            }

            if (!Manager.MinPlayersReached)
            {
                Manager.ChangeState(Manager.WaitingForReadyState);
            }
        }

        IEnumerator WarmupCountdown()
        {
            while (GameState.warmupCountdown.Value > 0)
            {
                yield return CoroutinesHelper.OneSecond;

                // Bedingungen waehrend Countdown erneut pruefen
                if (!Manager.MinPlayersReached)
                {
                    Manager.ChangeState(Manager.WaitingForReadyState);
                    yield break;
                }

                GameState.warmupCountdown.Value--;
            }

            m_WarmupRoutine = null;

            // Alle Spieler und Bots an Spawn-Points teleportieren
            Manager.RespawnAllConnectedPlayers();
            if (GameState.AIBotSpawner != null)
            {
                GameState.AIBotSpawner.RespawnAllBots();
            }

            Manager.ChangeState(Manager.StartingRoundState);
        }
    }

    /// <summary>
    /// State: Alle bereit — sichtbarer 3, 2, 1 Countdown vor dem Match-Start.
    /// Broadcastet RoundStarting an Clients (Input deaktivieren, Countdown-UI zeigen).
    /// Bei Spieler-Disconnect unter MinPlayers zurueck zu WaitingForReady.
    /// </summary>
    internal sealed class RoundFlowStartingRoundState : RoundFlowState
    {
        Coroutine m_CountdownRoutine;

        public override void Enter()
        {
            Debug.Log("[RoundFlow] Enter StartingRoundState");

            // Server-seitig: Alle Spieler Movement-Lock setzen (3,2,1-Countdown)
            Manager.SetAllPlayersMovementLocked(true);

            GameState.roundStartCountdown.Value = 3;
            GameState.BroadcastRoundStarting();
            m_CountdownRoutine = Manager.StartCoroutine(CountdownThenStart());
        }

        public override void Exit()
        {
            if (m_CountdownRoutine != null)
            {
                Manager.StopCoroutine(m_CountdownRoutine);
                m_CountdownRoutine = null;
            }

            GameState.roundStartCountdown.Value = 0;
        }

        internal override void OnClientDisconnected()
        {
            if (!Manager.MinPlayersReached || !Manager.AreAllConnectedClientsReady())
            {
                Manager.ChangeState(Manager.WaitingForReadyState);
            }
        }

        IEnumerator CountdownThenStart()
        {
            // 3 -> 2 -> 1 -> GO (0)
            while (GameState.roundStartCountdown.Value > 0)
            {
                yield return CoroutinesHelper.OneSecond;

                if (!Manager.MinPlayersReached || !Manager.AreAllConnectedClientsReady())
                {
                    Manager.ChangeState(Manager.WaitingForReadyState);
                    yield break;
                }

                GameState.roundStartCountdown.Value--;
            }

            m_CountdownRoutine = null;
            Manager.ChangeState(Manager.RunningState);
        }
    }

    /// <summary>
    /// State: Match-Countdown laeuft, Runde aktiv.
    /// Startet den Countdown und broadcastet Match-Start an alle Clients.
    /// Ruft jeden Frame OnRunFrame auf dem GametypeManager auf und synchronisiert die Phase.
    /// Wechselt zu SwitchingMap wenn Countdown abgelaufen oder Gametype RoundRestart anfordert.
    /// </summary>
    internal sealed class RoundFlowRunningState : RoundFlowState
    {
        Coroutine m_CountdownRoutine;
        Coroutine m_GameLoopRoutine;
        bool m_RoundRestartPending;
        float m_RoundRestartDelay;

        public override void Enter()
        {
            Debug.Log("[RoundFlow] Enter RunningState");
            m_RoundRestartPending = false;

            GametypeManager gametypeManager = ServiceLocator.Get<GametypeManager>();

            // Teamgroessen an Gametype melden (z.B. AliveHiderCount initialisieren)
            if (gametypeManager != null)
            {
                (int redCount, int blueCount) = Manager.CountTeams();
                gametypeManager.InitializeRoundState(redCount, blueCount);
            }

            // GametypeManager ueber Rundenstart informieren
            gametypeManager?.OnRoundStart();

            // Runden-Info an Clients synchronisieren
            if (gametypeManager != null)
            {
                GameState.currentRound.Value = gametypeManager.GetCurrentRound();
                GameState.roundLimit.Value = gametypeManager.GetRoundLimit();
            }

            // Initiale Phase synchronisieren
            if (gametypeManager != null)
            {
                GameState.gametypePhase.Value = gametypeManager.GetCurrentPhase();
            }

            GameState.BroadcastMatchStarted();

            // Server-seitig: Alle Spieler entsperren (Hider duerfen sofort laufen).
            // Fuer HideAndSeek: Seeker bleiben gesperrt waehrend Hiding-Phase.
            Manager.SetAllPlayersMovementLocked(false);

            string activeGametype = GameState.activeGametypeId.Value.ToString();
            if (activeGametype == "hideandseek" && gametypeManager != null
                && gametypeManager.GetCurrentPhase() == (int)HideAndSeekPhase.Hiding)
            {
                // Seeker (Blue) bleiben gesperrt bis Seeking-Phase beginnt
                Manager.SetTeamMovementLocked((uint)GametypeTeam.Blue, true);
                Debug.Log("[RoundFlow] HideAndSeek: Seeker movement locked during Hiding phase");

                // Nur Hider-Bots aktivieren (Seeker-Bots starten erst in Seeking-Phase)
                Manager.ActivateBotGoals(GametypeTeam.Red);
            }
            else
            {
                // Kein HideAndSeek oder keine Hiding-Phase: alle Bots sofort aktivieren
                Manager.ActivateBotGoals();
            }

            m_CountdownRoutine = Manager.StartCoroutine(RunCountdown());
            m_GameLoopRoutine = Manager.StartCoroutine(GameLoop());
        }

        public override void Exit()
        {
            if (m_CountdownRoutine != null)
            {
                Manager.StopCoroutine(m_CountdownRoutine);
                m_CountdownRoutine = null;
            }

            if (m_GameLoopRoutine != null)
            {
                Manager.StopCoroutine(m_GameLoopRoutine);
                m_GameLoopRoutine = null;
            }

            // GOAP-Ziele deaktivieren — Bots sollen zwischen den Runden idle sein
            Manager.DeactivateBotGoals();

            // GametypeManager ueber Rundenende informieren
            GametypeManager gametypeManager = ServiceLocator.Get<GametypeManager>();
            gametypeManager?.OnRoundEnd();

            // Phase zuruecksetzen
            GameState.gametypePhase.Value = 0;
        }

        internal override void OnRoundRestartRequested(float delaySeconds)
        {
            if (m_RoundRestartPending)
            {
                return;
            }

            m_RoundRestartPending = true;
            m_RoundRestartDelay = delaySeconds;
            Debug.Log($"[RoundFlow] Round restart requested, delay={delaySeconds}s");
        }

        /// <summary>
        /// Game-Loop: Ruft jeden Frame OnRunFrame auf dem GametypeManager auf,
        /// synchronisiert die gametype-spezifische Phase und reagiert auf Restart-Anforderungen.
        /// </summary>
        IEnumerator GameLoop()
        {
            GametypeManager gametypeManager = ServiceLocator.Get<GametypeManager>();
            int lastPhase = gametypeManager?.GetCurrentPhase() ?? 0;
            uint lastPhaseTime = 0;

            while (true)
            {
                yield return null;

                if (gametypeManager != null)
                {
                    gametypeManager.OnRunFrame(Time.deltaTime);

                    // Phase synchronisieren wenn geaendert
                    int currentPhase = gametypeManager.GetCurrentPhase();
                    if (currentPhase != lastPhase)
                    {
                        GameState.gametypePhase.Value = currentPhase;
                        Debug.Log($"[RoundFlow] Gametype phase changed to {currentPhase}");

                        // HideAndSeek: Seeker entsperren wenn Seeking-Phase beginnt
                        if (lastPhase == (int)HideAndSeekPhase.Hiding
                            && currentPhase == (int)HideAndSeekPhase.Seeking)
                        {
                            Manager.SetTeamMovementLocked((uint)GametypeTeam.Blue, false);
                            Debug.Log("[RoundFlow] HideAndSeek: Seeker movement unlocked — Seeking phase");

                            // Seeker-Bots jetzt aktivieren (GOAP-Planung startet erst jetzt)
                            Manager.ActivateBotGoals(GametypeTeam.Blue);
                        }

                        lastPhase = currentPhase;
                    }

                    // Phasen-Restzeit synchronisieren (auf ganze Sekunden gerundet, nur bei Aenderung)
                    uint currentPhaseTime = (uint)Mathf.CeilToInt(gametypeManager.GetPhaseTimeRemaining());
                    if (currentPhaseTime != lastPhaseTime)
                    {
                        GameState.phaseTimeRemaining.Value = currentPhaseTime;
                        lastPhaseTime = currentPhaseTime;
                    }
                }

                // Gametype hat Runden-Restart angefordert (z.B. alle Hider eliminiert)
                if (m_RoundRestartPending)
                {
                    yield return new WaitForSeconds(m_RoundRestartDelay);
                    m_GameLoopRoutine = null;

                    // Pruefen ob Map gewechselt werden soll
                    if (Manager.ShouldSwitchMap())
                    {
                        Manager.ChangeState(Manager.SwitchingMapState);
                        yield break;
                    }

                    Manager.ChangeState(Manager.WaitingForReadyState);
                    yield break;
                }
            }
        }

        IEnumerator RunCountdown()
        {
            // Countdown-Wert vom GametypeManager (Server-Config: timelimit / roundtimelimit)
            GametypeManager gametypeManager = ServiceLocator.Get<GametypeManager>();
            uint countdownValue = gametypeManager != null
                ? gametypeManager.GetRoundTimeLimit()
                : 300;

            GameState.matchCountdown.Value = countdownValue;

            while (GameState.matchCountdown.Value > 0)
            {
                yield return CoroutinesHelper.OneSecond;
                GameState.matchCountdown.Value--;

                // Match-Zeit kumulieren (fuer timelimit-basiertes Map-Switching)
                Manager.MatchElapsedTime += 1f;
            }

            m_CountdownRoutine = null;

            // Zeit abgelaufen: Gametype fragen was passiert
            if (gametypeManager != null)
            {
                GametypeEventResult result = gametypeManager.OnTimeExpired();

                // Team-Score-Deltas auf NetworkedGameState anwenden
                GameState.ApplyGametypeResult(result);

                if (result.RestartRound)
                {
                    Debug.Log($"[RoundFlow] Time expired: {result.BroadcastMessage}");
                    yield return new WaitForSeconds(result.RestartDelaySeconds);

                    // Pruefen ob Map gewechselt werden soll
                    if (Manager.ShouldSwitchMap())
                    {
                        Manager.ChangeState(Manager.SwitchingMapState);
                        yield break;
                    }

                    // Naechste Runde auf gleicher Map
                    Manager.ChangeState(Manager.WaitingForReadyState);
                    yield break;
                }
            }

            Manager.ChangeState(Manager.SwitchingMapState);
        }
    }

    /// <summary>
    /// State: Match beendet, Map-Switch-Countdown laeuft.
    /// Zeigt naechste Map an, zaehlt von 5 runter, laedt dann die naechste Map.
    /// MapLoader feuert MapLoadPhase.Started => Transition zurueck zu LoadingState.
    /// </summary>
    internal sealed class RoundFlowSwitchingMapState : RoundFlowState
    {
        Coroutine m_SwitchRoutine;

        public override void Enter()
        {
            Debug.Log("[RoundFlow] Enter SwitchingMapState");
            GameState.BroadcastMatchEnded();
            m_SwitchRoutine = Manager.StartCoroutine(MapSwitchCountdown());
        }

        public override void Exit()
        {
            if (m_SwitchRoutine != null)
            {
                Manager.StopCoroutine(m_SwitchRoutine);
                m_SwitchRoutine = null;
            }
        }

        internal override void OnMapLoadPhase(MapLoadPhase phase)
        {
            if (phase == MapLoadPhase.Started)
            {
                Manager.ChangeState(Manager.LoadingState);
            }
        }

        IEnumerator MapSwitchCountdown()
        {
            string currentMap = GameState.currentMapName.Value.ToString();

            // Map-Rotation aus Server-Config (sv_mapRotation)
            ServerConfigurationLoader configLoader = ServiceLocator.Get<ServerConfigurationLoader>();
            string nextMap = GetNextMapFromRotation(configLoader, currentMap);

            Debug.Log($"[RoundFlow] Map-Rotation: {currentMap} -> {nextMap}");

            GameState.nextMapName.Value = new FixedString128Bytes(nextMap);
            GameState.mapSwitchCountdown.Value = 5;

            while (GameState.mapSwitchCountdown.Value > 0)
            {
                yield return CoroutinesHelper.OneSecond;
                GameState.mapSwitchCountdown.Value--;
            }

            m_SwitchRoutine = null;
            GameState.currentMapName.Value = new FixedString128Bytes(nextMap);
        }

        /// <summary>
        /// Ermittelt die naechste Map aus sv_mapRotation der Server-Config (Round-Robin).
        /// Fallback: aktuelle Map wiederholen.
        /// </summary>
        static string GetNextMapFromRotation(ServerConfigurationLoader configLoader, string currentMap)
        {
            string[] rotation = configLoader?.Configuration?.sv_mapRotation;
            if (rotation == null || rotation.Length == 0)
            {
                return currentMap;
            }

            int currentIndex = System.Array.IndexOf(rotation, currentMap);
            int nextIndex = (currentIndex + 1) % rotation.Length;
            return rotation[nextIndex];
        }
    }
}
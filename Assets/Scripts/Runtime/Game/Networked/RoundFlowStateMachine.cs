using System.Collections;
using System.Collections.Generic;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.Core;
using Tolik.RemakeSoF.Runtime.DataManagement;
using Tolik.RemakeSoF.Runtime.Game.Characters.Networked;
using Tolik.RemakeSoF.Runtime.Management.MapManagement;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

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
        internal readonly RoundFlowStartingRoundState StartingRoundState = new();
        internal readonly RoundFlowRunningState RunningState = new();
        internal readonly RoundFlowSwitchingMapState SwitchingMapState = new();

        /// <summary>Referenz auf das NetworkedGameState fuer Netzwerk-Operationen.</summary>
        internal NetworkedGameState GameState { get; private set; }

        /// <summary>Ob die MinPlayers-Schwelle aktuell erreicht ist.</summary>
        internal bool MinPlayersReached;

        /// <summary>Client-IDs die fuer die aktuelle Runde "Gameplay sichtbar" gemeldet haben.</summary>
        internal readonly HashSet<ulong> ClientsReadyForRound = new();

        /// <summary>Delay in Sekunden zwischen "alle bereit" und Match-Start.</summary>
        internal const float RoundStartDelaySeconds = 3.1f;

        /// <summary>Name des aktuell aktiven States (Debug/HUD).</summary>
        internal string CurrentStateName => m_CurrentState?.GetType().Name ?? "None";

        bool m_Initialized;

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
                    StartingRoundState,
                    RunningState,
                    SwitchingMapState
                };
                InitializeStates(states, LoadingState);
                m_Initialized = true;
            }

            MinPlayersReached = GameState.NetworkManager.ConnectedClientsIds.Count >= GameState.MinPlayers;
            ClientsReadyForRound.Clear();
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
            MinPlayersReached = true;
            m_CurrentState?.OnMinPlayersReached();
        }

        /// <summary>Aktualisiert Spieleranzahl und delegiert an den aktuellen State.</summary>
        internal void OnClientConnected()
        {
            GameState.playersConnected.Value = GameState.NetworkManager.ConnectedClientsIds.Count;
            MinPlayersReached = GameState.NetworkManager.ConnectedClientsIds.Count >= GameState.MinPlayers;
            m_CurrentState?.OnClientConnected();
        }

        /// <summary>Aktualisiert Spieleranzahl, entfernt Ready-Eintrag und delegiert an den aktuellen State.</summary>
        internal void OnClientDisconnected()
        {
            ClientsReadyForRound.IntersectWith(GameState.NetworkManager.ConnectedClientsIds);
            GameState.playersConnected.Value = GameState.NetworkManager.ConnectedClientsIds.Count;
            MinPlayersReached = GameState.NetworkManager.ConnectedClientsIds.Count >= GameState.MinPlayers;
            m_CurrentState?.OnClientDisconnected();
        }

        // ----- Hilfsmethoden fuer States -----

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
    }

    // =====================================================================
    //  Concrete States
    // =====================================================================

    /// <summary>
    /// State: Map wird geladen/gebaut.
    /// Wartet auf MapLoadPhase.Complete um in WaitingForReady zu wechseln.
    /// </summary>
    internal sealed class RoundFlowLoadingState : RoundFlowState
    {
        public override void Enter()
        {
            Debug.Log("[RoundFlow] Enter LoadingState");
            Manager.ClientsReadyForRound.Clear();
            GameState.matchCountdown.Value = 0;
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
    /// Respawnt Spieler beim Eintritt. Wechselt zu StartingRound sobald alles bereit.
    /// </summary>
    internal sealed class RoundFlowWaitingForReadyState : RoundFlowState
    {
        public override void Enter()
        {
            Debug.Log("[RoundFlow] Enter WaitingForReadyState");
            Manager.RespawnAllConnectedPlayers();

            // Host-Mode: Server-Client hat kein Loading-Overlay, gilt sofort als bereit.
            if (GameState.IsHost)
            {
                Manager.ClientsReadyForRound.Add(GameState.NetworkManager.LocalClientId);
            }

            TryStartMatch();
        }

        public override void Exit() { }

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

            Manager.ChangeState(Manager.StartingRoundState);
        }
    }

    /// <summary>
    /// State: Alle bereit — kurzer Delay vor dem Match-Start.
    /// Bei Spieler-Disconnect unter MinPlayers zurueck zu WaitingForReady.
    /// </summary>
    internal sealed class RoundFlowStartingRoundState : RoundFlowState
    {
        Coroutine m_DelayRoutine;

        public override void Enter()
        {
            Debug.Log("[RoundFlow] Enter StartingRoundState");
            m_DelayRoutine = Manager.StartCoroutine(DelayThenStart());
        }

        public override void Exit()
        {
            if (m_DelayRoutine != null)
            {
                Manager.StopCoroutine(m_DelayRoutine);
                m_DelayRoutine = null;
            }
        }

        internal override void OnClientDisconnected()
        {
            if (!Manager.MinPlayersReached || !Manager.AreAllConnectedClientsReady())
            {
                Manager.ChangeState(Manager.WaitingForReadyState);
            }
        }

        IEnumerator DelayThenStart()
        {
            yield return new WaitForSeconds(RoundFlowStateMachine.RoundStartDelaySeconds);
            m_DelayRoutine = null;

            if (!Manager.MinPlayersReached || !Manager.AreAllConnectedClientsReady())
            {
                Manager.ChangeState(Manager.WaitingForReadyState);
                yield break;
            }

            Manager.ChangeState(Manager.RunningState);
        }
    }

    /// <summary>
    /// State: Match-Countdown laeuft, Runde aktiv.
    /// Startet den Countdown und broadcastet Match-Start an alle Clients.
    /// Wechselt zu SwitchingMap wenn Countdown abgelaufen.
    /// </summary>
    internal sealed class RoundFlowRunningState : RoundFlowState
    {
        Coroutine m_CountdownRoutine;

        public override void Enter()
        {
            Debug.Log("[RoundFlow] Enter RunningState");
            GameState.BroadcastMatchStarted();
            m_CountdownRoutine = Manager.StartCoroutine(RunCountdown());
        }

        public override void Exit()
        {
            if (m_CountdownRoutine != null)
            {
                Manager.StopCoroutine(m_CountdownRoutine);
                m_CountdownRoutine = null;
            }
        }

        IEnumerator RunCountdown()
        {
            MapDataLoader mapDataLoader = ServiceLocator.Get<MapDataLoader>();
            string mapId = GameState.currentMapName.Value.ToString();
            MapDefinition mapDef = mapDataLoader?.GetByMapId(mapId);
            uint countdownValue = mapDef != null && mapDef.countdownStartValue > 0
                ? mapDef.countdownStartValue
                : 300;

            GameState.matchCountdown.Value = countdownValue;

            while (GameState.matchCountdown.Value > 0)
            {
                yield return CoroutinesHelper.OneSecond;
                GameState.matchCountdown.Value--;
            }

            m_CountdownRoutine = null;
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
            MapDataLoader mapDataLoader = ServiceLocator.Get<MapDataLoader>();
            string currentMap = GameState.currentMapName.Value.ToString();
            string nextMap = mapDataLoader.GetNextMapId(currentMap);

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
    }
}
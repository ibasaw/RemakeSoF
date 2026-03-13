using System;
using UnityEngine;
using Tolik.RemakeSoF.Runtime.Game.Characters.Client;

namespace Tolik.RemakeSoF.Runtime
{
    internal class MatchController : Controller<GameApplication>
    {
        MatchView View => App.View.Match;

        /// <summary>
        /// Intervall in Sekunden zwischen FPS-Updates in der View.
        /// </summary>
        private const float k_FpsUpdateInterval = 0.5f;

        /// <summary>
        /// Intervall in Sekunden zwischen Debug-HUD-Updates.
        /// </summary>
        private const float k_DebugHudUpdateInterval = 0.05f;

        /// <summary>
        /// Akkumulierte Zeit seit letztem FPS-Update.
        /// </summary>
        private float m_FpsTimer;

        /// <summary>
        /// Anzahl gerendeter Frames seit letztem FPS-Update.
        /// </summary>
        private int m_FrameCount;

        /// <summary>
        /// Akkumulierte Zeit seit letztem Debug-HUD-Update.
        /// </summary>
        private float m_DebugHudTimer;

        void Awake()
        {
            App.Model.Countdown.OnValueChanged += OnCountdownChanged;
            App.Model.PlayersConnected.OnValueChanged += OnPlayersConnectedChanged;
            App.Model.NetworkedGameState.OnMatchStarted += OnMatchStarted;
            App.Model.NetworkedGameState.OnMatchEnded += OnMatchEnded;
            Debug.Log("MatchController Awake: Listeners added to NetworkedGameState events.");
        }

        void OnDestroy()
        {
            RemoveListeners();
            Debug.Log("MatchController OnDestroy: Listeners removed from NetworkedGameState events.");
        }

        internal override void RemoveListeners()
        {
            App.Model.Countdown.OnValueChanged -= OnCountdownChanged;
            App.Model.PlayersConnected.OnValueChanged -= OnPlayersConnectedChanged;
            App.Model.NetworkedGameState.OnMatchStarted -= OnMatchStarted;
            App.Model.NetworkedGameState.OnMatchEnded -= OnMatchEnded;
        }

        void OnCountdownChanged(uint previousValue, uint newValue)
        {
            View.OnCountdownChanged(newValue);
        }

        void OnPlayersConnectedChanged(int previousValue, int newValue)
        {
            View.OnPlayersConnectedChanged(newValue);
        }

        void OnMatchEnded()
        {
            Broadcast(new EndMatchEvent());
            Debug.Log("[MatchController] Match ended, broadcasting EndMatchEvent.");
        }

        void OnMatchStarted()
        {
            Broadcast(new StartMatchEvent());
            Debug.Log("[MatchController] Match started, broadcasting StartMatchEvent.");
        }

        /// <summary>
        /// FPS berechnen und View aktualisieren in regelmäßigen Intervallen.
        /// Debug-HUD mit Spielerdaten aktualisieren.
        /// </summary>
        private void Update()
        {
            m_FrameCount++;
            m_FpsTimer += Time.unscaledDeltaTime;

            if (m_FpsTimer >= k_FpsUpdateInterval)
            {
                float fps = m_FrameCount / m_FpsTimer;
                View.OnFpsChanged(fps);
                m_FrameCount = 0;
                m_FpsTimer = 0f;
            }

            m_DebugHudTimer += Time.unscaledDeltaTime;
            if (m_DebugHudTimer >= k_DebugHudUpdateInterval)
            {
                m_DebugHudTimer = 0f;
                UpdateDebugHud();
            }
        }

        /// <summary>
        /// Liest aktuelle Daten vom PlayerCharacter und aktualisiert das Debug-HUD.
        /// </summary>
        private void UpdateDebugHud()
        {
            ClientPlayerCharacter player = App.Model.PlayerCharacter;
            if (player == null)
            {
                return;
            }

            View.UpdateDebugHud(
                player.IsGrounded,
                player.IsAttacking,
                player.IsCrouching,
                player.HorizontalSpeed,
                player.VerticalSpeed,
                player.transform.position,
                player.CurrentAirtime,
                player.CurrentJumpPhaseAirtime,
                player.CurrentFallPhaseAirtime,
                player.CurrentJumpHeight,
                player.CurrentFallHeight,
                player.CurrentAirDistanceHoriz,
                player.CurrentAirDistanceVert,
                player.FullAirtime,
                player.FullJumpPhaseAirtime,
                player.FullFallPhaseAirtime,
                player.FullJumpHeight,
                player.FullFallHeight,
                player.FullAirDistanceHoriz,
                player.FullAirDistanceVert
            );

            View.UpdateBhopDisplay(
                player.HorizontalSpeed,
                player.BhopChainCount,
                player.BhopChainPeakSpeed,
                player.BhopChainDistance,
                player.LastBhopChainCount,
                player.LastBhopChainPeakSpeed,
                player.LastBhopChainDistance
            );
        }
    }
}

using System;
using UnityEngine;

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
        /// Akkumulierte Zeit seit letztem FPS-Update.
        /// </summary>
        private float m_FpsTimer;

        /// <summary>
        /// Anzahl gerendeter Frames seit letztem FPS-Update.
        /// </summary>
        private int m_FrameCount;

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
        }
    }
}

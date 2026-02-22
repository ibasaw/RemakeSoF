using System;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime
{
    internal class MatchController : Controller<GameApplication>
    {
        MatchView View => App.View.Match;

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
    }
}

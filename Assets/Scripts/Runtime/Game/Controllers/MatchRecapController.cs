using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.DataManagement;
using Tolik.RemakeSoF.Runtime.Game.Networked;
using Unity.Collections;

namespace Tolik.RemakeSoF.Runtime
{
    /// <summary>
    /// Controller fuer den Match-Recap-Screen.
    /// Zeigt Game Over an und den Countdown bis zum Map-Wechsel.
    /// </summary>
    internal class MatchRecapController : Controller<GameApplication>
    {
        MatchRecapView View => App.View.MatchRecap;

        void Awake()
        {
            AddListener<EndMatchEvent>(OnClientEndMatch);
        }

        void OnDestroy()
        {
            RemoveListeners();
        }

        internal override void RemoveListeners()
        {
            RemoveListener<EndMatchEvent>(OnClientEndMatch);

            NetworkedGameState gameState = App.Model.NetworkedGameState;
            gameState.mapSwitchCountdown.OnValueChanged -= OnMapSwitchCountdownChanged;
            gameState.nextMapName.OnValueChanged -= OnNextMapNameChanged;
        }

        void OnClientEndMatch(EndMatchEvent evt)
        {
            App.Model.PlayerCharacter.SetInputsActive(false);
            View.OnClientEndMatch();

            // Auf Map-Wechsel-Countdown reagieren
            NetworkedGameState gameState = App.Model.NetworkedGameState;
            gameState.mapSwitchCountdown.OnValueChanged += OnMapSwitchCountdownChanged;
            gameState.nextMapName.OnValueChanged += OnNextMapNameChanged;
        }

        void OnMapSwitchCountdownChanged(uint previousValue, uint newValue)
        {
            UpdateCountdownDisplay(newValue);
        }

        void OnNextMapNameChanged(FixedString128Bytes previousValue, FixedString128Bytes newValue)
        {
            string nextMapId = newValue.ToString();
            if (string.IsNullOrEmpty(nextMapId))
            {
                return;
            }

            uint countdown = App.Model.NetworkedGameState.mapSwitchCountdown.Value;
            UpdateCountdownDisplay(countdown);
        }

        /// <summary>
        /// Holt den Anzeigenamen der naechsten Map und aktualisiert die View.
        /// </summary>
        void UpdateCountdownDisplay(uint secondsRemaining)
        {
            string nextMapId = App.Model.NetworkedGameState.nextMapName.Value.ToString();
            if (string.IsNullOrEmpty(nextMapId))
            {
                return;
            }

            // Map-Anzeigename aus MapDataLoader holen
            MapDataLoader mapDataLoader = ServiceLocator.Get<MapDataLoader>();
            MapDefinition mapDef = mapDataLoader?.GetByMapId(nextMapId);
            string displayName = mapDef != null ? mapDef.mapName : nextMapId;

            View.UpdateMapSwitchCountdown(displayName, secondsRemaining);
        }
    }
}

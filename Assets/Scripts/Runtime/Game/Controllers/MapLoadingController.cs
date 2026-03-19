using Tolik.RemakeSoF.Runtime.DataManagement;
using Tolik.RemakeSoF.Runtime.Management.MapManagement;

namespace Tolik.RemakeSoF.Runtime
{
    /// <summary>
    /// Controller für die Map-Ladefortschrittsanzeige.
    /// Abonniert MapLoadProgress-Events von NetworkedGameState und
    /// aktualisiert die MapLoadingView.
    /// Versteckt Game-Views bis die Map geladen ist.
    /// </summary>
    internal class MapLoadingController : Controller<GameApplication>
    {
        MapLoadingView View => App.View.MapLoading;

        void Awake()
        {
            App.Model.NetworkedGameState.OnMapLoadProgress += OnMapLoadProgress;
            App.Model.NetworkedGameState.OnMapChangeStarting += OnMapChangeStarting;
            View.OnReadyToHide += OnLoadingScreenReadyToHide;

            // Loading-Screen sichtbar, Game-Views versteckt bis Map geladen
            View.Show();
            App.View.Match.Hide();
            App.View.Menu.Hide();
            App.View.MatchRecap.Hide();
        }

        void OnDestroy()
        {
            RemoveListeners();
        }

        internal override void RemoveListeners()
        {
            App.Model.NetworkedGameState.OnMapLoadProgress -= OnMapLoadProgress;
            App.Model.NetworkedGameState.OnMapChangeStarting -= OnMapChangeStarting;
            View.OnReadyToHide -= OnLoadingScreenReadyToHide;
        }

        /// <summary>
        /// Wird vor einem Map-Wechsel aufgerufen.
        /// Zeigt den Loading-Screen mit LevelShot-Hintergrund und Map-Name.
        /// </summary>
        void OnMapChangeStarting(MapDefinition mapDef)
        {
            // Show() vor SetMapInfo() damit OnEnable die VisualElements abfragt
            View.Show();
            View.SetMapInfo(mapDef);
            App.View.Match.Hide();
            App.View.MatchRecap.Hide();
        }

        /// <summary>
        /// Wird bei jeder Map-Ladephase aufgerufen.
        /// Aktualisiert die View. Verstecken uebernimmt OnReadyToHide nach Mindestzeit.
        /// </summary>
        void OnMapLoadProgress(MapLoadPhase phase)
        {
            View.OnProgressChanged(phase);

            // Bei Fehler sofort verstecken
            if (phase == MapLoadPhase.Failed)
            {
                View.Hide();
                App.View.Match.Show();
            }
        }

        /// <summary>
        /// Wird aufgerufen wenn die View nach Mindestanzeigedauer und Animation bereit ist.
        /// </summary>
        void OnLoadingScreenReadyToHide()
        {
            View.Hide();
            App.View.Match.Show();
            App.Model.NetworkedGameState.NotifyGameplayVisibleOnClient();
        }
    }
}

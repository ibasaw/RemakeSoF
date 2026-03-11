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
        }

        /// <summary>
        /// Wird bei jeder Map-Ladephase aufgerufen.
        /// Aktualisiert die View und versteckt sie bei Complete/Failed.
        /// </summary>
        void OnMapLoadProgress(MapLoadPhase phase)
        {
            View.OnProgressChanged(phase);

            if (phase == MapLoadPhase.Complete || phase == MapLoadPhase.Failed)
            {
                View.Hide();
                App.View.Match.Show();
            }
        }
    }
}

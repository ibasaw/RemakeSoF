using UnityEngine;

namespace Tolik.RemakeSoF.Runtime
{
    /// <summary>
    /// Main View of the <see cref="GameApplication"></see>
    /// </summary>
    public class GameView : View<GameApplication>
    {
        internal MatchView Match => m_MatchView;

        [SerializeField]
        MatchView m_MatchView;
        
        internal GameMenuView Menu => m_GameMenuView;

        [SerializeField]
        GameMenuView m_GameMenuView;

        internal MatchRecapView MatchRecap => m_MatchRecapView;

        [SerializeField]
        MatchRecapView m_MatchRecapView;

        internal MapLoadingView MapLoading => m_MapLoadingView;

        [SerializeField]
        MapLoadingView m_MapLoadingView;

        internal ScoreboardView Scoreboard => m_ScoreboardView;

        [SerializeField]
        ScoreboardView m_ScoreboardView;
    }
}

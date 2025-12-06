using UnityEngine;

namespace Unity.DedicatedGameServerSample.Runtime
{
    /// <summary>
    /// Main view of the <see cref="MetagameApplication"></see>
    /// </summary>
    public class MetagameView : View<MetagameApplication>
    {
        internal MainMenuView MainMenu => m_MainMenuView;

        [SerializeField]
        MainMenuView m_MainMenuView;

        internal MatchmakerView Matchmaker => m_MatchmakerView;

        [SerializeField]
        MatchmakerView m_MatchmakerView;

        internal DirectIPView DirectIP => m_DirectIPView;

        [SerializeField]
        DirectIPView m_DirectIPView;

        internal ClientConnectingView ClientConnecting => m_ClientConnectingView;

        [SerializeField]
        ClientConnectingView m_ClientConnectingView;

        [SerializeField]
        LoginView m_LoginView;

        internal LoginView LoginView => m_LoginView;

        [SerializeField]
        RegisterView m_RegisterView;

        internal RegisterView RegisterView => m_RegisterView;

        [SerializeField]
        ConsoleView m_ConsoleView;

        internal ConsoleView ConsoleView => m_ConsoleView;

        [SerializeField]
        LoadoutView m_LoadoutView;

        internal LoadoutView LoadoutView => m_LoadoutView;

        [SerializeField]
        OptionsView m_OptionsView;

        internal OptionsView OptionsView => m_OptionsView;

        [SerializeField]
        CreateServerView m_CreateServerView;
        internal CreateServerView CreateServerView => m_CreateServerView;

        [SerializeField]
        JoinServerView m_JoinServerView;
        internal JoinServerView JoinServerView => m_JoinServerView;

        [SerializeField]
        LogoutView m_LogoutView;
        internal LogoutView LogoutView => m_LogoutView;
    }
}

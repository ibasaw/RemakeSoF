using System;
using UnityEngine.UIElements;
namespace Unity.DedicatedGameServerSample.Runtime
{
    [UnityEngine.RequireComponent(typeof(UIDocument))]
    internal class LoadoutView : View<MetagameApplication>
    {
        UIDocument m_UIDocument;

        Label m_PlayerNameLabel;

        Label m_PlayerIdLabel;

        void Awake()
        {
            m_UIDocument = GetComponent<UIDocument>();
        }

        void OnEnable()
        {
            var root = m_UIDocument.rootVisualElement;

            m_PlayerNameLabel = root.Q<Label>("playerName");
            m_PlayerIdLabel = root.Q<Label>("playerId");

            m_PlayerNameLabel.text = App.Model.PlayerData.PlayerName;
            m_PlayerIdLabel.text = App.Model.PlayerData.PlayerId;
        }
    }
}
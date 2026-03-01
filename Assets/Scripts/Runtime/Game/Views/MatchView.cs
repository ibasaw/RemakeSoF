using System;
using UnityEngine.UIElements;

namespace Tolik.RemakeSoF.Runtime
{
    [UnityEngine.RequireComponent(typeof(UIDocument))]

    internal class MatchView : View<GameApplication>
    {
        UIDocument m_UIDocument;
        Label m_TimerLabel;
        Label m_PlayersConnectedLabel;
        Label m_FpsLabel;

        void Awake()
        {
            m_UIDocument = GetComponent<UIDocument>();
        }

        void OnEnable()
        {
            var root = m_UIDocument.rootVisualElement;
            m_TimerLabel = root.Query<Label>("timerLabel");
            m_PlayersConnectedLabel = root.Query<Label>("playersConnectedLabel");
            m_FpsLabel = root.Query<Label>("fpsLabel");
        }

        internal void OnCountdownChanged(uint newValue)
        {
            m_TimerLabel.text = string.Format("{0:D2}:{1:D2}", newValue / 60, newValue % 60);
        }

        internal void OnPlayersConnectedChanged(int newValue)
        {
            m_PlayersConnectedLabel.text = $"Players connected: {newValue}";
        }

        internal void OnFpsChanged(float newValue)
        {
            m_FpsLabel.text = $"FPS: {newValue:F1}";
        }
    }
}

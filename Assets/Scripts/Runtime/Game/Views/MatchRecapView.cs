using UnityEngine.UIElements;

namespace Tolik.RemakeSoF.Runtime
{
    /// <summary>
    /// Zeigt den Match-Recap-Screen an.
    /// Zeigt "Game Over", die naechste Map und einen Countdown bis zum Map-Wechsel.
    /// </summary>
    internal class MatchRecapView : View<GameApplication>
    {
        Label m_ResultLabel;
        Label m_MapSwitchLabel;
        UIDocument m_UIDocument;

        void Awake()
        {
            m_UIDocument = GetComponent<UIDocument>();
        }

        void OnEnable()
        {
            VisualElement root = m_UIDocument.rootVisualElement;
            m_ResultLabel = root.Q<Label>("resultLabel");
            m_MapSwitchLabel = root.Q<Label>("mapSwitchLabel");
        }

        /// <summary>
        /// Zeigt den Recap-Screen mit "Game Over" an.
        /// </summary>
        internal void OnClientEndMatch()
        {
            gameObject.SetActive(true);
            m_ResultLabel.text = "Game Over!";
            if (m_MapSwitchLabel != null)
            {
                m_MapSwitchLabel.text = "";
            }
        }

        /// <summary>
        /// Aktualisiert die Anzeige fuer den Map-Wechsel-Countdown.
        /// </summary>
        /// <param name="nextMapDisplayName">Anzeigename der naechsten Map.</param>
        /// <param name="secondsRemaining">Verbleibende Sekunden bis zum Wechsel.</param>
        internal void UpdateMapSwitchCountdown(string nextMapDisplayName, uint secondsRemaining)
        {
            if (m_MapSwitchLabel == null)
            {
                return;
            }

            if (secondsRemaining > 0)
            {
                m_MapSwitchLabel.text = $"Next map: {nextMapDisplayName} in {secondsRemaining}...";
            }
            else
            {
                m_MapSwitchLabel.text = $"Loading {nextMapDisplayName}...";
            }
        }
    }
}

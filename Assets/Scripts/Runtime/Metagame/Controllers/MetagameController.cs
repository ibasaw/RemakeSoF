using UnityEngine;

namespace Tolik.RemakeSoF.Runtime
{
    /// <summary>
    /// Globaler controller koordiniert die Metagame-Application. 
    /// Handelt alle globale Events wie ApplicationQuitEvent und SceneTransitionEvent.
    /// </summary>
    public class MetagameController : Controller<MetagameApplication>
    {
        void Awake()
        {
            AddListener<MatchEnteredEvent>(OnMatchEntered);
        }

        void OnDestroy()
        {
            RemoveListeners();
        }

        internal override void RemoveListeners()
        {
            RemoveListener<MatchEnteredEvent>(OnMatchEntered);
        }

        void OnMatchEntered(MatchEnteredEvent evt)
        {
            DisableViewsAndListeners();
        }

        void DisableViewsAndListeners()
        {
            for (int i = 0; i < App.View.transform.childCount; i++)
            {
                App.View.transform.GetChild(i).gameObject.SetActive(false);
            }
        }
    }
}

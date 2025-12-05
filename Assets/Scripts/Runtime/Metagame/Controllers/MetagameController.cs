using UnityEngine;

namespace Unity.DedicatedGameServerSample.Runtime
{
    /// <summary>
    /// Globaler controller koordiniert die Metagame-Application. 
    /// Handelt alle globale Events wie ApplicationQuitEvent und SceneTransitionEvent.
    /// </summary>
    public class MetagameController : Controller<MetagameApplication>
    {
        void Awake()
        {
            AddListener<PlayerSignedIn>(OnPlayerSignedIn);
            AddListener<MatchEnteredEvent>(OnMatchEntered);
        }

        void OnDestroy()
        {
            RemoveListeners();
        }

        internal override void RemoveListeners()
        {
            RemoveListener<PlayerSignedIn>(OnPlayerSignedIn);
            RemoveListener<MatchEnteredEvent>(OnMatchEntered);
        }

        void OnPlayerSignedIn(PlayerSignedIn evt)
        {
            if (evt.Success)
            {
                Debug.Log($"Player signed in with id {evt.PlayerId}");
            }
            else
            {
                Debug.Log("Player did not sign in");
            }
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

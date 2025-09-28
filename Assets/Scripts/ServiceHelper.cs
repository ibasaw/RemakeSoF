using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections;

public class ServiceHelper : MonoBehaviour
{
    private static bool serversStarted = false;

    void Start()
    {
        if (serversStarted) return;
        serversStarted = true;

        Debug.Log("[ServiceHelper] Starte Auth und Game Server...");
        StartAuthServer();
        StartGameServer();
        
        StartCoroutine(LoadLoginSceneWhenServersReady());
    }

    private void StartAuthServer()
    {
        if (FindObjectOfType<AuthServer>() == null)
        {
            GameObject authObj = new GameObject("AuthServer");
            authObj.AddComponent<AuthServer>();
            DontDestroyOnLoad(authObj);
        }
    }

    private void StartGameServer()
    {
        if (FindObjectOfType<GameServer>() == null)
        {
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("[ServiceHelper] Kein NetworkManager in der Scene!");
                return;
            }

            GameObject gs = new GameObject("GameServer");
            gs.AddComponent<GameServer>();
            DontDestroyOnLoad(gs);
        }
    }

    private IEnumerator LoadLoginSceneWhenServersReady()
    {
        AuthServer auth = null;
        GameServer game = null;

        // Warten, bis beide Server existieren
        while (auth == null || game == null)
        {
            auth = FindObjectOfType<AuthServer>();
            game = FindObjectOfType<GameServer>();
            yield return null;
        }

        // Warten, bis beide Server laufen
        while (!auth.IsRunning() || !game.IsRunning())
        {
            Debug.Log("[ServiceHelper] Warte auf Auth und Game Server...");
            yield return null;
        }

        Debug.Log("[ServiceHelper] Beide Server bereit - Lade LoginScene...");
        SceneManager.LoadScene("LoginScene");
    }
}

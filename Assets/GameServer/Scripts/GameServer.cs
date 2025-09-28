using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP; // Unity Transport

public class GameServer : MonoBehaviour
{
    public int port = 7777; // Netcode Port
    private bool isRunning = false;

    void Awake()
    {
        if (FindObjectsOfType<GameServer>().Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        // Server starten im Start() statt Awake(), um NetworkManager.Singleton sicher zu haben
    }

    void Start()
    {
        StartServer();
    }

    private void StartServer()
    {
        if (isRunning) return;

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[GameServer] NetworkManager fehlt in der Scene!");
            return;
        }

        // Starte Netcode Server
        NetworkManager.Singleton.StartServer();
        isRunning = true;
        Debug.Log("[GameServer] NetworkManager Server gestartet!");
        // IP und Port aus Transport auslesen (Unity Transport)
        var utp = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;
        if (utp != null)
        {
            Debug.Log($"[GameServer] Listening on {utp.ConnectionData.Address}:{utp.ConnectionData.Port}");
        }
        else
        {
            Debug.Log("[GameServer] Server gestartet, aber kein UnityTransport gefunden");
        }
    }

    void OnApplicationQuit()
    {
        if (isRunning && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
            isRunning = false;
            Debug.Log("[GameServer] Server gestoppt");
        }
    }

    public bool IsRunning()
    {
        return isRunning && NetworkManager.Singleton != null;
    }

    public void StopServer()
    {
        if (isRunning && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
            isRunning = false;
            Debug.Log("[GameServer] Server gestoppt");
        }
    }

}

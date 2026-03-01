using UnityEngine;
using Unity.Multiplayer;


#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Unity.Multiplayer.Tools.NetStatsMonitor;
#endif

namespace Tolik.RemakeSoF.Runtime.ApplicationLifecycle
{
    /// <summary>
    /// Initializes and manages the RuntimeNetStatsMonitor for Development builds.
    /// This MonoBehaviour is automatically instantiated in the Editor and persists across scenes.
    /// </summary>
    [MultiplayerRoleRestricted]
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public class NetworkStatsMonitorInitializer : MonoBehaviour
    {
        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            InitializeNetworkStatsMonitor();
        }

        void InitializeNetworkStatsMonitor()
        {
            if (!TryGetComponent<RuntimeNetStatsMonitor>(out var monitor))
            {
                Debug.LogWarning("[NetworkStatsMonitorInitializer] No RuntimeNetStatsMonitor found on this GameObject.");
                return;
            }

            monitor.Visible = true;
            Debug.Log("[NetworkStatsMonitorInitializer] RuntimeNetStatsMonitor initialized and made visible.");
        }
    }
#else
    // In Production builds: Dummy class to avoid compilation errors
    public class NetworkStatsMonitorInitializer : MonoBehaviour
    {
        void Awake()
        {
            // NetworkStatsMonitor ist in Production nicht verfügbar
            Destroy(gameObject);
        }
    }
#endif
}

using Tolik.RemakeSoF.Runtime.Game.Camera;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Management.MapManagement
{
    /// <summary>
    /// Treibt das Proximity-Culling fuer Map-Lichter.
    /// Aktiviert nur Lichter in der Naehe des Spielers, deaktiviert entfernte.
    /// Wird vom MapLoader nach ApplyLights() an die Map-Instanz angehaengt.
    /// Forward+ hat kein Per-Object-Lichtlimit, aber hunderte aktive Lights
    /// kosten trotzdem GPU-Cluster-Evaluierung — dieses Component deaktiviert
    /// entfernte Lichter fuer bessere Performance auf grossen Maps.
    /// </summary>
    public class MapLightProximityCuller : MonoBehaviour
    {
        /// <summary>
        /// Minimale Distanz die der Spieler zuruecklegen muss bevor
        /// die Proximity-Berechnung erneut ausgefuehrt wird (in Metern).
        /// Vermeidet unnoetige Neuberechnung wenn der Spieler still steht.
        /// </summary>
        private const float k_UpdateDistanceThreshold = 3f;

        /// <summary>
        /// Quadrierter Schwellwert (vermeidet Sqrt).
        /// </summary>
        private const float k_UpdateDistanceThresholdSqr =
            k_UpdateDistanceThreshold * k_UpdateDistanceThreshold;

        /// <summary>
        /// Referenz auf den MapLightApplier der die Lichter verwaltet.
        /// </summary>
        private MapLightApplier m_LightApplier;

        /// <summary>
        /// Gecachte Referenz auf den AimCameraController.
        /// Camera.main ist in diesem Projekt immer null (Cinemachine Virtual Cameras,
        /// keine "MainCamera"-getaggte Kamera). Stattdessen wird der
        /// AimCameraController als Positions-Proxy verwendet — derselbe Ansatz
        /// den EffectFactory und FlashbangScreenEffect nutzen.
        /// </summary>
        private AimCameraController m_CameraController;

        /// <summary>
        /// Letzte Position bei der Proximity ausgewertet wurde.
        /// </summary>
        private Vector3 m_LastUpdatePosition;

        /// <summary>
        /// Ob das erste Proximity-Update bereits erfolgt ist.
        /// AimCameraController existiert erst nach Player-Spawn.
        /// Das erste Update wird in LateUpdate nachgeholt.
        /// </summary>
        private bool m_HasInitialUpdate;

        /// <summary>
        /// Initialisiert den Culler mit dem MapLightApplier.
        /// Das erste Proximity-Update erfolgt in LateUpdate sobald
        /// der AimCameraController verfuegbar ist (nach Player-Spawn).
        /// </summary>
        /// <param name="lightApplier">Der MapLightApplier mit den Proximity-Daten.</param>
        public void Initialize(MapLightApplier lightApplier)
        {
            m_LightApplier = lightApplier;
            m_HasInitialUpdate = false;
            m_CameraController = null;
            Debug.Log("[MapLightProximityCuller] Initialisiert. Warte auf AimCameraController...");
        }

        /// <summary>
        /// Prueft ob der Spieler sich weit genug bewegt hat und aktualisiert
        /// die Licht-Sichtbarkeit basierend auf Distanz zum Spieler.
        /// </summary>
        private void LateUpdate()
        {
            if (m_LightApplier == null)
            {
                return;
            }

            // AimCameraController lazy finden (existiert erst nach Player-Spawn).
            if (m_CameraController == null)
            {
                m_CameraController = Object.FindAnyObjectByType<AimCameraController>();
                if (m_CameraController == null)
                {
                    return;
                }
            }

            Vector3 viewerPosition = m_CameraController.transform.position;

            // Erstes Update: Controller ist jetzt verfuegbar (nach Player-Spawn).
            if (!m_HasInitialUpdate)
            {
                m_HasInitialUpdate = true;
                m_LastUpdatePosition = viewerPosition;
                int active = m_LightApplier.UpdateProximity(viewerPosition);
                Debug.Log($"[MapLightProximityCuller] Erstes Update: {active} Lichter im Radius aktiviert " +
                          $"(Viewer bei {viewerPosition}).");
                return;
            }

            // Nur neu berechnen wenn Spieler sich um mehr als k_UpdateDistanceThreshold bewegt hat.
            if ((viewerPosition - m_LastUpdatePosition).sqrMagnitude < k_UpdateDistanceThresholdSqr)
            {
                return;
            }

            m_LastUpdatePosition = viewerPosition;
            m_LightApplier.UpdateProximity(viewerPosition);
        }
    }
}

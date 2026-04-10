using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.AI
{
    /// <summary>
    /// Erzeugt ein Array von Sensor3D-Instanzen in einem Field-of-View-Faecher.
    /// Die Sensoren werden als Children des angegebenen Transforms erstellt und zeigen
    /// in gleichmaessig verteilten Richtungen innerhalb des konfigurierten horizontalen
    /// und vertikalen Oeffnungswinkels. Alle Sensoren werden per Sense() getaktet.
    /// </summary>
    public class SensorArrayGenerator : MonoBehaviour
    {
        [Header("Field of View")]
        [Tooltip("Horizontaler Oeffnungswinkel in Grad (symmetrisch links/rechts).")]
        [SerializeField]
        private float m_HorizontalFOV = 120f;

        [Tooltip("Vertikaler Oeffnungswinkel in Grad (symmetrisch oben/unten).")]
        [SerializeField]
        private float m_VerticalFOV = 40f;

        [Header("Ray Count")]
        [Tooltip("Anzahl horizontaler Strahlen (ungerade = Mittelstrahl).")]
        [SerializeField]
        private int m_HorizontalSteps = 7;

        [Tooltip("Anzahl vertikaler Strahlen.")]
        [SerializeField]
        private int m_VerticalSteps = 5;

        [Header("Sensor Settings")]
        [Tooltip("Maximale Reichweite jedes Sensors.")]
        [SerializeField]
        private float m_MaxDistance = 25f;

        [Tooltip("Layer-Maske fuer alle erzeugten Sensoren (Default, Ground, Player, BrushCollision).")]
        [SerializeField]
        private LayerMask m_SensorLayerMask = (1 << 0) | (1 << 6) | (1 << 7) | (1 << 9);

        [Tooltip("SphereCast-Radius (0 = normaler Raycast).")]
        [SerializeField]
        private float m_SphereRadius = 0.15f;

        [Tooltip("Debug-Zeichnung der Sensoren aktivieren.")]
        [SerializeField]
        private bool m_DebugDraw = true;

        /// <summary>Das erzeugte Sensor-Array. Null bis Generate() aufgerufen wird.</summary>
        public Sensor3D[] Sensors { get; private set; }

        /// <summary>Container-Transform fuer alle Sensoren (Child von sensorRoot, Y-Offset fuer Kopfhoehe).</summary>
        private Transform m_SensorContainer;

        /// <summary>Gesamtanzahl der erzeugten Sensoren (erst nach Generate() > 0).</summary>
        public int SensorCount => Sensors != null ? Sensors.Length : 0;

        /// <summary>Berechnete Gesamtanzahl der Sensoren laut Konfiguration (ohne Generate()-Aufruf noetig).</summary>
        public int TotalSensorCount => m_HorizontalSteps * m_VerticalSteps;

        /// <summary>Layer-Index fuer Player (fuer Sensor-Encoding: Player-Treffer negativ codiert).</summary>
        private const int k_PlayerLayer = 7;

        /// <summary>
        /// Konfiguriert die FOV-Parameter vor dem Generate()-Aufruf.
        /// Ueberschreibt die serialisierten Werte (Prefab-Inspector wird ignoriert).
        /// </summary>
        /// <param name="horizontalFOV">Horizontaler Oeffnungswinkel in Grad.</param>
        /// <param name="verticalFOV">Vertikaler Oeffnungswinkel in Grad.</param>
        public void ConfigureFOV(float horizontalFOV, float verticalFOV)
        {
            m_HorizontalFOV = horizontalFOV;
            m_VerticalFOV = verticalFOV;
        }



        /// <summary>
        /// Erzeugt die Sensor3D-Instanzen als Child-GameObjects des angegebenen Transforms.
        /// Vorherige Sensoren werden zerstoert.
        /// Sensoren werden am VisualRoot (immer korrekte Forward-Richtung) auf Kopfhoehe platziert.
        /// </summary>
        /// <param name="sensorRoot">Das Transform, an das die Sensoren angehaengt werden (z.B. VisualRoot).</param>
        /// <param name="heightOffset">Y-Offset ueber sensorRoot (Kopfhoehe, z.B. cranium.position.y - visualRoot.position.y).</param>
        public void Generate(Transform sensorRoot, float heightOffset = 0f)
        {
            Clear();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Debug-Visualisierung in Development-Builds immer erzwingen
            m_DebugDraw = true;
#endif

            int totalCount = m_HorizontalSteps * m_VerticalSteps;
            Debug.Log($"[AI·Sensor] Generiere {totalCount} Sensoren | Root='{sensorRoot.name}' | H={heightOffset:F2}m | Debug={m_DebugDraw}");
            Sensors = new Sensor3D[totalCount];

            // Container am VisualRoot auf Kopfhoehe erstellen.
            // VisualRoot zeigt immer in korrekter Forward-Richtung (Unity Z-Forward),
            // keine Bone-Rotations-Korrektur noetig (SoF2-Skelett-Problem umgangen).
            GameObject containerGO = new("SensorContainer");
            containerGO.transform.SetParent(sensorRoot, false);
            containerGO.transform.localPosition = new Vector3(0f, heightOffset, 0f);
            containerGO.transform.localRotation = Quaternion.identity;
            m_SensorContainer = containerGO.transform;

            int index = 0;
            for (int v = 0; v < m_VerticalSteps; v++)
            {
                float vertAngle = 0f;
                if (m_VerticalSteps > 1)
                {
                    float vertFraction = (float)v / (m_VerticalSteps - 1);
                    vertAngle = Mathf.Lerp(-m_VerticalFOV * 0.5f, m_VerticalFOV * 0.5f, vertFraction);
                }

                for (int h = 0; h < m_HorizontalSteps; h++)
                {
                    float horAngle = 0f;
                    if (m_HorizontalSteps > 1)
                    {
                        float horFraction = (float)h / (m_HorizontalSteps - 1);
                        horAngle = Mathf.Lerp(-m_HorizontalFOV * 0.5f, m_HorizontalFOV * 0.5f, horFraction);
                    }

                    Vector3 localDirection = Quaternion.Euler(vertAngle, horAngle, 0f) * Vector3.forward;

                    GameObject sensorGO = new("Sensor_H" + h + "_V" + v);
                    sensorGO.transform.SetParent(m_SensorContainer, false);
                    sensorGO.transform.localPosition = Vector3.zero;
                    sensorGO.transform.localRotation = Quaternion.identity;

                    Sensor3D sensor = sensorGO.AddComponent<Sensor3D>();
                    sensor.SetLocalDirection(localDirection);
                    sensor.Configure(m_MaxDistance, m_SensorLayerMask, m_SphereRadius, m_DebugDraw);

                    Sensors[index] = sensor;
                    index++;
                }
            }
        }

        /// <summary>
        /// Fuehrt Sense() auf allen erzeugten Sensoren aus.
        /// Wird vom AIBotController pro Tick aufgerufen.
        /// </summary>
        public void TickAll()
        {
            if (Sensors == null)
            {
                return;
            }

            for (int i = 0; i < Sensors.Length; i++)
            {
                if (Sensors[i] != null)
                {
                    Sensors[i].Sense();
                }
            }
        }

        /// <summary>
        /// Sammelt alle normalisierten Sensor-Distanzen in ein double-Array fuer NN-Input.
        /// </summary>
        /// <param name="buffer">Ziel-Array, muss mindestens SensorCount gross sein.</param>
        /// <param name="startIndex">Startposition im Ziel-Array.</param>
        public void CollectDistances(double[] buffer, int startIndex)
        {
            if (Sensors == null)
            {
                return;
            }

            for (int i = 0; i < Sensors.Length; i++)
            {
                float output = Sensors[i].Output;
                // Player-Layer getroffen → negativ codieren (NN kann Player von Wand unterscheiden)
                if (Sensors[i].HitLayer == k_PlayerLayer)
                {
                    buffer[startIndex + i] = -output;
                }
                else
                {
                    buffer[startIndex + i] = output;
                }
            }
        }

        /// <summary>
        /// Zaehlt Sensoren die Waende/Hindernisse (nicht-Player) innerhalb einer Schwelldistanz treffen.
        /// </summary>
        /// <param name="thresholdMeters">Maximale Distanz fuer "nahe Wand" (z.B. 2m).</param>
        /// <returns>Anzahl Sensoren mit nahem Wand-Treffer.</returns>
        public int CountCloseWallHits(float thresholdMeters)
        {
            if (Sensors == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < Sensors.Length; i++)
            {
                if (Sensors[i] != null && Sensors[i].HitLayer >= 0
                    && Sensors[i].HitLayer != k_PlayerLayer
                    && Sensors[i].RawDistance <= thresholdMeters)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Prueft ob irgendein Sensor einen Spieler (Layer 7) getroffen hat.
        /// </summary>
        public bool AnyPlayerDetected()
        {
            if (Sensors == null)
            {
                return false;
            }

            for (int i = 0; i < Sensors.Length; i++)
            {
                if (Sensors[i] != null && Sensors[i].HitLayer == k_PlayerLayer)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Zerstoert alle erzeugten Sensor-GameObjects.
        /// </summary>
        public void Clear()
        {
            if (Sensors == null)
            {
                return;
            }

            for (int i = 0; i < Sensors.Length; i++)
            {
                if (Sensors[i] != null)
                {
                    Destroy(Sensors[i].gameObject);
                }
            }

            if (m_SensorContainer != null)
            {
                Destroy(m_SensorContainer.gameObject);
                m_SensorContainer = null;
            }

            Sensors = null;
        }

        private void OnDestroy()
        {
            Clear();
        }
    }
}

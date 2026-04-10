using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.AI
{
    /// <summary>
    /// 3D-Raycasting-Sensor fuer AI-Bots.
    /// Misst die Distanz zu Hindernissen in einer bestimmten Richtung relativ zum Owner-Transform.
    /// Unterstuetzt optional SphereCast fuer robustere Erkennung.
    /// Liefert normalisierte Distanz sowie Layer/Tag-Informationen fuer das neuronale Netz.
    /// Debug-Visualisierung per LineRenderer direkt im Game View.
    /// </summary>
    public class Sensor3D : MonoBehaviour
    {
        [Header("Sensor Settings")]
        [SerializeField]
        private LayerMask m_LayerToSense = ~0;

        [SerializeField]
        private float m_MaxDistance = 20f;

        [SerializeField]
        private float m_MinDistance = 0.1f;

        [Header("Direction")]
        [Tooltip("Lokaler Richtungsvektor relativ zum Owner-Transform (wird automatisch normalisiert).")]
        [SerializeField]
        private Vector3 m_LocalDirection = Vector3.forward;

        [Header("SphereCast (optional)")]
        [SerializeField]
        private bool m_UseSphereCast = true;

        [SerializeField]
        private float m_SphereRadius = 0.15f;

        [Header("Debug")]
        [SerializeField]
        private bool m_DebugDraw = false;

        [SerializeField]
        private Color m_DebugHitColor = Color.green;

        [SerializeField]
        private Color m_DebugMissColor = Color.red;

        /// <summary>Gecachter Sprites/Default Shader fuer LineRenderer-Material.</summary>
        private static Shader s_CachedSpritesShader;

        /// <summary>LineRenderer fuer In-Game Debug-Visualisierung.</summary>
        private LineRenderer m_DebugLineRenderer;

        /// <summary>Material fuer den Debug-LineRenderer.</summary>
        private Material m_DebugMaterial;

        /// <summary>Normalisierter Wert [0..1] (0 = ganz nah/minDistance, 1 = maxDistance/nichts getroffen).</summary>
        public float Output { get; private set; } = 1f;

        /// <summary>Ungefilterte Distanz in Unity-Einheiten.</summary>
        public float RawDistance { get; private set; }

        /// <summary>LayerIndex des getroffenen Colliders oder -1 wenn nichts getroffen.</summary>
        public int HitLayer { get; private set; } = -1;

        /// <summary>Tag des getroffenen GameObjects oder null.</summary>
        public string HitTag { get; private set; }

        /// <summary>Referenz auf den getroffenen Collider (null wenn nichts getroffen).</summary>
        public Collider HitCollider { get; private set; }

        /// <summary>Weltrichtung, in die der Sensor aktuell schaut.</summary>
        public Vector3 WorldDirection { get; private set; }

        /// <summary>
        /// Setzt die lokale Sensorrichtung (wird automatisch normalisiert).
        /// Verwendung: SensorArrayGenerator setzt die Richtung per Code.
        /// </summary>
        public void SetLocalDirection(Vector3 direction)
        {
            m_LocalDirection = direction.normalized;
        }

        /// <summary>
        /// Konfiguriert den Sensor per Code (fuer SensorArrayGenerator).
        /// </summary>
        public void Configure(float maxDistance, LayerMask layerMask, float sphereRadius, bool debugDraw)
        {
            m_MaxDistance = maxDistance;
            m_LayerToSense = layerMask;
            m_SphereRadius = sphereRadius;
            m_UseSphereCast = sphereRadius > 0f;
            m_DebugDraw = debugDraw;
        }

        /// <summary>
        /// Fuehrt einen einzelnen Sensor-Tick aus. Wird vom besitzenden System (z.B. ServerAICharacter)
        /// pro Frame aufgerufen, NICHT per FixedUpdate, um Server-Tick-Rate-Kontrolle zu behalten.
        /// </summary>
        public void Sense()
        {
            Vector3 origin = transform.position;
            WorldDirection = transform.TransformDirection(m_LocalDirection.normalized);

            bool didHit;
            RaycastHit hit;

            if (m_UseSphereCast && m_SphereRadius > 0f)
            {
                didHit = Physics.SphereCast(origin, m_SphereRadius, WorldDirection, out hit,
                    m_MaxDistance, m_LayerToSense, QueryTriggerInteraction.Ignore);
            }
            else
            {
                didHit = Physics.Raycast(origin, WorldDirection, out hit,
                    m_MaxDistance, m_LayerToSense, QueryTriggerInteraction.Ignore);
            }

            if (didHit)
            {
                RawDistance = Mathf.Max(hit.distance, m_MinDistance);
                HitLayer = hit.collider != null ? hit.collider.gameObject.layer : -1;
                HitTag = hit.collider != null ? hit.collider.gameObject.tag : null;
                HitCollider = hit.collider;
            }
            else
            {
                RawDistance = m_MaxDistance;
                HitLayer = -1;
                HitTag = null;
                HitCollider = null;
            }

            Output = Mathf.Clamp01(RawDistance / m_MaxDistance);

            if (m_DebugDraw)
            {
                UpdateDebugLineRenderer(origin, origin + WorldDirection * RawDistance, didHit);
            }
        }

        /// <summary>
        /// Aktualisiert den LineRenderer fuer In-Game Debug-Visualisierung.
        /// Erstellt den LineRenderer lazy beim ersten Aufruf.
        /// </summary>
        private void UpdateDebugLineRenderer(Vector3 start, Vector3 end, bool didHit)
        {
            if (m_DebugLineRenderer == null)
            {
                if (s_CachedSpritesShader == null)
                {
                    s_CachedSpritesShader = Shader.Find("Sprites/Default");
                }

                m_DebugLineRenderer = gameObject.AddComponent<LineRenderer>();
                m_DebugMaterial = new Material(s_CachedSpritesShader);
                m_DebugLineRenderer.material = m_DebugMaterial;
                m_DebugLineRenderer.positionCount = 2;
                m_DebugLineRenderer.startWidth = 0.04f;
                m_DebugLineRenderer.endWidth = 0.04f;
                m_DebugLineRenderer.useWorldSpace = true;
                m_DebugLineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                m_DebugLineRenderer.receiveShadows = false;
                m_DebugLineRenderer.allowOcclusionWhenDynamic = false;
            }

            Color color = didHit ? m_DebugHitColor : m_DebugMissColor;
            m_DebugLineRenderer.startColor = color;
            m_DebugLineRenderer.endColor = color;
            m_DebugLineRenderer.SetPosition(0, start);
            m_DebugLineRenderer.SetPosition(1, end);
        }

        /// <summary>
        /// Raeumt den Debug-LineRenderer und das Material auf.
        /// </summary>
        private void OnDestroy()
        {
            if (m_DebugMaterial != null)
            {
                Destroy(m_DebugMaterial);
            }
        }

        /// <summary>
        /// Prueft ob der letzte Hit auf einem bestimmten Layer liegt.
        /// </summary>
        /// <param name="layerName">Name des Layers (z.B. "Player", "Wall").</param>
        /// <returns>True wenn der letzte Hit auf dem angegebenen Layer war.</returns>
        public bool IsHitOnLayer(string layerName)
        {
            return HitLayer == LayerMask.NameToLayer(layerName);
        }

        /// <summary>
        /// Gibt den normalisierten Distanzwert als float-Input fuer das neuronale Netz zurueck.
        /// 0 = nah, 1 = weit/nichts getroffen.
        /// </summary>
        public float GetDistanceNormalized()
        {
            return Output;
        }

        /// <summary>
        /// Gibt 1f zurueck wenn der Hit auf dem angegebenen Layer liegt, sonst 0f.
        /// Nuetzlich als One-Hot-Input fuer das neuronale Netz.
        /// </summary>
        public float GetLayerFlag(string layerName)
        {
            return IsHitOnLayer(layerName) ? 1f : 0f;
        }
    }
}

using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.Characters.Client
{
    /// <summary>
    /// Berechnet Box-Parameter aus Character-Bones (Cranium, Fuesse).
    /// Stellt Box-Daten fuer die SoF2-Physik (BoxCasts / AABB) bereit.
    /// Optionaler Visual-Debug: zeichnet Box + Ground-Check-Disk zur Laufzeit im Editor.
    /// </summary>
    [DisallowMultipleComponent]
    public class ClientColliderSystem : MonoBehaviour
    {
        [Header("Capsule Settings")]
        [SerializeField]
        private float m_CapsuleRadius;

        [SerializeField]
        private float m_CapsuleHeight;

        [SerializeField]
        private Vector3 m_CapsuleCenter;

        [Header("Auto Capsule Sizing")]
        [SerializeField]
        private bool m_AutoSizeCapsule = true;

        [SerializeField]
        private bool m_DynamicCapsuleSizing = true;

        /// <summary>Verwendet feste SoF2-authentische Capsule-Dimensionen statt Bone-basierter Berechnung.</summary>
        [SerializeField]
        private bool m_UseFixedSoF2Size = true;

        [Header("Visual Collider Debug")]
        [SerializeField]
        private bool m_ShowVisualCollider;

        [SerializeField]
        private Color m_ColliderColor = new(0f, 1f, 0f, 0.3f);

        [SerializeField]
        private Material m_ColliderMaterial;

        /// <summary>Dynamisch erstelltes Material fuer Ground-Check-Visualisierung.</summary>
        private Material m_VisualGroundCheckMaterial;

        /// <summary>Basis-Hoehe fuer Standing (vor Crouch-Skalierung).</summary>
        private float m_BaseCapsuleHeight;

        /// <summary>Basis-Radius (unabhaengig von Crouch).</summary>
        private float m_BaseCapsuleRadius;

        /// <summary>Basis-Center fuer Standing.</summary>
        private Vector3 m_BaseCapsuleCenter;

        /// <summary>Lokaler Y-Wert der Fuesse (fuer Ground-Check-Disk Positionierung).</summary>
        private float m_FeetYLocal;

        /// <summary>Breite zwischen linkem und rechtem Fuss.</summary>
        private float m_FeetWidth;

        /// <summary>Minimaler Capsule-Radius (SoF2-proportional).</summary>
        private const float k_MinCapsuleRadius = 0.20f;

        /// <summary>Maximaler Capsule-Radius (verhindert zu breite Capsule bei T-Pose).</summary>
        private const float k_MaxCapsuleRadius = 0.40f;

        /// <summary>SoF2 Standing-Hoehe: 89 Units (-46 bis 43) * 0.0254 m/unit.</summary>
        private const float k_SoF2StandingHeight = 2.2606f;

        /// <summary>SoF2 Crouching-Hoehe: 64 Units (-46 bis 18) * 0.0254 m/unit.</summary>
        private const float k_SoF2CrouchingHeight = 1.6256f;

        /// <summary>SoF2 Capsule-Radius: 15 Units * 0.0254 m/unit.</summary>
        private const float k_SoF2Radius = 0.381f;

        // Visual Collider GameObjects + Components
        private GameObject m_VisualColliderObject;
        private MeshRenderer m_VisualColliderRenderer;
        private MeshFilter m_VisualColliderMeshFilter;

        private GameObject m_VisualGroundCheckObject;
        private MeshRenderer m_VisualGroundCheckRenderer;
        private MeshFilter m_VisualGroundCheckMeshFilter;

        /// <summary>Gecachter Grounded-State fuer Visual-Farbe.</summary>
        private bool m_IsGroundedVisual;

        /// <summary>Physics BoxCollider fuer BoxCast-Detection durch andere Spieler (SoF2 AABB).</summary>
        private BoxCollider m_PhysicsCollider;

        // --- Public Getters ---

        /// <summary>Aktueller Capsule-Radius.</summary>
        public float GetCurrentCapsuleRadius() => m_CapsuleRadius;

        /// <summary>Aktuelle Capsule-Hoehe.</summary>
        public float GetCurrentCapsuleHeight() => m_CapsuleHeight;

        /// <summary>Aktuelles Capsule-Center (lokal).</summary>
        public Vector3 GetCurrentCapsuleCenter() => m_CapsuleCenter;

        /// <summary>
        /// Lokaler Y-Wert der Fuesse relativ zum Player-Transform.
        /// Negativ wenn der Skeleton-Ursprung (Pelvis) ueber den Fuessen liegt.
        /// Wird verwendet um das Visual vertikal zu korrigieren.
        /// </summary>
        public float FeetYLocal => m_FeetYLocal;

        /// <summary>
        /// Physik-BoxCollider fuer temporaeres Deaktivieren waehrend eigener Simulation.
        /// </summary>
        public BoxCollider PhysicsCollider => m_PhysicsCollider;

        /// <summary>
        /// Setzt den Grounded-State fuer die Visual-Debug-Farbe.
        /// Wird von ClientPlayerCharacter pro Frame aufgerufen.
        /// </summary>
        public void SetGroundedState(bool isGrounded)
        {
            m_IsGroundedVisual = isGrounded;
        }

        /// <summary>
        /// Berechnet Capsule-Groesse aus Character-Bones.
        /// Capsule geht von Y=0 (Fuesse) bis Y=Height (Cranium).
        /// Breite basiert auf Fuss-Abstand.
        /// </summary>
        public void CalculateAutoCapsuleSize(
            Transform cranium,
            Transform pelvis,
            Transform leftHandBolt,
            Transform rightHandBolt,
            Transform leftFoot,
            Transform rightFoot)
        {
            if (!m_AutoSizeCapsule)
            {
                return;
            }

            if (m_UseFixedSoF2Size)
            {
                ApplyFixedSoF2Size(false);
                return;
            }

            if (cranium == null)
            {
                Debug.LogWarning("[ClientColliderSystem] Cranium bone nicht zugewiesen! Verwende Standard-Capsule.");
                return;
            }

            // Hoechsten Punkt bestimmen: SkinnedMeshRenderer-Bounds (falls vorhanden) statt Transform-Position.
            // *head_t_0 hat einen SkinnedMeshRenderer dessen bounds.max.y den tatsaechlich gerenderten Scheitelpunkt liefert.
            SkinnedMeshRenderer smr = cranium.GetComponent<SkinnedMeshRenderer>();
            float highestY;
            if (smr != null)
            {
                Vector3 boundsMaxLocal = transform.InverseTransformPoint(smr.bounds.max);
                highestY = boundsMaxLocal.y;
            }
            else
            {
                Vector3 craniumLocal = transform.InverseTransformPoint(cranium.position);
                highestY = craniumLocal.y;
            }

            // Niedrigsten Y-Wert aus Fuessen bestimmen
            float lowestY = 0f;
            if (leftFoot != null && rightFoot != null)
            {
                Vector3 leftLocal = transform.InverseTransformPoint(leftFoot.position);
                Vector3 rightLocal = transform.InverseTransformPoint(rightFoot.position);
                lowestY = Mathf.Min(leftLocal.y, rightLocal.y);
            }
            else if (leftFoot != null)
            {
                lowestY = transform.InverseTransformPoint(leftFoot.position).y;
            }
            else if (rightFoot != null)
            {
                lowestY = transform.InverseTransformPoint(rightFoot.position).y;
            }

            float newHeight = highestY - lowestY;

            // Breite aus SkinnedMeshRenderer-Bounds berechnen (Fuss-Abstand als Fallback)
            float newRadius = CalculateRadiusFromBounds(leftFoot, rightFoot);

            // Fuss-Daten speichern
            m_FeetYLocal = lowestY;
            if (leftFoot != null && rightFoot != null)
            {
                Vector3 leftLocal = transform.InverseTransformPoint(leftFoot.position);
                Vector3 rightLocal = transform.InverseTransformPoint(rightFoot.position);
                Vector3 horizDiff = new(leftLocal.x - rightLocal.x, 0f, leftLocal.z - rightLocal.z);
                m_FeetWidth = horizDiff.magnitude;
            }
            else
            {
                m_FeetWidth = newRadius * 2f;
            }

            // Center bei halber Hoehe (Capsule von 0 bis Height)
            float centerY = newHeight * 0.5f;

            m_BaseCapsuleHeight = newHeight;
            m_BaseCapsuleRadius = newRadius;
            m_BaseCapsuleCenter = new Vector3(0f, centerY, 0f);

            m_CapsuleHeight = newHeight;
            m_CapsuleRadius = newRadius;
            m_CapsuleCenter = m_BaseCapsuleCenter;

            // Physics BoxCollider erstellen/aktualisieren
            UpdatePhysicsCollider();

            // Visual Debug initialisieren (falls aktiviert)
            InitializeVisualCollider();
            InitializeVisualGroundCheck();

            Debug.Log($"[ClientColliderSystem] Auto-sized Capsule — Height: {newHeight:F2}, Radius: {newRadius:F2}, Center: {m_CapsuleCenter}");
        }

        /// <summary>
        /// Setzt Capsule-Groesse fuer Standing / Crouching.
        /// Bei fixem SoF2-Modus werden authentische Dimensionen verwendet.
        /// Anderenfalls Crouch = 60% der Standing-Hoehe.
        /// </summary>
        public void UpdateCapsuleSizeForState(bool isCrouching)
        {
            if (!m_DynamicCapsuleSizing || !m_AutoSizeCapsule)
            {
                return;
            }

            if (m_UseFixedSoF2Size)
            {
                ApplyFixedSoF2Size(isCrouching);
                return;
            }

            if (isCrouching)
            {
                m_CapsuleHeight = m_BaseCapsuleHeight * 0.6f;
                m_CapsuleCenter = new Vector3(0f, m_CapsuleHeight * 0.5f, 0f);
            }
            else
            {
                m_CapsuleHeight = m_BaseCapsuleHeight;
                m_CapsuleCenter = m_BaseCapsuleCenter;
            }

            UpdatePhysicsCollider();
        }

        /// <summary>
        /// Setzt feste SoF2-authentische Capsule-Dimensionen.
        /// Standing: 89 Units = 2.26m Hoehe, 15 Units = 0.381m Radius.
        /// Crouching: 64 Units = 1.63m Hoehe, gleicher Radius.
        /// Quelle: bg_public.h playerMins/playerMaxs + PM_CheckDuck.
        /// </summary>
        private void ApplyFixedSoF2Size(bool isCrouching)
        {
            float height = isCrouching ? k_SoF2CrouchingHeight : k_SoF2StandingHeight;
            float centerY = height * 0.5f;

            m_BaseCapsuleHeight = k_SoF2StandingHeight;
            m_BaseCapsuleRadius = k_SoF2Radius;
            m_BaseCapsuleCenter = new Vector3(0f, k_SoF2StandingHeight * 0.5f, 0f);

            m_CapsuleHeight = height;
            m_CapsuleRadius = k_SoF2Radius;
            m_CapsuleCenter = new Vector3(0f, centerY, 0f);

            m_FeetYLocal = 0f;
            m_FeetWidth = k_SoF2Radius * 2f;

            UpdatePhysicsCollider();
            InitializeVisualCollider();
            InitializeVisualGroundCheck();
        }

        private void LateUpdate()
        {
            if (!m_ShowVisualCollider)
            {
                return;
            }

            UpdateVisualCollider();
            UpdateVisualGroundCheck();
        }

        private void OnDestroy()
        {
            if (m_ColliderMaterial != null)
            {
                Destroy(m_ColliderMaterial);
                m_ColliderMaterial = null;
            }

            if (m_VisualGroundCheckMaterial != null)
            {
                Destroy(m_VisualGroundCheckMaterial);
                m_VisualGroundCheckMaterial = null;
            }

            if (m_VisualColliderObject != null)
            {
                Destroy(m_VisualColliderObject);
            }

            if (m_VisualGroundCheckObject != null)
            {
                Destroy(m_VisualGroundCheckObject);
            }
        }

        // ===================================================================
        // Physics BoxCollider (SoF2 AABB)
        // ===================================================================

        /// <summary>
        /// Erstellt oder aktualisiert den Physics-BoxCollider.
        /// Dieser Collider wird von BoxCasts anderer Spieler erkannt
        /// und ermoeglicht Player-Player Collision (SoF2 AABB).
        /// </summary>
        private void UpdatePhysicsCollider()
        {
            if (m_PhysicsCollider == null)
            {
                m_PhysicsCollider = gameObject.AddComponent<BoxCollider>();
            }

            m_PhysicsCollider.size = new Vector3(m_CapsuleRadius * 2f, m_CapsuleHeight, m_CapsuleRadius * 2f);
            m_PhysicsCollider.center = m_CapsuleCenter;
        }

        // ===================================================================
        // Visual Debug — Box + Ground-Check-Disk
        // ===================================================================

        /// <summary>
        /// Erstellt das Visual-Collider-Mesh-GameObject (Box / SoF2 AABB).
        /// </summary>
        private void InitializeVisualCollider()
        {
            if (!m_ShowVisualCollider)
            {
                return;
            }

            if (m_VisualColliderObject != null)
            {
                return;
            }

            m_VisualColliderObject = new("VisualCollider");
            m_VisualColliderObject.transform.SetParent(transform);
            m_VisualColliderObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            m_VisualColliderObject.transform.localScale = Vector3.one;

            m_VisualColliderMeshFilter = m_VisualColliderObject.AddComponent<MeshFilter>();
            m_VisualColliderRenderer = m_VisualColliderObject.AddComponent<MeshRenderer>();

            m_VisualColliderMeshFilter.mesh = CreateBoxMesh();

            if (m_ColliderMaterial == null)
            {
                Shader shader = Shader.Find("Unlit/Color");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                m_ColliderMaterial = new Material(shader);
                m_ColliderMaterial.color = m_ColliderColor;

                if (shader != null && shader.name == "Standard")
                {
                    m_ColliderMaterial.SetFloat("_Mode", 3);
                    m_ColliderMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    m_ColliderMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    m_ColliderMaterial.SetInt("_ZWrite", 0);
                    m_ColliderMaterial.DisableKeyword("_ALPHATEST_ON");
                    m_ColliderMaterial.EnableKeyword("_ALPHABLEND_ON");
                    m_ColliderMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    m_ColliderMaterial.renderQueue = 3000;
                }
            }

            m_VisualColliderRenderer.material = m_ColliderMaterial;
            m_VisualColliderRenderer.enabled = true;
        }

        /// <summary>
        /// Erstellt das Visual-Ground-Check-Mesh-GameObject (Disk).
        /// </summary>
        private void InitializeVisualGroundCheck()
        {
            if (!m_ShowVisualCollider)
            {
                return;
            }

            if (m_VisualGroundCheckObject != null)
            {
                return;
            }

            m_VisualGroundCheckObject = new("VisualGroundCheck");
            m_VisualGroundCheckObject.transform.SetParent(transform);
            m_VisualGroundCheckObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            m_VisualGroundCheckObject.transform.localScale = Vector3.one;

            m_VisualGroundCheckMeshFilter = m_VisualGroundCheckObject.AddComponent<MeshFilter>();
            m_VisualGroundCheckRenderer = m_VisualGroundCheckObject.AddComponent<MeshRenderer>();

            float groundCheckRadius = m_FeetWidth > 0f ? m_FeetWidth * 0.5f : m_CapsuleRadius;
            m_VisualGroundCheckMeshFilter.mesh = CreateGroundCheckDiskMesh(groundCheckRadius);

            Material groundCheckMat = new(Shader.Find("Unlit/Color"));
            groundCheckMat.color = new Color(1f, 1f, 0f, 0.5f);
            m_VisualGroundCheckRenderer.material = groundCheckMat;
            m_VisualGroundCheckMaterial = groundCheckMat;
            m_VisualGroundCheckRenderer.sortingOrder = 999;
        }

        /// <summary>
        /// Aktualisiert Position, Mesh und Farbe der Visual-Box.
        /// </summary>
        private void UpdateVisualCollider()
        {
            if (m_VisualColliderObject == null)
            {
                return;
            }

            m_VisualColliderObject.SetActive(true);
            m_VisualColliderObject.transform.localPosition = Vector3.zero;

            if (m_VisualColliderMeshFilter != null)
            {
                m_VisualColliderMeshFilter.mesh = CreateBoxMesh();
            }

            if (m_VisualColliderRenderer != null && m_VisualColliderRenderer.material != null)
            {
                Color current = m_IsGroundedVisual ? Color.green : Color.red;
                current.a = m_ColliderColor.a;
                m_VisualColliderRenderer.material.color = current;
            }
        }

        /// <summary>
        /// Aktualisiert Position, Mesh und Farbe der Visual-Ground-Check-Disk.
        /// </summary>
        private void UpdateVisualGroundCheck()
        {
            if (m_VisualGroundCheckObject == null)
            {
                return;
            }

            m_VisualGroundCheckObject.SetActive(true);

            Vector3 groundCheckPos = new(0f, m_FeetYLocal - 0.05f, 0f);
            m_VisualGroundCheckObject.transform.localPosition = groundCheckPos;

            if (m_VisualGroundCheckRenderer != null && m_VisualGroundCheckRenderer.material != null)
            {
                Color current = m_IsGroundedVisual ? Color.green : Color.yellow;
                current.a = 0.5f;
                m_VisualGroundCheckRenderer.material.color = current;
            }

            if (m_VisualGroundCheckMeshFilter != null)
            {
                float groundCheckRadius = m_FeetWidth > 0f ? m_FeetWidth * 0.5f : m_CapsuleRadius;
                m_VisualGroundCheckMeshFilter.mesh = CreateGroundCheckDiskMesh(groundCheckRadius);
            }
        }

        // ===================================================================
        // Mesh Generation
        // ===================================================================

        /// <summary>
        /// Erstellt ein Box-Mesh (SoF2 AABB).
        /// Box geht von Y=0 bis Y=height, Breite/Tiefe = radius*2.
        /// </summary>
        private Mesh CreateBoxMesh()
        {
            Mesh mesh = new() { name = "BoxVisual" };

            float radius = Mathf.Max(0.01f, m_CapsuleRadius);
            float height = Mathf.Max(0.01f, m_CapsuleHeight);
            float halfW = radius;

            Vector3[] verts = new Vector3[8]
            {
                new(-halfW, 0f,     -halfW), // 0: left-bottom-back
                new( halfW, 0f,     -halfW), // 1: right-bottom-back
                new( halfW, 0f,      halfW), // 2: right-bottom-front
                new(-halfW, 0f,      halfW), // 3: left-bottom-front
                new(-halfW, height, -halfW), // 4: left-top-back
                new( halfW, height, -halfW), // 5: right-top-back
                new( halfW, height,  halfW), // 6: right-top-front
                new(-halfW, height,  halfW), // 7: left-top-front
            };

            int[] tris = new int[]
            {
                // Bottom (Y=0) — winding outward (down)
                0, 1, 2,  0, 2, 3,
                // Top (Y=height) — winding outward (up)
                4, 6, 5,  4, 7, 6,
                // Front (Z+)
                3, 2, 6,  3, 6, 7,
                // Back (Z-)
                0, 5, 1,  0, 4, 5,
                // Left (X-)
                0, 3, 7,  0, 7, 4,
                // Right (X+)
                1, 6, 2,  1, 5, 6,
            };

            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// Erstellt ein flaches Disk-Mesh fuer die Ground-Check-Visualisierung.
        /// </summary>
        private Mesh CreateGroundCheckDiskMesh(float diskRadius)
        {
            Mesh mesh = new() { name = "GroundCheckVisual" };

            int segments = 32;
            float height = 0.1f;

            System.Collections.Generic.List<Vector3> verts = new();
            System.Collections.Generic.List<Vector2> uvs = new();
            System.Collections.Generic.List<int> tris = new();

            // Top center
            int topCenter = verts.Count;
            verts.Add(new Vector3(0f, height * 0.5f, 0f));
            uvs.Add(new Vector2(0.5f, 0.5f));

            // Bottom center
            int botCenter = verts.Count;
            verts.Add(new Vector3(0f, -height * 0.5f, 0f));
            uvs.Add(new Vector2(0.5f, 0.5f));

            // Top circle
            int topStart = verts.Count;
            for (int seg = 0; seg <= segments; seg++)
            {
                float u = (float)seg / segments;
                float theta = u * Mathf.PI * 2f;
                verts.Add(new Vector3(Mathf.Cos(theta) * diskRadius, height * 0.5f, Mathf.Sin(theta) * diskRadius));
                uvs.Add(new Vector2(u, 0.5f));
            }

            // Bottom circle
            int botStart = verts.Count;
            for (int seg = 0; seg <= segments; seg++)
            {
                float u = (float)seg / segments;
                float theta = u * Mathf.PI * 2f;
                verts.Add(new Vector3(Mathf.Cos(theta) * diskRadius, -height * 0.5f, Mathf.Sin(theta) * diskRadius));
                uvs.Add(new Vector2(u, 0.5f));
            }

            // Top face
            for (int seg = 0; seg < segments; seg++)
            {
                tris.Add(topCenter);
                tris.Add(topStart + seg + 1);
                tris.Add(topStart + seg);
            }

            // Bottom face
            for (int seg = 0; seg < segments; seg++)
            {
                tris.Add(botCenter);
                tris.Add(botStart + seg);
                tris.Add(botStart + seg + 1);
            }

            // Side
            for (int seg = 0; seg < segments; seg++)
            {
                int tc = topStart + seg;
                int tn = topStart + seg + 1;
                int bc = botStart + seg;
                int bn = botStart + seg + 1;

                tris.Add(tc); tris.Add(bn); tris.Add(tn);
                tris.Add(tc); tris.Add(bc); tris.Add(bn);
            }

            mesh.vertices = verts.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.triangles = tris.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        // ===================================================================
        // Helpers
        // ===================================================================

        /// <summary>
        /// Berechnet Capsule-Radius aus Fuss-Abstand.
        /// </summary>
        /// <summary>
        /// Berechnet den Capsule-Radius aus SkinnedMeshRenderer-Bounds.
        /// Faellt auf Fuss-Abstand zurueck falls keine Renderer gefunden werden.
        /// Ergebnis wird auf [k_MinCapsuleRadius, k_MaxCapsuleRadius] geklemmt fuer realistische SoF2-Proportionen.
        /// </summary>
        private float CalculateRadiusFromBounds(Transform leftFoot, Transform rightFoot)
        {
            // Primary: SkinnedMeshRenderer-Bounds (Surface-Meshes des Character-Models)
            SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }

                float boundsRadius = Mathf.Max(bounds.size.x, bounds.size.z) * 0.5f;
                return Mathf.Clamp(boundsRadius, k_MinCapsuleRadius, k_MaxCapsuleRadius);
            }

            // Fallback: Fuss-Abstand
            if (leftFoot != null && rightFoot != null)
            {
                Vector3 diff = new(
                    leftFoot.position.x - rightFoot.position.x,
                    0f,
                    leftFoot.position.z - rightFoot.position.z);
                float feetRadius = diff.magnitude * 0.5f;
                return Mathf.Clamp(feetRadius, k_MinCapsuleRadius, k_MaxCapsuleRadius);
            }

            return m_CapsuleRadius > 0f ? m_CapsuleRadius : k_MinCapsuleRadius;
        }
    }
}

using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.Characters.Client
{
    /// <summary>
    /// Berechnet Capsule-Parameter aus Character-Bones (Cranium, Fuesse).
    /// Stellt Capsule-Daten fuer die SoF2-Physik (CapsuleCasts) bereit.
    /// Optionaler Visual-Debug: zeichnet Capsule + Ground-Check-Disk zur Laufzeit im Editor.
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

        /// <summary>Ground-Check-Distanz fuer CapsuleCast nach unten.</summary>
        [SerializeField]
        private float m_GroundCheckDistance = 0.2f;

        [Header("Auto Capsule Sizing")]
        [SerializeField]
        private bool m_AutoSizeCapsule = true;

        [SerializeField]
        private bool m_DynamicCapsuleSizing = true;

        [Header("Visual Collider Debug")]
        [SerializeField]
        private bool m_ShowVisualCollider;

        [SerializeField]
        private Color m_ColliderColor = new(0f, 1f, 0f, 0.3f);

        [SerializeField]
        private Material m_ColliderMaterial;

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

        // Visual Collider GameObjects + Components
        private GameObject m_VisualColliderObject;
        private MeshRenderer m_VisualColliderRenderer;
        private MeshFilter m_VisualColliderMeshFilter;

        private GameObject m_VisualGroundCheckObject;
        private MeshRenderer m_VisualGroundCheckRenderer;
        private MeshFilter m_VisualGroundCheckMeshFilter;

        /// <summary>Gecachter Grounded-State fuer Visual-Farbe.</summary>
        private bool m_IsGroundedVisual;

        /// <summary>Actual Physics CapsuleCollider fuer CapsuleCast-Detection durch andere Spieler.</summary>
        private CapsuleCollider m_PhysicsCollider;

        // --- Public Getters ---

        /// <summary>Aktueller Capsule-Radius.</summary>
        public float GetCurrentCapsuleRadius() => m_CapsuleRadius;

        /// <summary>Aktuelle Capsule-Hoehe.</summary>
        public float GetCurrentCapsuleHeight() => m_CapsuleHeight;

        /// <summary>Aktuelles Capsule-Center (lokal).</summary>
        public Vector3 GetCurrentCapsuleCenter() => m_CapsuleCenter;

        /// <summary>Ground-Check-Distanz fuer CapsuleCast.</summary>
        public float GetCurrentGroundCheckDistance() => m_GroundCheckDistance;

        /// <summary>
        /// Physik-CapsuleCollider fuer temporaeres Deaktivieren waehrend eigener Simulation.
        /// </summary>
        public CapsuleCollider PhysicsCollider => m_PhysicsCollider;

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

            // Breite aus Fuss-Abstand berechnen
            float newRadius = CalculateRadiusFromFeet(leftFoot, rightFoot);

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

            // Physics CapsuleCollider erstellen/aktualisieren
            UpdatePhysicsCollider();

            // Visual Debug initialisieren (falls aktiviert)
            InitializeVisualCollider();
            InitializeVisualGroundCheck();

            Debug.Log($"[ClientColliderSystem] Auto-sized Capsule — Height: {newHeight:F2}, Radius: {newRadius:F2}, Center: {m_CapsuleCenter}");
        }

        /// <summary>
        /// Setzt Capsule-Groesse fuer Standing / Crouching.
        /// Crouch = 60% der Standing-Hoehe.
        /// </summary>
        public void UpdateCapsuleSizeForState(bool isCrouching)
        {
            if (!m_DynamicCapsuleSizing || !m_AutoSizeCapsule)
            {
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
        // Physics CapsuleCollider
        // ===================================================================

        /// <summary>
        /// Erstellt oder aktualisiert den Physics-CapsuleCollider.
        /// Dieser Collider wird von CapsuleCasts anderer Spieler erkannt
        /// und ermoeglicht Player-Player Collision.
        /// </summary>
        private void UpdatePhysicsCollider()
        {
            if (m_PhysicsCollider == null)
            {
                m_PhysicsCollider = gameObject.AddComponent<CapsuleCollider>();
            }

            m_PhysicsCollider.height = m_CapsuleHeight;
            m_PhysicsCollider.radius = m_CapsuleRadius;
            m_PhysicsCollider.center = m_CapsuleCenter;
        }

        // ===================================================================
        // Visual Debug — Capsule + Ground-Check-Disk
        // ===================================================================

        /// <summary>
        /// Erstellt das Visual-Collider-Mesh-GameObject (Capsule).
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

            m_VisualColliderMeshFilter.mesh = CreateCapsuleMesh();

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
            m_VisualGroundCheckRenderer.sortingOrder = 999;
        }

        /// <summary>
        /// Aktualisiert Position, Mesh und Farbe des Visual-Capsule.
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
                m_VisualColliderMeshFilter.mesh = CreateCapsuleMesh();
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
        /// Erstellt ein Capsule-Mesh (Top-Hemi + Zylinder + Bottom-Hemi + Bottom-Cap).
        /// Capsule geht von Y=0 bis Y=height.
        /// </summary>
        private Mesh CreateCapsuleMesh()
        {
            Mesh mesh = new() { name = "CapsuleVisual" };

            int segments = 16;
            int hemiRings = 8;
            int cylRings = 4;
            float radius = Mathf.Max(0.01f, m_CapsuleRadius);
            float height = Mathf.Max(0.01f, m_CapsuleHeight);

            if (radius <= 0f || height <= 0f)
            {
                return mesh;
            }

            System.Collections.Generic.List<Vector3> verts = new();
            System.Collections.Generic.List<Vector2> uvs = new();
            System.Collections.Generic.List<int> tris = new();

            float cylHeight = Mathf.Max(0f, height - radius * 2f);
            float cylBottom = radius;
            float cylTop = height - radius;

            // --- Top hemisphere ---
            for (int ring = 0; ring <= hemiRings; ring++)
            {
                float v = (float)ring / hemiRings;
                float phi = v * Mathf.PI / 2f;

                for (int seg = 0; seg <= segments; seg++)
                {
                    float u = (float)seg / segments;
                    float theta = u * Mathf.PI * 2f;

                    float x = Mathf.Cos(theta) * Mathf.Sin(phi) * radius;
                    float y = cylTop + Mathf.Cos(phi) * radius;
                    float z = Mathf.Sin(theta) * Mathf.Sin(phi) * radius;

                    verts.Add(new Vector3(x, y, z));
                    uvs.Add(new Vector2(u, v));
                }
            }

            // --- Cylinder ---
            int cylVertOffset = verts.Count;
            if (cylHeight > 0f)
            {
                for (int ring = 0; ring <= cylRings; ring++)
                {
                    float v = (float)ring / cylRings;
                    float y = Mathf.Lerp(cylBottom, cylTop, v);

                    for (int seg = 0; seg <= segments; seg++)
                    {
                        float u = (float)seg / segments;
                        float theta = u * Mathf.PI * 2f;

                        verts.Add(new Vector3(Mathf.Cos(theta) * radius, y, Mathf.Sin(theta) * radius));
                        uvs.Add(new Vector2(u, v));
                    }
                }
            }

            // --- Bottom hemisphere ---
            int botVertOffset = verts.Count;
            for (int ring = 0; ring <= hemiRings; ring++)
            {
                float v = (float)ring / hemiRings;
                float phi = (1f - v) * Mathf.PI / 2f;

                for (int seg = 0; seg <= segments; seg++)
                {
                    float u = (float)seg / segments;
                    float theta = u * Mathf.PI * 2f;

                    float x = Mathf.Cos(theta) * Mathf.Sin(phi) * radius;
                    float y = Mathf.Cos(phi) * radius;
                    float z = Mathf.Sin(theta) * Mathf.Sin(phi) * radius;

                    verts.Add(new Vector3(x, y, z));
                    uvs.Add(new Vector2(u, v));
                }
            }

            // Triangles — top hemisphere
            for (int ring = 0; ring < hemiRings; ring++)
            {
                for (int seg = 0; seg < segments; seg++)
                {
                    int cur = ring * (segments + 1) + seg;
                    int nxt = (ring + 1) * (segments + 1) + seg;

                    tris.Add(cur); tris.Add(nxt); tris.Add(cur + 1);
                    tris.Add(cur + 1); tris.Add(nxt); tris.Add(nxt + 1);
                }
            }

            // Triangles — cylinder
            if (cylHeight > 0f)
            {
                for (int ring = 0; ring < cylRings; ring++)
                {
                    for (int seg = 0; seg < segments; seg++)
                    {
                        int cur = cylVertOffset + ring * (segments + 1) + seg;
                        int nxt = cylVertOffset + (ring + 1) * (segments + 1) + seg;

                        tris.Add(cur); tris.Add(nxt); tris.Add(cur + 1);
                        tris.Add(cur + 1); tris.Add(nxt); tris.Add(nxt + 1);
                    }
                }
            }
            else
            {
                // No cylinder — connect top to bottom directly
                int topLast = hemiRings;
                for (int seg = 0; seg < segments; seg++)
                {
                    int tc = topLast * (segments + 1) + seg;
                    int tn = tc + 1;
                    int bc = botVertOffset + seg;
                    int bn = bc + 1;

                    tris.Add(tc); tris.Add(bn); tris.Add(tn);
                    tris.Add(tc); tris.Add(bc); tris.Add(bn);
                }
            }

            // Triangles — bottom hemisphere
            for (int ring = 0; ring < hemiRings; ring++)
            {
                for (int seg = 0; seg < segments; seg++)
                {
                    int cur = botVertOffset + ring * (segments + 1) + seg;
                    int nxt = botVertOffset + (ring + 1) * (segments + 1) + seg;

                    tris.Add(cur); tris.Add(cur + 1); tris.Add(nxt);
                    tris.Add(cur + 1); tris.Add(nxt + 1); tris.Add(nxt);
                }
            }

            // Bottom cap
            int capCenter = verts.Count;
            verts.Add(new Vector3(0f, 0f, 0f));
            uvs.Add(new Vector2(0.5f, 0.5f));

            int capStart = verts.Count;
            for (int seg = 0; seg <= segments; seg++)
            {
                float u = (float)seg / segments;
                float theta = u * Mathf.PI * 2f;
                verts.Add(new Vector3(Mathf.Cos(theta) * radius, 0f, Mathf.Sin(theta) * radius));
                uvs.Add(new Vector2(u, 0.5f));
            }

            for (int seg = 0; seg < segments; seg++)
            {
                tris.Add(capCenter);
                tris.Add(capStart + seg + 1);
                tris.Add(capStart + seg);
            }

            mesh.vertices = verts.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.triangles = tris.ToArray();
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
        private float CalculateRadiusFromFeet(Transform leftFoot, Transform rightFoot)
        {
            if (leftFoot != null && rightFoot != null)
            {
                Vector3 diff = new(
                    leftFoot.position.x - rightFoot.position.x,
                    0f,
                    leftFoot.position.z - rightFoot.position.z);
                return diff.magnitude * 0.5f;
            }

            // Fallback: Renderer-Bounds
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                foreach (Renderer r in renderers)
                {
                    bounds.Encapsulate(r.bounds);
                }

                return Mathf.Max(bounds.size.x, bounds.size.z) * 0.5f;
            }

            return m_CapsuleRadius > 0f ? m_CapsuleRadius : 0.5f;
        }
    }
}

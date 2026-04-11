using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace Tolik.RemakeSoF.Runtime.Game.Environment
{
    /// <summary>
    /// Server-spawnte Barriere (z.B. M4 Alt-Attack Fence in HideAndSeek).
    /// Wird als NetworkObject gespawnt, blockiert Spieler- und Bot-Bewegung
    /// identisch zu Map-Geometrie (COL_/clip auf BrushCollision-Layer mit MeshCollider).
    /// NavMeshObstacle fuer Bot-Pathfinding. Despawnt nach Ablauf der Lebensdauer.
    /// </summary>
    public class FenceBarrier : NetworkBehaviour
    {
        /// <summary>Prefix fuer BSP-Brush-Volumes (COL_*0, COL_*1, ... COL_*N).</summary>
        private const string BRUSH_PREFIX = "COL_*";

        /// <summary>Suffix fuer Clip-Volumes (_clip).</summary>
        private const string CLIP_SUFFIX = "_clip";

        /// <summary>Layer-Name fuer Brush/Clip-Volumes (identisch zu MapColliderApplier).</summary>
        private const string BRUSH_COLLISION_LAYER = "BrushCollision";

        /// <summary>Verbleibende Lebensdauer in Sekunden.</summary>
        private float m_TimeRemaining;

        /// <summary>Ob die Barriere initialisiert wurde.</summary>
        private bool m_Initialized;

        /// <summary>
        /// Server: Initialisiert die Barriere mit Lebensdauer.
        /// Erstellt Collider identisch zu MapColliderApplier:
        /// COL_* → MeshCollider auf BrushCollision-Layer (ShadowsOnly),
        /// COL_*N_clip → MeshCollider auf BrushCollision-Layer (unsichtbar),
        /// visuelle Surfaces → MeshCollider auf Default-Layer.
        /// NavMeshObstacle fuer Bot-Pathfinding.
        /// </summary>
        /// <param name="duration">Lebensdauer in Sekunden.</param>
        public void Initialize(float duration)
        {
            m_TimeRemaining = duration;
            m_Initialized = true;

            ApplyCollidersLikeMap();
            EnsureNavMeshObstacle();
        }

        /// <summary>
        /// Erstellt Collider fuer alle Kinder identisch zu MapColliderApplier.
        /// COL_* Brush-Volumes: MeshCollider + BrushCollision-Layer + ShadowsOnly.
        /// COL_*N_clip: MeshCollider + BrushCollision-Layer + Renderer aus.
        /// Alle anderen (visuelle Surfaces): MeshCollider auf Default-Layer.
        /// Spieler-Physik (BoxCast) trifft beide Layer → Fence blockiert wie Map-Wand.
        /// </summary>
        private void ApplyCollidersLikeMap()
        {
            int brushLayer = LayerMask.NameToLayer(BRUSH_COLLISION_LAYER);

            MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>(true);

            foreach (MeshFilter meshFilter in meshFilters)
            {
                Mesh mesh = meshFilter.sharedMesh;
                if (mesh == null || mesh.vertexCount == 0)
                {
                    continue;
                }

                GameObject child = meshFilter.gameObject;

                // Bereits einen Collider? Ueberspringen.
                if (child.GetComponent<Collider>() != null)
                {
                    continue;
                }

                if (IsClipVolume(child))
                {
                    // Clip-Volume: MeshCollider + BrushCollision + unsichtbar
                    MeshCollider col = child.AddComponent<MeshCollider>();
                    col.sharedMesh = mesh;
                    child.layer = brushLayer;

                    Renderer rend = child.GetComponent<Renderer>();
                    if (rend != null)
                    {
                        rend.enabled = false;
                    }
                }
                else if (IsBrushVolume(child))
                {
                    // Brush-Volume: MeshCollider + BrushCollision + ShadowsOnly
                    MeshCollider col = child.AddComponent<MeshCollider>();
                    col.sharedMesh = mesh;
                    child.layer = brushLayer;

                    Renderer rend = child.GetComponent<Renderer>();
                    if (rend != null)
                    {
                        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                        rend.sharedMaterials = System.Array.Empty<Material>();
                    }
                }
                else
                {
                    // Visuelle Surface: MeshCollider auf Default-Layer
                    MeshCollider col = child.AddComponent<MeshCollider>();
                    col.sharedMesh = mesh;
                }
            }
        }

        /// <summary>
        /// Prueft ob das GameObject ein Clip-Volume ist (Name: COL_*N_clip).
        /// </summary>
        private bool IsClipVolume(GameObject go)
        {
            string name = go.name;
            if (!name.StartsWith(BRUSH_PREFIX, System.StringComparison.Ordinal) ||
                !name.EndsWith(CLIP_SUFFIX, System.StringComparison.Ordinal))
            {
                return false;
            }

            int digitStart = BRUSH_PREFIX.Length;
            int digitEnd = name.Length - CLIP_SUFFIX.Length;
            if (digitEnd <= digitStart)
            {
                return false;
            }

            for (int i = digitStart; i < digitEnd; i++)
            {
                if (!char.IsDigit(name[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Prueft ob das GameObject ein Brush-Volume ist (Name: COL_*N, nur Ziffern nach Prefix).
        /// </summary>
        private bool IsBrushVolume(GameObject go)
        {
            string name = go.name;
            if (!name.StartsWith(BRUSH_PREFIX, System.StringComparison.Ordinal))
            {
                return false;
            }

            for (int i = BRUSH_PREFIX.Length; i < name.Length; i++)
            {
                if (!char.IsDigit(name[i]))
                {
                    return false;
                }
            }

            return name.Length > BRUSH_PREFIX.Length;
        }

        /// <summary>
        /// Stellt sicher, dass ein NavMeshObstacle vorhanden ist.
        /// Groesse wird aus den kombinierten Renderer-Bounds berechnet.
        /// </summary>
        private void EnsureNavMeshObstacle()
        {
            if (GetComponent<NavMeshObstacle>() != null)
            {
                return;
            }

            Bounds bounds = CalculateCombinedBounds();
            NavMeshObstacle obstacle = gameObject.AddComponent<NavMeshObstacle>();
            obstacle.carving = true;
            obstacle.carveOnlyStationary = false;
            obstacle.center = transform.InverseTransformPoint(bounds.center);
            obstacle.size = bounds.size;
        }

        /// <summary>
        /// Berechnet die kombinierten Bounds aller Renderer-Kinder.
        /// </summary>
        /// <returns>Kombinierte Welt-Bounds.</returns>
        private Bounds CalculateCombinedBounds()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

            if (renderers.Length == 0)
            {
                return new Bounds(transform.position, Vector3.one);
            }

            Bounds combined = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                combined.Encapsulate(renderers[i].bounds);
            }

            return combined;
        }

        private void Update()
        {
            if (!IsServer || !m_Initialized)
            {
                return;
            }

            m_TimeRemaining -= Time.deltaTime;

            if (m_TimeRemaining <= 0f)
            {
                NetworkObject.Despawn();
            }
        }
    }
}

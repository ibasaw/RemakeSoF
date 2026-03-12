using System.Linq;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Management.MapManagement
{
    /// <summary>
    /// Erstellt Collider für alle Map-Renderer. Unabhängig von Texturen,
    /// damit der Server Kollisionsgeometrie erhält, ohne TextureManager zu benötigen.
    /// </summary>
    public class MapColliderApplier
    {
        /// <summary>
        /// Prefix für is_transparent Properties in Ghoul2Meta.
        /// </summary>
        private const string k_TransparentPrefix = "is_transparent_";

        /// <summary>
        /// Erstellt Collider für alle Renderer mit MeshFilter in der Map-Instanz.
        /// Transparente Surfaces (is_transparent_0..N) werden übersprungen.
        /// </summary>
        /// <param name="mapInstance">Die instanziierte Map.</param>
        public void ApplyColliders(GameObject mapInstance)
        {
            if (mapInstance == null)
            {
                return;
            }

            Renderer[] allRenderers = mapInstance.GetComponentsInChildren<Renderer>(true);
            int colliderCount = 0;

            foreach (Renderer renderer in allRenderers)
            {
                if (ApplyCollider(renderer))
                {
                    colliderCount++;
                }
            }

            Debug.Log($"[MapColliderApplier] Created {colliderCount} colliders for {allRenderers.Length} renderers.");

            if (colliderCount > 0)
            {
                Physics.SyncTransforms();
            }
        }

        /// <summary>
        /// Erstellt einen MeshCollider für den Renderer.
        /// Überspringt transparente Surfaces (Ghoul2Meta is_transparent_0..N).
        /// </summary>
        /// <returns>True wenn ein Collider erstellt wurde.</returns>
        private bool ApplyCollider(Renderer renderer)
        {
            if (!renderer.TryGetComponent(out MeshFilter meshFilter))
            {
                return false;
            }

            if (IsTransparent(renderer))
            {
                return false;
            }

            Mesh mesh = meshFilter.sharedMesh;
            if (mesh == null || mesh.vertexCount == 0)
            {
                return false;
            }

            GameObject go = renderer.gameObject;
            go.isStatic = true;

            MeshCollider collider = go.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;

            return true;
        }

        /// <summary>
        /// Prüft ob der Renderer als transparent markiert ist (via Ghoul2Meta is_transparent_0..N).
        /// </summary>
        private bool IsTransparent(Renderer renderer)
        {
            if (!renderer.TryGetComponent(out Ghoul2Meta meta))
            {
                return false;
            }

            return meta.GetPropertyNames().Any(name => name.StartsWith(k_TransparentPrefix));
        }
    }
}

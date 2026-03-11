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
        /// Schwellwert unterhalb dessen eine Bounds-Achse als flach gilt.
        /// </summary>
        private const float k_FlatThreshold = 0.001f;

        /// <summary>
        /// Erstellt Collider für alle Renderer mit MeshFilter in der Map-Instanz.
        /// Flache Meshes (eine Achse ~0) erhalten einen BoxCollider,
        /// volumetrische Meshes einen MeshCollider.
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
        /// Erstellt einen Collider für den Renderer. Flache Meshes (eine Achse ~0)
        /// erhalten einen BoxCollider, volumetrische Meshes einen MeshCollider.
        /// </summary>
        /// <returns>True wenn ein Collider erstellt wurde.</returns>
        private bool ApplyCollider(Renderer renderer)
        {
            if (!renderer.TryGetComponent(out MeshFilter meshFilter))
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

            Vector3 size = mesh.bounds.size;
            if (size.x < k_FlatThreshold || size.y < k_FlatThreshold || size.z < k_FlatThreshold)
            {
                BoxCollider box = go.AddComponent<BoxCollider>();
                box.center = mesh.bounds.center;
                box.size = size;
            }
            else
            {
                MeshCollider collider = go.AddComponent<MeshCollider>();
                collider.sharedMesh = mesh;
            }

            return true;
        }
    }
}

using System.Linq;
using Tolik.RemakeSoF.Runtime.Game.Effects;
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
        /// Prefix für surface_types_json Properties in Ghoul2Meta.
        /// </summary>
        private const string k_SurfaceTypesJsonPrefix = "surface_types_json_";

        /// <summary>
        /// Prefix fuer q3map_material Properties in Ghoul2Meta (q3map_material_0..N, pro Sub-Material-Slot).
        /// Wert ist der SoF2-Surface-Typ-Name (z.B. "Concrete", "HollowMetal", "Flesh").
        /// </summary>
        private const string k_Q3MapMaterialPrefix = "q3map_material_";

        /// <summary>
        /// Q3map_material ohne Slot-Suffix (Fallback wenn kein _N vorhanden).
        /// </summary>
        private const string k_Q3MapMaterialSingle = "q3map_material";

        /// <summary>
        /// Maximale Anzahl durchsuchter Slots.
        /// </summary>
        private const int k_MaxSlots = 32;

        /// <summary>
        /// Mindestdicke für Sky-BoxCollider um Physics-Tunneling zu verhindern.
        /// </summary>
        private const float k_MinSkyColliderThickness = 0.1f;

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
        /// Erstellt einen Collider für den Renderer.
        /// Sky-Surfaces erhalten einen BoxCollider, alle anderen einen MeshCollider.
        /// Transparente Surfaces (Ghoul2Meta is_transparent_0..N) werden übersprungen.
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

            if (IsSkyboxSurface(renderer))
            {
                BoxCollider boxCollider = go.AddComponent<BoxCollider>();
                EnforceSkyColliderThickness(boxCollider, mesh);
            }
            else
            {
                MeshCollider collider = go.AddComponent<MeshCollider>();
                collider.sharedMesh = mesh;
            }

            // SurfaceTypeMarker aus q3map_material Ghoul2Meta-Property setzen
            ApplySurfaceTypeMarker(renderer);

            return true;
        }

        /// <summary>
        /// Setzt den BoxCollider auf die Mesh-Bounds und erzwingt eine Mindestdicke
        /// auf jeder zu dünnen Dimension für Sky-Surfaces.
        /// </summary>
        private void EnforceSkyColliderThickness(BoxCollider boxCollider, Mesh mesh)
        {
            Vector3 center = mesh.bounds.center;
            Vector3 size = mesh.bounds.size;

            if (size.x < k_MinSkyColliderThickness) size.x = k_MinSkyColliderThickness;
            if (size.y < k_MinSkyColliderThickness) size.y = k_MinSkyColliderThickness;
            if (size.z < k_MinSkyColliderThickness) size.z = k_MinSkyColliderThickness;

            boxCollider.center = center;
            boxCollider.size = size;
        }

        /// <summary>
        /// Liest den q3map_material Wert aus Ghoul2Meta und setzt einen SurfaceTypeMarker.
        /// Sucht q3map_material_0..N (pro Slot) und q3map_material (Fallback ohne Suffix).
        /// Der Wert wird zu lowercase konvertiert fuer SoF2_data_per_surface.json Kompatibilitaet.
        /// </summary>
        private void ApplySurfaceTypeMarker(Renderer renderer)
        {
            if (!renderer.TryGetComponent(out Ghoul2Meta meta))
            {
                return;
            }

            string material = GetQ3MapMaterial(meta);
            if (string.IsNullOrEmpty(material))
            {
                return;
            }

            // SoF2-Surface-Keys sind lowercase (z.B. "concrete", "hollowmetal")
            // q3map_material Werte sind PascalCase (z.B. "Concrete", "HollowMetal")
            string surfaceType = material.ToLowerInvariant();

            GameObject go = renderer.gameObject;
            SurfaceTypeMarker marker = go.GetComponent<SurfaceTypeMarker>();
            if (marker == null)
            {
                marker = go.AddComponent<SurfaceTypeMarker>();
            }

            marker.SetSurfaceType(surfaceType);
        }

        /// <summary>
        /// Liest den ersten q3map_material Wert aus Ghoul2Meta.
        /// Sucht zuerst q3map_material_0..N, dann q3map_material als Fallback.
        /// </summary>
        private string GetQ3MapMaterial(Ghoul2Meta meta)
        {
            // Erst Slot-basierte Properties pruefen (q3map_material_0, _1, ...)
            for (int i = 0; i < k_MaxSlots; i++)
            {
                string propertyName = k_Q3MapMaterialPrefix + i;
                if (!meta.HasProperty(propertyName))
                {
                    break;
                }

                string value = meta.GetString(propertyName);
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }

            // Fallback: q3map_material ohne Suffix
            if (meta.HasProperty(k_Q3MapMaterialSingle))
            {
                string value = meta.GetString(k_Q3MapMaterialSingle);
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }

            return null;
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

        /// <summary>
        /// Prüft ob der Renderer ein Sky-Surface ist (surface_types_json_N enthält "sky").
        /// </summary>
        private bool IsSkyboxSurface(Renderer renderer)
        {
            if (!renderer.TryGetComponent(out Ghoul2Meta meta))
            {
                return false;
            }

            for (int i = 0; i < k_MaxSlots; i++)
            {
                string propertyName = k_SurfaceTypesJsonPrefix + i;
                if (!meta.HasProperty(propertyName))
                {
                    continue;
                }

                string jsonValue = meta.GetString(propertyName);
                if (!string.IsNullOrEmpty(jsonValue) && jsonValue.Contains("sky", System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

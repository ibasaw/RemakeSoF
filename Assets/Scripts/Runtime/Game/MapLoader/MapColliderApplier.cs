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
        /// Prefix fuer BSP-Brush-Volumes (COL_0, COL_1, ... COL_N).
        /// Diese sind unsichtbare Kollisions-Volumes ohne Texturen.
        /// </summary>
        private const string k_BrushPrefix = "COL_*";

        /// <summary>
        /// Prefix fuer Clip-Volumes (COL_*0_clip, COL_*1_clip, ... COL_*N_clip).
        /// Unsichtbare Kollisions-Volumes fuer Spielerbewegung.
        /// </summary>
        private const string k_ClipPrefix = "COL_*";
        private const string k_ClipSuffix = "_clip";

        /// <summary>
        /// Prefix fuer Transparenz-Properties (is_transparent_0..is_transparent_N).
        /// Transparente Surfaces erhalten keinen Collider.
        /// </summary>
        private const string k_TransparentPrefix = "is_transparent_";

        /// <summary>
        /// Layer-Name fuer Brush/Clip-Volumes. Wird von Hitscan-Raycasts ausgeschlossen,
        /// damit nur visuelle Surfaces mit SurfaceTypeMarker getroffen werden.
        /// </summary>
        private const string k_BrushCollisionLayerName = "BrushCollision";

        /// <summary>
        /// Mindestdicke fuer Sky-BoxCollider um Physics-Tunneling zu verhindern.
        /// </summary>
        private const float k_MinSkyColliderThickness = 0.1f;

        /// <summary>
        /// Erstellt Collider fuer alle BSP-Brush-Volumes in der Map-Instanz.
        /// Nur Brushes (*0..*N) erhalten MeshCollider — alle anderen Surfaces nur SurfaceTypeMarker.
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
        /// BSP-Brush-Volumes erhalten MeshCollider als Kollisionsgeometrie.
        /// Visuelle Surfaces erhalten ebenfalls MeshCollider + SurfaceTypeMarker
        /// fuer Raycast-Hit-Detection mit korrektem Surface-Typ.
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

            // BSP-Brush-Volumes (COL_*0..COL_*N): MeshCollider + ShadowsOnly, Materials leeren.
            // BrushCollision-Layer: Hitscan-Raycasts ignorieren diesen Layer.
            if (IsBrushVolume(go))
            {
                MeshCollider collider = go.AddComponent<MeshCollider>();
                collider.cookingOptions = MeshColliderCookingOptions.CookForFasterSimulation
                                        | MeshColliderCookingOptions.EnableMeshCleaning
                                        | MeshColliderCookingOptions.WeldColocatedVertices;
                collider.sharedMesh = mesh;

                go.layer = LayerMask.NameToLayer(k_BrushCollisionLayerName);

                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                renderer.sharedMaterials = System.Array.Empty<Material>();
                return true;
            }

            // Clip-Volumes (COL_*0_clip..COL_*N_clip): MeshCollider, komplett unsichtbar.
            // BrushCollision-Layer: Hitscan-Raycasts ignorieren diesen Layer.
            if (IsClipVolume(go))
            {
                MeshCollider collider = go.AddComponent<MeshCollider>();
                collider.cookingOptions = MeshColliderCookingOptions.CookForFasterSimulation
                                        | MeshColliderCookingOptions.EnableMeshCleaning
                                        | MeshColliderCookingOptions.WeldColocatedVertices;
                collider.sharedMesh = mesh;

                go.layer = LayerMask.NameToLayer(k_BrushCollisionLayerName);

                renderer.enabled = false;
                return true;
            }

            // Sky-Surfaces: BoxCollider als Boundary.
            if (IsSkyboxSurface(renderer))
            {
                BoxCollider boxCollider = go.AddComponent<BoxCollider>();
                EnforceSkyColliderThickness(boxCollider, mesh);
                ApplySurfaceTypeMarker(renderer);
                return true;
            }

            // Transparente Surfaces: kein Collider (z.B. Glas, Gitter, Rauch).
            if (IsTransparentSurface(renderer))
            {
                ApplySurfaceTypeMarker(renderer);
                return false;
            }

            // Alle anderen visuellen Surfaces: MeshCollider + SurfaceTypeMarker.
            // Ermoeglicht Raycast-Hit-Detection mit korrektem Surface-Typ.
            MeshCollider surfaceCollider = go.AddComponent<MeshCollider>();
            surfaceCollider.cookingOptions = MeshColliderCookingOptions.CookForFasterSimulation
                                           | MeshColliderCookingOptions.EnableMeshCleaning
                                           | MeshColliderCookingOptions.WeldColocatedVertices;
            surfaceCollider.sharedMesh = mesh;

            ApplySurfaceTypeMarker(renderer);
            return true;
        }

        /// <summary>
        /// Prueft ob das GameObject ein BSP-Brush-Volume ist (Name beginnt mit * gefolgt von Ziffern).
        /// </summary>
        private bool IsBrushVolume(GameObject go)
        {
            string name = go.name;
            if (!name.StartsWith(k_BrushPrefix, System.StringComparison.Ordinal))
            {
                return false;
            }

            for (int i = k_BrushPrefix.Length; i < name.Length; i++)
            {
                if (!char.IsDigit(name[i]))
                {
                    return false;
                }
            }

            return name.Length > k_BrushPrefix.Length;
        }

        /// <summary>
        /// Prueft ob das GameObject ein Clip-Volume ist (Name: COL_*N_clip).
        /// </summary>
        private bool IsClipVolume(GameObject go)
        {
            string name = go.name;
            if (!name.StartsWith(k_ClipPrefix, System.StringComparison.Ordinal) ||
                !name.EndsWith(k_ClipSuffix, System.StringComparison.Ordinal))
            {
                return false;
            }

            // Mittelteil (zwischen Prefix und Suffix) muss nur Ziffern enthalten
            int digitStart = k_ClipPrefix.Length;
            int digitEnd = name.Length - k_ClipSuffix.Length;
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
        /// Prueft ob der Renderer ein Sky-Surface ist (surface_types_json_N enthaelt "sky").
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

        /// <summary>
        /// Prueft ob der Renderer eine transparente Surface ist (is_transparent_0..N).
        /// Transparente Surfaces (Glas, Gitter, Rauch) erhalten keinen Collider.
        /// </summary>
        private bool IsTransparentSurface(Renderer renderer)
        {
            if (!renderer.TryGetComponent(out Ghoul2Meta meta))
            {
                return false;
            }

            for (int i = 0; i < k_MaxSlots; i++)
            {
                string propertyName = k_TransparentPrefix + i;
                if (!meta.HasProperty(propertyName))
                {
                    break;
                }

                if (meta.GetBool(propertyName))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Setzt den BoxCollider auf die Mesh-Bounds und erzwingt eine Mindestdicke
        /// auf jeder zu duennen Dimension fuer Sky-Surfaces.
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


    }
}

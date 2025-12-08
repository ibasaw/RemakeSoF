using UnityEngine;

public class WorldMaterialAssigner : MonoBehaviour
{
    public void ApplySmoothnessZero()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        foreach (Renderer r in renderers)
        {
            Material[] mats = r.materials;
            Ghoul2Meta meta = r.gameObject.GetComponent<Ghoul2Meta>() ?? r.gameObject.GetComponentInParent<Ghoul2Meta>();

            for (int i = 0; i < mats.Length; i++)
            {
                Material mat = mats[i];

                // Shader auf URP Unlit setzen
                Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
                if (unlitShader != null)
                    mat.shader = unlitShader;

                // Smoothness auf 0
                if (mat.HasProperty("_Smoothness"))
                    mat.SetFloat("_Smoothness", 0f);
                if (mat.HasProperty("_SpecSmoothness"))
                    mat.SetFloat("_SpecSmoothness", 0f);
                if (mat.HasProperty("_SmoothnessRemapMax"))
                    mat.SetFloat("_SmoothnessRemapMax", 0f);

                // Transparent prüfen
                bool transparent = false;
                bool twoSided = false;

                if (meta != null && meta.HasProperty("surface_types_json"))
                {
                    string surfaceType = meta.GetString("surface_types_json");
                    Debug.Log($"[WorldMaterialAssigner] Surface Type: {surfaceType}");
                    if (surfaceType.ToLower().Contains("trans") || surfaceType.ToLower().Contains("nonopaque")
                    || surfaceType.ToLower().Contains("nonsolid")
                    || surfaceType.ToLower().Contains("alpha") || surfaceType.ToLower().Contains("translucent")
                    || surfaceType.ToLower().Contains("transparent")
                    || surfaceType.ToLower().Contains("translucent") || surfaceType.ToLower().Contains("nosolid"))
                        transparent = true;
                    Debug.Log($"[WorldMaterialAssigner] Transparent: {transparent}");
                }

                string matNameLower = mat.name.ToLower();
                if (matNameLower.Contains("_shd") || matNameLower.Contains("_nonsolid") || matNameLower.Contains("_trans"))
                {
                    transparent = true;
                }

                if (matNameLower.Contains("_two_sided") || matNameLower.Contains("_cull_disable")
                || matNameLower.Contains("_cull_back") || matNameLower.Contains("_cull") || matNameLower.Contains("_twosided")
                || matNameLower.Contains(".vertex") || matNameLower.Contains(".grid"))
                {
                    twoSided = true;
                }

                if (transparent)
                {
                    if (mat.HasProperty("_Surface"))
                        mat.SetFloat("_Surface", 1f); // Transparent

                    // Alpha Clipping aktivieren, falls nötig
                    if (mat.HasProperty("_AlphaClip"))
                        mat.SetFloat("_AlphaClip", 1f);

                    if (mat.HasProperty("_Cutoff"))
                        mat.SetFloat("_Cutoff", 0.5f);

                    mat.SetOverrideTag("RenderType", "Transparent");
                    mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                }


                // CullMode prüfen
                if (meta != null && (meta.HasProperty("cull") || meta.HasProperty("two_sided")) || twoSided)
                {
                    if (mat.HasProperty("_Cull"))
                        mat.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off); // Beide Seiten sichtbar
                    mat.EnableKeyword("_DOUBLESIDED_ON");
                }
            }
        }

        Debug.Log($"[WorldMaterialAssigner] Materialien von {renderers.Length} Renderer(n) angepasst.");
    }
}

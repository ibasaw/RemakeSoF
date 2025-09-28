using UnityEngine;
using System.Text;

[DisallowMultipleComponent]
public class MeshVisibilityLogger : MonoBehaviour
{
    [Header("Sampling")]
    public float sampleInterval = 0.1f; // Sekunden zwischen Prüfungen

    [Header("Checks")]
    public bool logBounds = true;
    public bool logBlendshapes = true;
    public bool logMaterials = true;

    SkinnedMeshRenderer[] smrs;
    Animator animator;
    float timer = 0f;

    // Cache
    Bounds[] lastLocalBounds;
    Bounds[] lastBakedBounds;
    float[][] lastBlendshapeWeights;
    float[] lastMaterialAlpha;

    void Start()
    {
        smrs = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        animator = GetComponent<Animator>();

        lastLocalBounds = new Bounds[smrs.Length];
        lastBakedBounds = new Bounds[smrs.Length];
        lastBlendshapeWeights = new float[smrs.Length][];
        lastMaterialAlpha = new float[smrs.Length];

        for (int i = 0; i < smrs.Length; i++)
        {
            lastLocalBounds[i] = smrs[i].localBounds;
            lastBakedBounds[i] = BakeBoundsSafe(smrs[i]);
            int blendCount = smrs[i].sharedMesh?.blendShapeCount ?? 0;
            lastBlendshapeWeights[i] = new float[blendCount];
            for (int j = 0; j < blendCount; j++)
                lastBlendshapeWeights[i][j] = smrs[i].GetBlendShapeWeight(j);

            var mats = smrs[i].sharedMaterials;
            lastMaterialAlpha[i] = (mats.Length > 0 && mats[0].HasProperty("_Color")) ? mats[0].color.a : -1f;
        }

        Debug.Log($"[EnhancedLogger] Monitoring {smrs.Length} SkinnedMeshRenderer(s) on '{gameObject.name}'. Animator: {(animator != null ? animator.cullingMode.ToString() : "None")}");
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= sampleInterval)
        {
            timer = 0f;
            Sample();
        }
    }

    void Sample()
    {
        for (int i = 0; i < smrs.Length; i++)
        {
            var smr = smrs[i];
            if (smr == null) continue;

            bool isVisible = smr.isVisible;
            bool updateWhenOffscreen = smr.updateWhenOffscreen;

            Bounds local = smr.localBounds;
            Bounds baked = BakeBoundsSafe(smr);
            if (smr.name != "head_bck_uppr_r_0") continue;

            var sb = new StringBuilder();
            sb.AppendLine($"[MeshLogger] {Time.time:F2}s: SMR='{smr.name}' Visible={isVisible}, updateWhenOffscreen={updateWhenOffscreen}");
            sb.AppendLine($"  LocalBounds: center={local.center}, size={local.size}");
            sb.AppendLine($"  BakedBounds: center={baked.center}, size={baked.size}");

            if (logBounds)
            {
                if (!BoundsApproximatelyEqual(local, lastLocalBounds[i]))
                {
                    sb.AppendLine($"    -> LocalBounds changed! OldSize={lastLocalBounds[i].size}, NewSize={local.size}");
                    lastLocalBounds[i] = local;
                }
                if (!BoundsApproximatelyEqual(baked, lastBakedBounds[i]))
                {
                    sb.AppendLine($"    -> BakedBounds changed! OldSize={lastBakedBounds[i].size}, NewSize={baked.size}");
                    lastBakedBounds[i] = baked;
                }
            }

            if (logBlendshapes && smr.sharedMesh != null)
            {
                for (int j = 0; j < smr.sharedMesh.blendShapeCount; j++)
                {
                    float weight = smr.GetBlendShapeWeight(j);
                    if (!Mathf.Approximately(weight, lastBlendshapeWeights[i][j]))
                    {
                        sb.AppendLine($"    -> Blendshape#{j} changed: {lastBlendshapeWeights[i][j]} -> {weight}");
                        lastBlendshapeWeights[i][j] = weight;
                    }
                }
            }

            if (logMaterials)
            {
                var mats = smr.sharedMaterials;
                if (mats.Length > 0 && mats[0].HasProperty("_Color"))
                {
                    float alpha = mats[0].color.a;
                    if (!Mathf.Approximately(alpha, lastMaterialAlpha[i]))
                    {
                        sb.AppendLine($"    -> Material Alpha changed: {lastMaterialAlpha[i]} -> {alpha}");
                        lastMaterialAlpha[i] = alpha;
                    }
                }
            }

            if (!isVisible && smr.enabled)
            {
                sb.AppendLine("    !! Mesh not visible but enabled -> likely culling (Bounds or Camera clipping)");
            }

            Debug.Log(sb.ToString());
        }
    }

    Bounds BakeBoundsSafe(SkinnedMeshRenderer smr)
    {
        var m = new Mesh();
        Bounds b = new Bounds();
        try
        {
            smr.BakeMesh(m);
            b = m.bounds;
        }
        catch { }
        finally { if (m != null) Destroy(m); }
        return b;
    }

    bool BoundsApproximatelyEqual(Bounds a, Bounds b, float tolerance = 0.001f)
    {
        return (a.center - b.center).sqrMagnitude < tolerance && (a.size - b.size).sqrMagnitude < tolerance;
    }
}

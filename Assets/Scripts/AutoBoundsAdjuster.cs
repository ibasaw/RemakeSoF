using UnityEngine;

[DisallowMultipleComponent]
public class AutoBoundsAdjuster : MonoBehaviour
{
    [Header("Sampling")]
    public float sampleInterval = 0.1f; // Sekunden zwischen Prüfungen
    public float margin = 0.1f; // zusätzliche Größe (10%) um sicher zu sein

    SkinnedMeshRenderer[] smrs;
    float timer = 0f;

    void Start()
    {
        smrs = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        Debug.Log($"[AutoBoundsAdjuster] Monitoring {smrs.Length} SkinnedMeshRenderer(s) on '{gameObject.name}'");
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= sampleInterval)
        {
            timer = 0f;
            AdjustBounds();
        }
    }

    void AdjustBounds()
    {
        foreach (var smr in smrs)
        {
            if (smr == null) continue;

            // Baked Mesh aus aktueller Pose
            Mesh bakedMesh = new Mesh();
            smr.BakeMesh(bakedMesh);

            Bounds baked = bakedMesh.bounds;

            // Optional Margin hinzufügen
            Vector3 expand = baked.size * margin;
            baked.Expand(expand);

            // Prüfen, ob localBounds kleiner als bakedBounds
            Bounds local = smr.localBounds;
            bool changed = false;

            if (baked.min.x < local.min.x || baked.min.y < local.min.y || baked.min.z < local.min.z ||
                baked.max.x > local.max.x || baked.max.y > local.max.y || baked.max.z > local.max.z)
            {
                smr.localBounds = baked;
                changed = true;
            }

            // Logging wie beim VisibilityLogger
            Debug.Log($"[AutoBoundsAdjuster] SMR='{smr.name}': localBounds updated={changed} center={smr.localBounds.center}, size={smr.localBounds.size}, bakedBounds center={baked.center}, size={baked.size}");
        }
    }
}

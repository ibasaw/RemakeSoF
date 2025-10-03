using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class AddCombineMeshColliders : MonoBehaviour
{
    [ContextMenu("Combine Meshes And Add Collider")]
    public void CombineMeshesAndAddCollider()
    {
        Mesh combinedMesh = CombineAllMeshes(transform);
        if (combinedMesh != null)
        {
            MeshCollider mc = gameObject.GetComponent<MeshCollider>();
            if (mc == null)
                mc = gameObject.AddComponent<MeshCollider>();

            mc.sharedMesh = combinedMesh;
            mc.convex = false; // Nicht convex für statische Map
            Debug.Log("Combined MeshCollider added to " + gameObject.name);
        }
    }

    private Mesh CombineAllMeshes(Transform parent)
    {
        MeshFilter[] meshFilters = parent.GetComponentsInChildren<MeshFilter>();
        if (meshFilters.Length == 0)
        {
            Debug.LogWarning("No MeshFilters found under " + parent.name);
            return null;
        }

        CombineInstance[] combine = new CombineInstance[meshFilters.Length];
        for (int i = 0; i < meshFilters.Length; i++)
        {
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
        }

        Mesh combinedMesh = new Mesh();
        combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // Für große Meshes
        combinedMesh.CombineMeshes(combine);
        return combinedMesh;
    }
}

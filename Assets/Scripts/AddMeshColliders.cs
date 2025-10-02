using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class AddMeshColliders : MonoBehaviour
{
    // Fügt rekursiv MeshColliders hinzu
    [ContextMenu("Add Mesh Colliders To Children")]
    public void AddColliders()
    {
        AddMeshColliderRecursively(transform);
    }

    private void AddMeshColliderRecursively(Transform parent)
    {
        MeshFilter mf = parent.GetComponent<MeshFilter>();
        if (mf != null && parent.GetComponent<MeshCollider>() == null)
        {
            MeshCollider mc = parent.gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
            mc.convex = false; // Optional: true, wenn es ein bewegliches Objekt ist
            Debug.Log($"MeshCollider added to {parent.name}");
        }

        // Rekursiv für alle Kinder
        foreach (Transform child in parent)
        {
            AddMeshColliderRecursively(child);
        }
    }
}
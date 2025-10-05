using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.IO;

/// <summary>
/// Visual helper that draws a capsule representing the character collider and
/// a cylinder representing the ground-check region. Meshes are recreated only
/// when parameters change to avoid allocations every frame.
/// </summary>
[DisallowMultipleComponent]
public class MyPlayerColliderSystem : MonoBehaviour
{
	// CharacterController removed - use capsule-based manual movement
	[Header("Capsule Settings (CharacterController SoF2 values)")]
	[SerializeField] private bool drawCapsuleDebugGUI = false;
	[SerializeField] private float capsuleRadius = 0f;  
	[SerializeField] private float capsuleHeight = 0f;
	[SerializeField] private Vector3 capsuleCenter = new Vector3(0, 0, 0);  // Center at half height
	[SerializeField] private float groundCheckDistance = 1f;  // Distance to check for ground 1 ist perfekt erstmal.

    [Header("Visual Collider Debug")]
	[SerializeField] private bool showVisualCollider = true;
	[SerializeField] private Color colliderColor = new Color(0, 1, 0, 0.3f);  // Semi-transparent green
	[SerializeField] private Material colliderMaterial;

	//Berechnet die Capsule-Größe basierend auf den Charakter-Bones (Cranium ↔ Pelvis)
	[Header("Auto Capsule Sizing")]
	[SerializeField] private bool autoSizeCapsule = true;
	[SerializeField] private bool dynamicCapsuleSizing = true;     // Adjust capsule size when crouching
	[SerializeField] private float capsuleRadiusMultiplier = 0.4f;  // Multiplier for character width
	[SerializeField] private float capsuleHeightOffset = 10f;      // Additional height offset
	[SerializeField] private float minCapsuleRadius = 3f;          // Minimum radius
	[SerializeField] private float maxCapsuleRadius = 15f;         // Maximum radius
	[SerializeField] private float minCapsuleHeight = 50f;         // Minimum height
	[SerializeField] private float maxCapsuleHeight = 100f;        // Maximum height
	[SerializeField] private float crouchHeightMultiplier = 0.6f;  // Height multiplier when crouching

    // Visual collider components
	private GameObject visualColliderObject;
	private MeshRenderer visualColliderRenderer;
	private MeshFilter visualColliderMeshFilter;
	
	// Visual ground check components
	private GameObject visualGroundCheckObject;
	private MeshRenderer visualGroundCheckRenderer;
	private MeshFilter visualGroundCheckMeshFilter;
	
	// Auto-sizing cache
	private float baseCapsuleHeight;
	private float baseCapsuleRadius;
	private Vector3 baseCapsuleCenter;

	// --- Public simple getters (geben die aktuellen internen Werte zurück) ---
	public float GetCurrentCapsuleRadius()
	{
		return capsuleRadius;
	}

	public float GetCurrentCapsuleHeight()
	{
		return capsuleHeight;
	}

	public Vector3 GetCurrentCapsuleCenter()
	{
		return capsuleCenter;
	}

	public float GetCurrentGroundCheckDistance()
	{
		return groundCheckDistance;
	}

	// --- Helper: berechnete/predicted Werte berücksichtigen AutoSizing / Crouch (ohne internen State zu ändern) ---
	/// <summary>
	/// Liefert die effektiven Capsule-Werte, wie sie aktuell gelten würden.
	/// Wenn autoSizeCapsule aktiv ist, verwendet die Methode die gespeicherten baseCapsule*-Werte.
	/// Optional kannst du isCrouching=true setzen, um die crouch-Variation zu bekommen.
	/// </summary>
	public void GetPredictedCapsule(out float outHeight, out float outRadius, out Vector3 outCenter, bool isCrouching = false)
	{
		// Standard: aktuelle Werte
		outHeight = capsuleHeight;
		outRadius = capsuleRadius;
		outCenter = capsuleCenter;

		// Wenn Auto-Sizing aktiv ist, benutze die berechneten base-Werte (falls vorhanden)
		if (autoSizeCapsule)
		{
			// Falls baseCapsuleHeight/-Radius noch 0 (nicht berechnet), benutze die vorhandenen Werte als Fallback
			float baseH = (baseCapsuleHeight > 0f) ? baseCapsuleHeight : outHeight;
			float baseR = (baseCapsuleRadius > 0f) ? baseCapsuleRadius : outRadius;

			outHeight = baseH;
			outRadius = baseR;
			outCenter = new Vector3(0f, outHeight * 0.5f, 0f);

			if (dynamicCapsuleSizing && isCrouching)
			{
				outHeight = outHeight * crouchHeightMultiplier;
				outCenter = new Vector3(0f, outHeight * 0.5f, 0f);
			}
		}
	}


	private void OnGUI(){
		float scaleFactor = Screen.height / 1080f;
		GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scaleFactor, scaleFactor, 1f));

		GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
		{
			fontSize = 22,
			fontStyle = FontStyle.Bold,
			normal = { textColor = Color.cyan }
		};
		GUIStyle valueStyle = new GUIStyle(GUI.skin.label)
		{
			fontSize = 20,
			normal = { textColor = Color.white }
		};

		float x = 10f, y = 10f, line = 24f;
		// Capsule Settings
		if (drawCapsuleDebugGUI)
		{
			GUI.Label(new Rect(x, y, 600, line), $"Capsule Radius: {capsuleRadius:F2} Height: {capsuleHeight:F2}", valueStyle); y += line;
			GUI.Label(new Rect(x, y, 600, line), $"Capsule Center: {capsuleCenter}", valueStyle); y += line;
			GUI.Label(new Rect(x, y, 600, line), $"Current Position: {transform.position}", valueStyle); y += line;
			//float dynamicCastDistance = groundCheckDistance + 0.01f + Mathf.Abs(velocity.y) * Time.deltaTime;
			GUI.Label(new Rect(x, y, 600, line), $"Ground Check Distance: {groundCheckDistance:F2}", valueStyle); y += line; // (Dynamic: {dynamicCastDistance:F2})
			GUI.Label(new Rect(x, y, 600, line), $"Visual Collider: {(showVisualCollider ? "ON" : "OFF")}", valueStyle); y += line;
			// Auto Sizing Info
			if (autoSizeCapsule)
			{
				GUI.Label(new Rect(x, y, 600, line), $"Auto Sizing: ON (Base: {baseCapsuleHeight:F2}x{baseCapsuleRadius:F2})", valueStyle); y += line;
				GUI.Label(new Rect(x, y, 600, line), $"Dynamic Sizing: {(dynamicCapsuleSizing ? "ON" : "OFF")}", valueStyle); y += line;
			}
			else
			{
				GUI.Label(new Rect(x, y, 600, line), "Auto Sizing: OFF", valueStyle); y += line;
			}
		}
	}

    /// <summary>
	/// Initialize the visual collider representation
	/// </summary>
	public void InitializeVisualCollider()
	{
		if (!showVisualCollider) return;
		
		// Create visual collider object
		visualColliderObject = new GameObject("VisualCollider");
		visualColliderObject.transform.SetParent(transform);
		visualColliderObject.transform.localPosition = Vector3.zero;
		visualColliderObject.transform.localRotation = Quaternion.identity;
		visualColliderObject.transform.localScale = Vector3.one;
		
		// Add mesh components
		visualColliderMeshFilter = visualColliderObject.AddComponent<MeshFilter>();
		visualColliderRenderer = visualColliderObject.AddComponent<MeshRenderer>();
		
		// Create capsule mesh
		visualColliderMeshFilter.mesh = CreateCapsuleMesh();
		
		// Set up material
		if (colliderMaterial == null)
		{
			// Create a simple unlit material
			colliderMaterial = new Material(Shader.Find("Unlit/Color"));
			colliderMaterial.color = colliderColor;
		}
		visualColliderRenderer.material = colliderMaterial;
		
		// Make sure it renders on top
		visualColliderRenderer.sortingOrder = 1000;
	}
	
	/// <summary>
	/// Initialize the visual ground check representation
	/// </summary>
	public void InitializeVisualGroundCheck()
	{
		if (!showVisualCollider) return;
		
		// Create visual ground check object
		visualGroundCheckObject = new GameObject("VisualGroundCheck");
		visualGroundCheckObject.transform.SetParent(transform);
		visualGroundCheckObject.transform.localPosition = Vector3.zero;
		visualGroundCheckObject.transform.localRotation = Quaternion.identity;
		visualGroundCheckObject.transform.localScale = Vector3.one;
		
		// Add mesh components
		visualGroundCheckMeshFilter = visualGroundCheckObject.AddComponent<MeshFilter>();
		visualGroundCheckRenderer = visualGroundCheckObject.AddComponent<MeshRenderer>();
		
		// Create ground check mesh
		visualGroundCheckMeshFilter.mesh = CreateGroundCheckMesh();
		
		// Set up material for ground check (yellow/cyan)
		Material groundCheckMaterial = new Material(Shader.Find("Unlit/Color"));
		groundCheckMaterial.color = new Color(1f, 1f, 0f, 0.5f); // Semi-transparent yellow
		visualGroundCheckRenderer.material = groundCheckMaterial;
		
		// Make sure it renders on top
		visualGroundCheckRenderer.sortingOrder = 999;
	}
	
	/// <summary>
	/// Create a capsule mesh for visual representation
	/// </summary>
	private Mesh CreateCapsuleMesh()
	{
		Mesh mesh = new Mesh();
		mesh.name = "CapsuleVisual";

		// Capsule parameters
		int segments = 16;
		int rings = 8;
		float radius = capsuleRadius;
		float height = capsuleHeight;

		// Calculate vertices
		List<Vector3> vertices = new List<Vector3>();
		List<Vector2> uvs = new List<Vector2>();
		List<int> triangles = new List<int>();
		
		// Generate vertices for the capsule
		// Top hemisphere
		for (int ring = 0; ring <= rings / 2; ring++)
		{
			float v = (float)ring / (rings / 2);
			float phi = v * Mathf.PI / 2;
			
			for (int seg = 0; seg <= segments; seg++)
			{
				float u = (float)seg / segments;
				float theta = u * Mathf.PI * 2;
				
				float x = Mathf.Cos(theta) * Mathf.Sin(phi) * radius;
				float y = Mathf.Cos(phi) * radius + height * 0.5f;
				float z = Mathf.Sin(theta) * Mathf.Sin(phi) * radius;
				
				vertices.Add(new Vector3(x, y, z));
				uvs.Add(new Vector2(u, v));
			}
		}
		
		// Cylinder part
		for (int ring = 1; ring < rings; ring++)
		{
			float v = (float)ring / rings;
			float y = height * 0.5f - (v - 0.5f) * height;
			
			for (int seg = 0; seg <= segments; seg++)
			{
				float u = (float)seg / segments;
				float theta = u * Mathf.PI * 2;
				
				float x = Mathf.Cos(theta) * radius;
				float z = Mathf.Sin(theta) * radius;
				
				vertices.Add(new Vector3(x, y, z));
				uvs.Add(new Vector2(u, v));
			}
		}
		
		// Bottom hemisphere
		for (int ring = rings / 2; ring <= rings; ring++)
		{
			float v = (float)ring / rings;
			float phi = (v - 0.5f) * Mathf.PI;
			
			for (int seg = 0; seg <= segments; seg++)
			{
				float u = (float)seg / segments;
				float theta = u * Mathf.PI * 2;
				
				float x = Mathf.Cos(theta) * Mathf.Sin(phi) * radius;
				float y = Mathf.Cos(phi) * radius - height * 0.5f;
				float z = Mathf.Sin(theta) * Mathf.Sin(phi) * radius;
				
				vertices.Add(new Vector3(x, y, z));
				uvs.Add(new Vector2(u, v));
			}
		}
		
		// Generate triangles
		for (int ring = 0; ring < rings; ring++)
		{
			for (int seg = 0; seg < segments; seg++)
			{
				int current = ring * (segments + 1) + seg;
				int next = current + segments + 1;
				
				// First triangle
				triangles.Add(current);
				triangles.Add(next);
				triangles.Add(current + 1);
				
				// Second triangle
				triangles.Add(current + 1);
				triangles.Add(next);
				triangles.Add(next + 1);
			}
		}
		
		mesh.vertices = vertices.ToArray();
		mesh.uv = uvs.ToArray();
		mesh.triangles = triangles.ToArray();
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		
		return mesh;
	}
	
	/// <summary>
	/// Create a ground check mesh for visual representation
	/// </summary>
	private Mesh CreateGroundCheckMesh()
	{
		Mesh mesh = new Mesh();
		mesh.name = "GroundCheckVisual";
		
		// Ground check parameters
		int segments = 16;
		float radius = capsuleRadius * 0.9f; // Slightly smaller than capsule
		float height = groundCheckDistance;
		
		// Calculate vertices for a cylinder representing the ground check
		List<Vector3> vertices = new List<Vector3>();
		List<Vector2> uvs = new List<Vector2>();
		List<int> triangles = new List<int>();
		
		// Generate vertices for the ground check cylinder
		for (int ring = 0; ring <= 1; ring++) // Top and bottom rings
		{
			float y = ring == 0 ? 0f : -height; // Top at 0, bottom at -height
			
			for (int seg = 0; seg <= segments; seg++)
			{
				float u = (float)seg / segments;
				float theta = u * Mathf.PI * 2;
				
				float x = Mathf.Cos(theta) * radius;
				float z = Mathf.Sin(theta) * radius;
				
				vertices.Add(new Vector3(x, y, z));
				uvs.Add(new Vector2(u, ring));
			}
		}
		
		// Generate triangles for the cylinder sides
		for (int seg = 0; seg < segments; seg++)
		{
			int current = seg;
			int next = current + segments + 1;
			
			// First triangle
			triangles.Add(current);
			triangles.Add(next);
			triangles.Add(current + 1);
			
			// Second triangle
			triangles.Add(current + 1);
			triangles.Add(next);
			triangles.Add(next + 1);
		}
		
		// Add bottom cap (circle)
		int centerIndex = vertices.Count;
		vertices.Add(new Vector3(0, -height, 0)); // Center of bottom
		uvs.Add(new Vector2(0.5f, 0.5f));
		
		for (int seg = 0; seg < segments; seg++)
		{
			int current = segments + 1 + seg;
			int next = segments + 1 + ((seg + 1) % segments);
			
			triangles.Add(centerIndex);
			triangles.Add(next);
			triangles.Add(current);
		}
		
		mesh.vertices = vertices.ToArray();
		mesh.uv = uvs.ToArray();
		mesh.triangles = triangles.ToArray();
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		
		return mesh;
	}
	
	/// <summary>
	/// Update the visual collider position and visibility
	/// </summary>
	public void UpdateVisualCollider(bool isGrounded)
	{
		if (visualColliderObject == null) return;
		
		// Update visibility
		visualColliderObject.SetActive(showVisualCollider);
		
		if (!showVisualCollider) return;
		
		// Update position to match capsule center
		visualColliderObject.transform.localPosition = capsuleCenter;
		
		// Update color based on grounded state
		if (visualColliderRenderer != null && visualColliderRenderer.material != null)
		{
			Color currentColor = isGrounded ? Color.green : Color.red;
			currentColor.a = colliderColor.a; // Keep original alpha
			visualColliderRenderer.material.color = currentColor;
		}
	}
	
	/// <summary>
	/// Update the visual ground check position and visibility
	/// </summary>
	public void UpdateVisualGroundCheck(bool isGrounded)
	{
		if (visualGroundCheckObject == null) return;
		
		// Update visibility
		visualGroundCheckObject.SetActive(showVisualCollider);
		
		if (!showVisualCollider) return;
		
		// Update position to match capsule bottom
		float halfHeight = Mathf.Max(0, (capsuleHeight * 0.5f) - capsuleRadius);
		Vector3 groundCheckPosition = new Vector3(0, -halfHeight, 0);
		visualGroundCheckObject.transform.localPosition = groundCheckPosition;
		
		// Update color based on grounded state
		if (visualGroundCheckRenderer != null && visualGroundCheckRenderer.material != null)
		{
			Color currentColor = isGrounded ? Color.green : Color.yellow;
			currentColor.a = 0.5f; // Semi-transparent
			visualGroundCheckRenderer.material.color = currentColor;
		}
		
		// Update mesh if ground check distance changed
		if (visualGroundCheckMeshFilter != null)
		{
			visualGroundCheckMeshFilter.mesh = CreateGroundCheckMesh();
		}
	}
	
	/// <summary>
	/// Calculate capsule size automatically based on character bones
	/// </summary>
	public void CalculateAutoCapsuleSize(Transform cranium, Transform pelvis, Transform leftHandBolt, Transform rightHandBolt)
	{
		// Auto-size capsule if enabled
		if (autoSizeCapsule)
		{
			if (cranium == null || pelvis == null)
			{
				Debug.LogWarning("[CalculateAutoCapsuleSize] Cranium or Pelvis bone not assigned! Using default capsule size.");
				return;
			}
			
			// Calculate character height from pelvis to cranium
			float characterHeight = Vector3.Distance(pelvis.position, cranium.position);
			
			// Calculate character width using shoulder bones or model bounds
			float characterWidth = CalculateCharacterWidth(leftHandBolt, rightHandBolt);
			
			// Calculate new capsule dimensions
			float newHeight = characterHeight + capsuleHeightOffset;
			float newRadius = characterWidth * capsuleRadiusMultiplier;
			
			// Apply min/max constraints
			newHeight = Mathf.Clamp(newHeight, minCapsuleHeight, maxCapsuleHeight);
			newRadius = Mathf.Clamp(newRadius, minCapsuleRadius, maxCapsuleRadius);
			
			// Store base values for dynamic sizing
			baseCapsuleHeight = newHeight;
			baseCapsuleRadius = newRadius;
			baseCapsuleCenter = new Vector3(0, newHeight * 0.5f, 0);
			
			// Update capsule values
			capsuleHeight = newHeight;
			capsuleRadius = newRadius;
			capsuleCenter = baseCapsuleCenter;
			
			// Update visual collider if it exists
			if (visualColliderObject != null)
			{
				UpdateVisualColliderMesh();
			}
			
			Debug.Log($"[CalculateAutoCapsuleSize] Auto-sized capsule - Height: {newHeight:F2}, Radius: {newRadius:F2}, Center: {capsuleCenter}");
		}
	}
	
	/// <summary>
	/// Calculate character width for capsule radius
	/// </summary>
	private float CalculateCharacterWidth(Transform leftHandBolt, Transform rightHandBolt)
	{
		// Try to use shoulder bones if available
		if (leftHandBolt != null && rightHandBolt != null)
		{
			float shoulderWidth = Vector3.Distance(leftHandBolt.position, rightHandBolt.position);
			return shoulderWidth * 0.6f; // Use 60% of shoulder width for capsule radius
		}
		
		// Fallback: use model bounds
		Renderer[] renderers = GetComponentsInChildren<Renderer>();
		if (renderers.Length > 0)
		{
			Bounds combinedBounds = renderers[0].bounds;
			foreach (Renderer renderer in renderers)
			{
				combinedBounds.Encapsulate(renderer.bounds);
			}
			
			// Use the wider of X or Z dimensions
			float width = Mathf.Max(combinedBounds.size.x, combinedBounds.size.z);
			return width * 0.5f; // Convert to radius
		}
		
		// Ultimate fallback: use default radius
		Debug.LogWarning("[CalculateCharacterWidth] Could not determine character width, using default radius");
		return capsuleRadius;
	}
	
	/// <summary>
	/// Update visual collider mesh with new dimensions
	/// </summary>
	private void UpdateVisualColliderMesh()
	{
		if (visualColliderMeshFilter != null)
		{
			visualColliderMeshFilter.mesh = CreateCapsuleMesh();
		}
		
		// Also update ground check mesh
		if (visualGroundCheckMeshFilter != null)
		{
			visualGroundCheckMeshFilter.mesh = CreateGroundCheckMesh();
		}
	}
	
	/// <summary>
	/// Update capsule size based on current state (crouching/standing)
	/// </summary>
	public void UpdateCapsuleSizeForState(bool isCrouching)
	{
		if (!dynamicCapsuleSizing || !autoSizeCapsule) return;
		
		if (isCrouching)
		{
			// Use crouched dimensions
			capsuleHeight = baseCapsuleHeight * crouchHeightMultiplier;
			capsuleCenter = new Vector3(0, capsuleHeight * 0.5f, 0);
		}
		else
		{
			// Use standing dimensions
			capsuleHeight = baseCapsuleHeight;
			capsuleCenter = baseCapsuleCenter;
		}
		
		// Update visual collider if it exists
		if (visualColliderObject != null)
		{
			UpdateVisualColliderMesh();
		}
	}
	
	/// <summary>
	/// Clean up visual collider when destroyed
	/// </summary>
	private void OnDestroy()
	{
		if (visualColliderObject != null)
		{
			DestroyImmediate(visualColliderObject);
		}
		
		if (visualGroundCheckObject != null)
		{
			DestroyImmediate(visualGroundCheckObject);
		}
	}

}
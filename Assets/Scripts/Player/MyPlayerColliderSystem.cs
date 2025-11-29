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
	[SerializeField] private float groundCheckRadius = 0.1f;  // Radius of ground check visual (calculated from feet width)
	[SerializeField] private float groundCheckHeight = 0.1f;  // Height of ground check disk (0.1 or 0 for flat disk)

    [Header("Visual Collider Debug")]
	[SerializeField] private bool showVisualCollider = true;
	[SerializeField] private Color colliderColor = new Color(0, 1, 0, 0.3f);  // Semi-transparent green
	[SerializeField] private Material colliderMaterial;

	//Berechnet die Capsule-Größe basierend auf den Charakter-Bones (Cranium ↔ Pelvis)
	[Header("Auto Capsule Sizing")]
	[SerializeField] private bool autoSizeCapsule = true;
	[SerializeField] private bool dynamicCapsuleSizing = true;     // Adjust capsule size when crouching

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
	private float feetYLocal; // Local Y position of feet (lowest point) for ground check positioning
	private float feetWidth; // Width between leftFoot and rightFoot for ground check radius

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
	/// Capsule geht immer von Y=0 bis Y=height, center ist bei height * 0.5f
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
			// Center ist immer bei half height (so dass Capsule von 0 bis height geht)
			outCenter = new Vector3(0f, outHeight * 0.5f, 0f);

			if (dynamicCapsuleSizing && isCrouching)
			{
				outHeight = outHeight * 0.6f; // 60% height when crouching
				outCenter = new Vector3(0f, outHeight * 0.5f, 0f);
			}
		}
	}


	private void OnGUICustom(){
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
			// Try to find Unlit/Color shader, fallback to Standard if not found
			Shader shader = Shader.Find("Unlit/Color");
			if (shader == null)
			{
				shader = Shader.Find("Standard");
			}
			colliderMaterial = new Material(shader);
			colliderMaterial.color = colliderColor;
			
			// Enable transparency if using Standard shader
			if (shader.name == "Standard")
			{
				colliderMaterial.SetFloat("_Mode", 3); // Transparent mode
				colliderMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
				colliderMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
				colliderMaterial.SetInt("_ZWrite", 0);
				colliderMaterial.DisableKeyword("_ALPHATEST_ON");
				colliderMaterial.EnableKeyword("_ALPHABLEND_ON");
				colliderMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
				colliderMaterial.renderQueue = 3000;
			}
		}
		visualColliderRenderer.material = colliderMaterial;
		
		// Make sure it renders
		visualColliderRenderer.enabled = true;
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
	/// Capsule goes from Y=0 (feet) to Y=height (cranium), centered at height * 0.5f
	/// </summary>
	private Mesh CreateCapsuleMesh()
	{
		Mesh mesh = new Mesh();
		mesh.name = "CapsuleVisual";

		// Capsule parameters
		int segments = 16;
		int hemisphereRings = 8; // Rings for each hemisphere
		int cylinderRings = 4; // Rings for the cylinder part
		float radius = Mathf.Max(0.01f, capsuleRadius); // Ensure minimum radius
		float height = Mathf.Max(0.01f, capsuleHeight); // Ensure minimum height
		
		// Validate parameters
		if (radius <= 0f || height <= 0f)
		{
			Debug.LogWarning($"[CreateCapsuleMesh] Invalid capsule dimensions - Radius: {capsuleRadius}, Height: {capsuleHeight}");
			return mesh; // Return empty mesh
		}

		// Calculate vertices
		List<Vector3> vertices = new List<Vector3>();
		List<Vector2> uvs = new List<Vector2>();
		List<int> triangles = new List<int>();
		
		// Calculate cylinder height (total height minus the two hemispheres)
		float cylinderHeight = Mathf.Max(0f, height - radius * 2f);
		float cylinderBottom = radius;
		float cylinderTop = height - radius;
		
		int vertexOffset = 0;
		
		// Generate vertices for TOP hemisphere (from cylinderTop to height)
		for (int ring = 0; ring <= hemisphereRings; ring++)
		{
			float v = (float)ring / hemisphereRings;
			float phi = v * Mathf.PI / 2f; // From 0 to PI/2
			
			for (int seg = 0; seg <= segments; seg++)
			{
				float u = (float)seg / segments;
				float theta = u * Mathf.PI * 2f;
				
				float x = Mathf.Cos(theta) * Mathf.Sin(phi) * radius;
				float y = cylinderTop + Mathf.Cos(phi) * radius;
				float z = Mathf.Sin(theta) * Mathf.Sin(phi) * radius;
				
				vertices.Add(new Vector3(x, y, z));
				uvs.Add(new Vector2(u, v));
			}
		}
		vertexOffset = vertices.Count;
		
		// Generate vertices for CYLINDER part (middle section from radius to height - radius)
		int cylinderVertexOffset = vertices.Count;
		if (cylinderHeight > 0f)
		{
			for (int ring = 0; ring <= cylinderRings; ring++)
			{
				float v = (float)ring / cylinderRings;
				float y = Mathf.Lerp(cylinderBottom, cylinderTop, v);
				
				for (int seg = 0; seg <= segments; seg++)
				{
					float u = (float)seg / segments;
					float theta = u * Mathf.PI * 2f;
					
					float x = Mathf.Cos(theta) * radius;
					float z = Mathf.Sin(theta) * radius;
					
					vertices.Add(new Vector3(x, y, z));
					uvs.Add(new Vector2(u, v));
				}
			}
		}
		
		// Generate vertices for BOTTOM hemisphere (from 0 to radius)
		int bottomVertexOffset = vertices.Count; // Set BEFORE adding bottom hemisphere vertices
		for (int ring = 0; ring <= hemisphereRings; ring++)
		{
			float v = (float)ring / hemisphereRings;
			float phi = (1f - v) * Mathf.PI / 2f; // From PI/2 to 0 (inverted)
			
			for (int seg = 0; seg <= segments; seg++)
			{
				float u = (float)seg / segments;
				float theta = u * Mathf.PI * 2f;
				
				float x = Mathf.Cos(theta) * Mathf.Sin(phi) * radius;
				float y = Mathf.Cos(phi) * radius; // Goes from 0 (at phi=PI/2) to radius (at phi=0)
				float z = Mathf.Sin(theta) * Mathf.Sin(phi) * radius;
				
				vertices.Add(new Vector3(x, y, z));
				uvs.Add(new Vector2(u, v));
			}
		}
		
		// Generate triangles for TOP hemisphere
		for (int ring = 0; ring < hemisphereRings; ring++)
		{
			for (int seg = 0; seg < segments; seg++)
			{
				int current = ring * (segments + 1) + seg;
				int next = (ring + 1) * (segments + 1) + seg;
				
				// Validate indices (seg can be at most segments-1, so current+1 and next+1 are valid)
				if (current + 1 >= vertices.Count || next + 1 >= vertices.Count)
				{
					Debug.LogError($"[CreateCapsuleMesh] Invalid top hemisphere indices - current: {current}, next: {next}, vertexCount: {vertices.Count}");
					continue;
				}
				
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
		
		// Generate triangles for CYLINDER part
		if (cylinderHeight > 0f)
		{
			for (int ring = 0; ring < cylinderRings; ring++)
			{
				for (int seg = 0; seg < segments; seg++)
				{
					int current = cylinderVertexOffset + ring * (segments + 1) + seg;
					int next = cylinderVertexOffset + (ring + 1) * (segments + 1) + seg;
					
					// Validate indices (seg can be at most segments-1, so current+1 and next+1 are valid)
					if (current + 1 >= vertices.Count || next + 1 >= vertices.Count)
					{
						Debug.LogError($"[CreateCapsuleMesh] Invalid cylinder indices - current: {current}, next: {next}, vertexCount: {vertices.Count}");
						continue;
					}
					
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
		}
		else
		{
			// If no cylinder, connect top hemisphere directly to bottom hemisphere
			// Top hemisphere last ring connects to bottom hemisphere first ring
			int topLastRing = hemisphereRings;
			int bottomFirstRing = 0;
			
			for (int seg = 0; seg < segments; seg++)
			{
				int topCurrent = topLastRing * (segments + 1) + seg;
				int topNext = topLastRing * (segments + 1) + seg + 1; // seg+1 is valid since seg < segments
				int bottomCurrent = bottomVertexOffset + bottomFirstRing * (segments + 1) + seg;
				int bottomNext = bottomVertexOffset + bottomFirstRing * (segments + 1) + seg + 1;
				
				// Validate indices
				if (topCurrent >= vertices.Count || topNext >= vertices.Count || 
				    bottomCurrent >= vertices.Count || bottomNext >= vertices.Count)
				{
					Debug.LogError($"[CreateCapsuleMesh] Invalid connection indices - topCurrent: {topCurrent}, topNext: {topNext}, bottomCurrent: {bottomCurrent}, bottomNext: {bottomNext}, vertexCount: {vertices.Count}");
					continue;
				}
				
				// Connect top to bottom
				triangles.Add(topCurrent);
				triangles.Add(bottomNext);
				triangles.Add(topNext);
				
				triangles.Add(topCurrent);
				triangles.Add(bottomCurrent);
				triangles.Add(bottomNext);
			}
		}
		
		// Generate triangles for BOTTOM hemisphere
		for (int ring = 0; ring < hemisphereRings; ring++)
		{
			for (int seg = 0; seg < segments; seg++)
			{
				int current = bottomVertexOffset + ring * (segments + 1) + seg;
				int next = bottomVertexOffset + (ring + 1) * (segments + 1) + seg;
				
				// Validate indices
				if (current >= vertices.Count || current + 1 >= vertices.Count || 
				    next >= vertices.Count || next + 1 >= vertices.Count)
				{
					Debug.LogError($"[CreateCapsuleMesh] Invalid bottom hemisphere indices - ring: {ring}, seg: {seg}, current: {current}, next: {next}, bottomVertexOffset: {bottomVertexOffset}, vertexCount: {vertices.Count}");
					continue;
				}
				
				// First triangle (inverted winding for bottom)
				triangles.Add(current);
				triangles.Add(current + 1);
				triangles.Add(next);
				
				// Second triangle (inverted winding for bottom)
				triangles.Add(current + 1);
				triangles.Add(next + 1);
				triangles.Add(next);
			}
		}
		
		// Add bottom cap (flat circle at Y=0)
		int bottomCapCenterIndex = vertices.Count;
		vertices.Add(new Vector3(0, 0, 0)); // Center at Y=0
		uvs.Add(new Vector2(0.5f, 0.5f));
		
		// Add bottom cap circle vertices
		int bottomCapCircleStart = vertices.Count;
		for (int seg = 0; seg <= segments; seg++)
		{
			float u = (float)seg / segments;
			float theta = u * Mathf.PI * 2f;
			
			float x = Mathf.Cos(theta) * radius;
			float z = Mathf.Sin(theta) * radius;
			
			vertices.Add(new Vector3(x, 0, z));
			uvs.Add(new Vector2(u, 0.5f));
		}
		
		// Generate bottom cap triangles (fan from center)
		// Note: bottomCapCircleStart is the first circle vertex index
		// We have segments+1 vertices in the circle (indices bottomCapCircleStart to bottomCapCircleStart + segments)
		for (int seg = 0; seg < segments; seg++)
		{
			int current = bottomCapCircleStart + seg;
			int next = bottomCapCircleStart + seg + 1; // Next vertex (seg+1, which is <= segments, so it's valid)
			
			// Validate indices before adding
			if (current >= vertices.Count || next >= vertices.Count || bottomCapCenterIndex >= vertices.Count)
			{
				Debug.LogError($"[CreateCapsuleMesh] Invalid bottom cap indices - current: {current}, next: {next}, center: {bottomCapCenterIndex}, vertexCount: {vertices.Count}");
				continue;
			}
			
			triangles.Add(bottomCapCenterIndex);
			triangles.Add(next);
			triangles.Add(current);
		}
		
		// Validate mesh data
		if (vertices.Count == 0)
		{
			Debug.LogWarning("[CreateCapsuleMesh] No vertices generated!");
			return mesh;
		}
		
		if (triangles.Count == 0)
		{
			Debug.LogWarning("[CreateCapsuleMesh] No triangles generated!");
			return mesh;
		}
		
		// Validate triangle indices
		int maxVertexIndex = vertices.Count - 1;
		for (int i = 0; i < triangles.Count; i++)
		{
			if (triangles[i] < 0 || triangles[i] > maxVertexIndex)
			{
				Debug.LogError($"[CreateCapsuleMesh] Invalid triangle index: {triangles[i]} (max: {maxVertexIndex})");
				return mesh;
			}
		}
		
		mesh.vertices = vertices.ToArray();
		mesh.uv = uvs.ToArray();
		mesh.triangles = triangles.ToArray();
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		
		//Debug.Log($"[CreateCapsuleMesh] Generated mesh - Vertices: {vertices.Count}, Triangles: {triangles.Count / 3}, Radius: {radius:F2}, Height: {height:F2}");
		
		return mesh;
	}
	
	/// <summary>
	/// Create a ground check mesh for visual representation
	/// Creates a flat disk (or very thin cylinder) positioned directly under the feet
	/// </summary>
	private Mesh CreateGroundCheckMesh()
	{
		Mesh mesh = new Mesh();
		mesh.name = "GroundCheckVisual";
		
		// Ground check parameters - flat disk
		int segments = 32; // More segments for smoother circle
		float radius = groundCheckRadius; // Radius based on feet width
		float height = groundCheckHeight; // Very thin disk (0.1 or 0)
		
		// Calculate vertices for a flat disk
		List<Vector3> vertices = new List<Vector3>();
		List<Vector2> uvs = new List<Vector2>();
		List<int> triangles = new List<int>();
		
		// Center vertex (top)
		int centerTopIndex = vertices.Count;
		vertices.Add(new Vector3(0, height * 0.5f, 0));
		uvs.Add(new Vector2(0.5f, 0.5f));
		
		// Center vertex (bottom) - only if height > 0
		int centerBottomIndex = -1;
		if (height > 0f)
		{
			centerBottomIndex = vertices.Count;
			vertices.Add(new Vector3(0, -height * 0.5f, 0));
			uvs.Add(new Vector2(0.5f, 0.5f));
		}
		
		// Generate vertices for top circle
		int topCircleStartIndex = vertices.Count;
		for (int seg = 0; seg <= segments; seg++)
		{
			float u = (float)seg / segments;
			float theta = u * Mathf.PI * 2;
			
			float x = Mathf.Cos(theta) * radius;
			float z = Mathf.Sin(theta) * radius;
			
			vertices.Add(new Vector3(x, height * 0.5f, z));
			uvs.Add(new Vector2(u, 0.5f));
		}
		
		// Generate vertices for bottom circle (if height > 0)
		int bottomCircleStartIndex = -1;
		if (height > 0f && centerBottomIndex >= 0)
		{
			bottomCircleStartIndex = vertices.Count;
			for (int seg = 0; seg <= segments; seg++)
			{
				float u = (float)seg / segments;
				float theta = u * Mathf.PI * 2;
				
				float x = Mathf.Cos(theta) * radius;
				float z = Mathf.Sin(theta) * radius;
				
				vertices.Add(new Vector3(x, -height * 0.5f, z));
				uvs.Add(new Vector2(u, 0.5f));
			}
		}
		
		// Generate top face triangles (fan from center)
		for (int seg = 0; seg < segments; seg++)
		{
			int current = topCircleStartIndex + seg;
			int next = topCircleStartIndex + ((seg + 1) % (segments + 1));
			
			triangles.Add(centerTopIndex);
			triangles.Add(next);
			triangles.Add(current);
		}
		
		// Generate bottom face triangles (if height > 0)
		if (height > 0f && centerBottomIndex >= 0 && bottomCircleStartIndex >= 0)
		{
			for (int seg = 0; seg < segments; seg++)
			{
				int current = bottomCircleStartIndex + seg;
				int next = bottomCircleStartIndex + ((seg + 1) % (segments + 1));
				
				triangles.Add(centerBottomIndex);
				triangles.Add(current);
				triangles.Add(next);
			}
			
			// Generate side triangles (connecting top and bottom circles)
			for (int seg = 0; seg < segments; seg++)
			{
				int topCurrent = topCircleStartIndex + seg;
				int topNext = topCircleStartIndex + ((seg + 1) % (segments + 1));
				int bottomCurrent = bottomCircleStartIndex + seg;
				int bottomNext = bottomCircleStartIndex + ((seg + 1) % (segments + 1));
				
				// First triangle
				triangles.Add(topCurrent);
				triangles.Add(bottomNext);
				triangles.Add(topNext);
				
				// Second triangle
				triangles.Add(topCurrent);
				triangles.Add(bottomCurrent);
				triangles.Add(bottomNext);
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
	/// Update the visual collider position and visibility
	/// </summary>
	public void UpdateVisualCollider(bool isGrounded)
	{
		if (visualColliderObject == null) return;
		
		// Update visibility
		visualColliderObject.SetActive(showVisualCollider);
		
		if (!showVisualCollider) return;
		
		// Update position to match capsule center
		visualColliderObject.transform.localPosition = Vector3.zero;
		
		// Update mesh if dimensions changed
		if (visualColliderMeshFilter != null)
		{
			visualColliderMeshFilter.mesh = CreateCapsuleMesh();
		}
		
		// Update color based on grounded state
		if (visualColliderRenderer != null)
		{
			if (visualColliderRenderer.material == null)
			{
				// Recreate material if it was lost
				if (colliderMaterial == null)
				{
					colliderMaterial = new Material(Shader.Find("Unlit/Color"));
					colliderMaterial.color = colliderColor;
				}
				visualColliderRenderer.material = colliderMaterial;
			}
			
			Color currentColor = isGrounded ? Color.green : Color.red;
			currentColor.a = colliderColor.a; // Keep original alpha
			visualColliderRenderer.material.color = currentColor;
		}
	}
	
	/// <summary>
	/// Update the visual ground check position and visibility
	/// Ground check is positioned directly under the feet
	/// </summary>
	public void UpdateVisualGroundCheck(bool isGrounded)
	{
		if (visualGroundCheckObject == null) return;
		
		// Update visibility
		visualGroundCheckObject.SetActive(showVisualCollider);
		
		if (!showVisualCollider) return;
		
		// Update position to match feet position (lowestY in local space)
		// Position directly at feet level (feetYLocal), or slightly below if height > 0
		Vector3 groundCheckPosition = new Vector3(0, feetYLocal - groundCheckHeight * 0.5f, 0);
		visualGroundCheckObject.transform.localPosition = groundCheckPosition;
		
		// Update color based on grounded state
		if (visualGroundCheckRenderer != null && visualGroundCheckRenderer.material != null)
		{
			Color currentColor = isGrounded ? Color.green : Color.yellow;
			currentColor.a = 0.5f; // Semi-transparent
			visualGroundCheckRenderer.material.color = currentColor;
		}
		
		// Update mesh if ground check radius changed
		if (visualGroundCheckMeshFilter != null)
		{
			visualGroundCheckMeshFilter.mesh = CreateGroundCheckMesh();
		}
	}
	
	/// <summary>
	/// Calculate capsule size automatically based on character bones
	/// Capsule goes from Y=0 (feet) to Y=height (cranium)
	/// Width is based on leftFoot to rightFoot
	/// </summary>
	public void CalculateAutoCapsuleSize(Transform cranium, Transform pelvis, Transform leftHandBolt, Transform rightHandBolt, Transform leftFoot, Transform rightFoot)
	{
		// Auto-size capsule if enabled
		if (autoSizeCapsule)
		{
			if (cranium == null)
			{
				Debug.LogWarning("[CalculateAutoCapsuleSize] Cranium bone not assigned! Using default capsule size.");
				return;
			}
			
			// Convert bone positions to local space relative to this transform
			Vector3 craniumLocal = transform.InverseTransformPoint(cranium.position);
			float highestY = craniumLocal.y;
			
			// Get lowest Y from feet (should be at Y=0 or close to it)
			float lowestY = 0f;
			if (leftFoot != null && rightFoot != null)
			{
				Vector3 leftFootLocal = transform.InverseTransformPoint(leftFoot.position);
				Vector3 rightFootLocal = transform.InverseTransformPoint(rightFoot.position);
				lowestY = Mathf.Min(leftFootLocal.y, rightFootLocal.y);
			}
			else if (leftFoot != null)
			{
				Vector3 leftFootLocal = transform.InverseTransformPoint(leftFoot.position);
				lowestY = leftFootLocal.y;
			}
			else if (rightFoot != null)
			{
				Vector3 rightFootLocal = transform.InverseTransformPoint(rightFoot.position);
				lowestY = rightFootLocal.y;
			}
			else
			{
				Debug.LogWarning("[CalculateAutoCapsuleSize] LeftFoot or RightFoot not assigned! Assuming feet at Y=0.");
				lowestY = 0f;
			}
			
			// Calculate capsule height: from feet (lowestY) to cranium (highestY)
			// Adjust height so capsule starts at Y=0
			float newHeight = highestY - lowestY;
			
			// Calculate character width: from leftFoot to rightFoot (full width, not radius)
			float characterWidth = CalculateCharacterWidthFromFeet(leftFoot, rightFoot);
			
			// Radius: half the width from feet
			float newRadius = characterWidth * 0.5f;
			
			// Calculate capsule center: at half height (so capsule goes from 0 to height)
			// Since we want capsule from 0 to height, center is at height * 0.5f
			float centerY = newHeight * 0.5f;
			baseCapsuleCenter = new Vector3(0, centerY, 0);
			
			// Store feet Y position for ground check positioning
			feetYLocal = lowestY;
			
			// Calculate ground check width: from leftFoot to rightFoot
			if (leftFoot != null && rightFoot != null)
			{
				Vector3 leftFootLocal = transform.InverseTransformPoint(leftFoot.position);
				Vector3 rightFootLocal = transform.InverseTransformPoint(rightFoot.position);
				Vector3 horizontalDiff = new Vector3(leftFootLocal.x - rightFootLocal.x, 0f, leftFootLocal.z - rightFootLocal.z);
				feetWidth = horizontalDiff.magnitude;
				groundCheckRadius = feetWidth * 0.5f; // Radius is half the width
			}
			else
			{
				// Fallback: use a default radius
				feetWidth = newRadius * 2f;
				groundCheckRadius = newRadius;
			}
			
			// Store base values for dynamic sizing
			baseCapsuleHeight = newHeight;
			baseCapsuleRadius = newRadius;
			
			// Update capsule values
			capsuleHeight = newHeight;
			capsuleRadius = newRadius;
			capsuleCenter = baseCapsuleCenter;
			
			// Update visual collider if it exists
			if (visualColliderObject != null)
			{
				UpdateVisualColliderMesh();
			}
			
			// Update visual ground check if it exists
			if (visualGroundCheckObject != null)
			{
				UpdateVisualGroundCheckMesh();
			}
			
			Debug.Log($"[CalculateAutoCapsuleSize] Auto-sized capsule - Height: {newHeight:F2} (from {lowestY:F2} to {highestY:F2}), Radius: {newRadius:F2}, Center: {capsuleCenter}, GroundCheckRadius: {groundCheckRadius:F2}, FeetWidth: {feetWidth:F2}");
		}
	}
	
	/// <summary>
	/// Calculate character width for capsule radius using feet
	/// </summary>
	private float CalculateCharacterWidthFromFeet(Transform leftFoot, Transform rightFoot)
	{
		// Use feet width for capsule radius
		if (leftFoot != null && rightFoot != null)
		{
			// Calculate horizontal distance (ignore Y difference)
			Vector3 leftPos = leftFoot.position;
			Vector3 rightPos = rightFoot.position;
			Vector3 horizontalDiff = new Vector3(leftPos.x - rightPos.x, 0f, leftPos.z - rightPos.z);
			float feetWidth = horizontalDiff.magnitude;
			
			// Return the full width (will be converted to radius in CalculateAutoCapsuleSize)
			return feetWidth;
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
			return width; // Return full width (will be converted to radius in CalculateAutoCapsuleSize)
		}
		
		// Ultimate fallback: use default radius
		Debug.LogWarning("[CalculateCharacterWidthFromFeet] Could not determine character width, using default radius");
		return capsuleRadius > 0f ? capsuleRadius * 2f : 10f; // Return width (radius * 2)
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
	/// Update visual ground check mesh with new dimensions
	/// </summary>
	private void UpdateVisualGroundCheckMesh()
	{
		if (visualGroundCheckMeshFilter != null)
		{
			visualGroundCheckMeshFilter.mesh = CreateGroundCheckMesh();
		}
	}
	
	/// <summary>
	/// Update capsule size based on current state (crouching/standing)
	/// Capsule always goes from Y=0 to Y=height, center is at height * 0.5f
	/// </summary>
	public void UpdateCapsuleSizeForState(bool isCrouching)
	{
		if (!dynamicCapsuleSizing || !autoSizeCapsule) return;
		
		if (isCrouching)
		{
			// Use crouched dimensions (60% of standing height)
			capsuleHeight = baseCapsuleHeight * 0.6f;
			// Center is always at half height (so capsule goes from 0 to height)
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
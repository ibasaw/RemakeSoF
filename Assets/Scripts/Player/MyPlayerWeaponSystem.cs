using UnityEngine;
using System;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Collections.Generic;
using System.Linq;

using Tolik.RemakeSoF.Data;
using Tolik.RemakeSoF.Runtime;

[DisallowMultipleComponent]
public class MyPlayerWeaponSystem : MonoBehaviour
{
	[SerializeField] private float attachedWeaponZOverride = -90f;
	[SerializeField] private float attachedWeaponScaleOverride = 0.01f;

	[Header("Visual Collider Debug")]
	[SerializeField] private bool showVisualCollider = true;
	[SerializeField] private Color hitColor = new Color(0, 1, 0, 0.3f);  // Semi-transparent green
	[SerializeField] private Material hitMaterial;
	[SerializeField] private float hitMarkerSize = 4f;
	[SerializeField] private float hitMarkerDuration = 3f;
	private int hitMarkerCounter = 0;
	[SerializeField] private GameObject WorldHitmarkerContainer;

	[Header("Weapons (read-only from SoF2_Weapons.json)")]
	[SerializeField] private string[] availableWeaponNames = Array.Empty<string>();
	private JArray cachedWeaponsData;
	private bool materialsLoaded = false;
	private string lastMaterialHit = "None";
	private Dictionary<string, MaterialInfo> materialInfos = new Dictionary<string, MaterialInfo>(StringComparer.OrdinalIgnoreCase);

	private string currentWeaponName;

	private GameObject currentWeaponPrefab;

	private WeaponHitReporter currentHitReporter;

	public void InitializeWeaponSystem()
	{
		JArray weaponsArray = LoadWeaponsData();
		if (weaponsArray == null)
		{
			availableWeaponNames = Array.Empty<string>();
			cachedWeaponsData = null;
			return;
		}

		var names = new List<string>();
		foreach (JToken weaponObj in weaponsArray)
		{
			if (weaponObj is JObject o)
			{
				string n = o.Value<string>("name");
				if (!string.IsNullOrEmpty(n) && !names.Contains(n, StringComparer.OrdinalIgnoreCase))
				{
					names.Add(n);
				}
			}
		}
		names.Sort(StringComparer.OrdinalIgnoreCase);
		availableWeaponNames = names.ToArray();
		cachedWeaponsData = weaponsArray;

		LoadSurfaceData();
	}

	/// <summary>
	/// Attach the weapon to the hand bolt
	/// </summary>
	public void AttachWeapon(string weaponName, Transform handBolt)
	{
		if (string.IsNullOrEmpty(weaponName))
		{
			Debug.LogWarning("AttachWeapon called with empty weaponName");
			return;
		}

		if (availableWeaponNames == null || availableWeaponNames.Length == 0)
		{
			Debug.LogWarning("No available weapons loaded. Call InitializeWeaponSystem() first.");
			return;
		}

		bool exists = availableWeaponNames.Contains(weaponName, StringComparer.OrdinalIgnoreCase);
		if (!exists)
		{
			Debug.LogWarning($"Weapon '{weaponName}' not found in availableWeaponNames.");
			return;
		}

		currentWeaponPrefab = currentWryLoadWeaponPrefabByName(weaponName);
		if (handBolt == null)
		{
			Debug.LogWarning("Right hand bolt is not assigned in the inspector!");
			return;
		}

		// Instantiate from prefab (no existing Transform reference)
		if (currentWeaponPrefab != null)
		{
			GameObject weaponInstance = Instantiate(currentWeaponPrefab, handBolt);
			Transform weaponTransform = weaponInstance.transform;
			weaponTransform.localPosition = Vector3.zero;
			// Apply Z rotation override to fix SoF2 weapon axis issues
			weaponTransform.localRotation = Quaternion.Euler(0f, 0f, attachedWeaponZOverride);
			// Apply scale override to fix SoF2 weapon size issues
			weaponTransform.localScale = Vector3.one * attachedWeaponScaleOverride;
			currentWeaponName = weaponName;
			Debug.Log($"[WeaponSystem] Instantiated and attached start weapon prefab '{currentWeaponPrefab.name}' to right hand bolt '{handBolt.name}' with Z-rotation: {attachedWeaponZOverride}° and scale: {attachedWeaponScaleOverride}");

			// If this is a knife (category 1), attach hit reporter
			int? category = GetCategoryCodeForWeapon(currentWeaponName);
			if (category == 1)
			{
				var reporter = weaponInstance.GetComponent<WeaponHitReporter>();
				if (reporter == null) reporter = weaponInstance.AddComponent<WeaponHitReporter>();
				reporter.owner = this;
				var tip = weaponInstance.transform.Find("BladeTip") ?? weaponInstance.transform.Find("Tip") ?? weaponInstance.transform;
				reporter.SetTipTransform(tip);
				currentHitReporter = reporter;
				// Enable hit window immediately so testing works without animation events
				currentHitReporter.StartKnifeWindow();
			}
		}
		else
		{
			Debug.LogWarning($"No prefab found for weapon '{weaponName}'. Ensure a matching prefab exists under Resources.");
		}
	}

	private GameObject currentWryLoadWeaponPrefabByName(string weaponName)
	{
		// Try common Resources paths; adjust as needed
		string[] candidates = new string[]
		{
			weaponName,
			"Weapons/" + weaponName,
			"Prefabs/" + weaponName
		};
		foreach (var path in candidates)
		{
			var prefab = Resources.Load<GameObject>(path);
			if (prefab != null) return prefab;
		}
		return null;
	}

	private int? GetCategoryCodeForWeapon(string weaponName)
	{
		if (string.IsNullOrEmpty(weaponName) || cachedWeaponsData == null) return null;
		foreach (JToken t in cachedWeaponsData)
		{
			if (t is not JObject o) continue;
			string name = o.Value<string>("name");
			if (!string.Equals(name, weaponName, StringComparison.OrdinalIgnoreCase)) continue;
			JObject wpn = o.Value<JObject>("wpn");
			if (wpn == null) return null;
			JToken catTok;
			if (!wpn.TryGetValue("category", StringComparison.OrdinalIgnoreCase, out catTok) || catTok == null) return null;
			if (catTok.Type == JTokenType.Integer) return catTok.Value<int>();
			int parsed;
			if (int.TryParse(catTok.ToString(), out parsed)) return parsed;
			return null;
		}
		return null;
	}

	public void HandleKnifeHit(Collision collision)
	{
		if (collision == null) return;
		Vector3 p = (collision.contacts != null && collision.contacts.Length > 0) ? collision.contacts[0].point : collision.collider.ClosestPoint(transform.position);
		lastMaterialHit = GetMaterialType(collision.collider);

		Debug.Log($"[WeaponSystem] Knife hit collision with '{collision.collider.gameObject.name}' at position {p}. Material: {lastMaterialHit}");
		DrawHitCross(p, 0.06f, 0.35f);
		SpawnWorldHitMarker(p);
	}

	public void HandleKnifeHit(Collider other)
	{
		if (other == null) return;
		Debug.Log($"[WeaponSystem] Knife trigger with '{other.gameObject.name}'");
		Vector3 p = other.ClosestPoint(transform.position);
		lastMaterialHit = GetMaterialType(other);
		DrawHitCross(p, 0.06f, 0.35f);
		SpawnWorldHitMarker(p);
	}

	public void HandleKnifeHitAt(Vector3 point, Collider other)
	{
		lastMaterialHit = GetMaterialType(other);
		Debug.Log($"[WeaponSystem] Knife hit '{(other != null ? other.gameObject.name : "unknown")}' at {point}. Material: {lastMaterialHit}");
		DrawHitCross(point, 0.06f, 0.35f);
		SpawnWorldHitMarker(point);
	}

	// Expose animation hooks so controller/animation events can enable hit window
	public void StartKnifeHitWindow()
	{
		if (currentHitReporter != null) currentHitReporter.StartKnifeWindow();
	}
	public void EndKnifeHitWindow()
	{
		if (currentHitReporter != null) currentHitReporter.EndKnifeWindow();
	}

	private void DrawHitCross(Vector3 position, float halfSize, float duration)
	{
		Color c = Color.red;
		Debug.DrawLine(position + new Vector3(-halfSize, 0f, 0f), position + new Vector3(halfSize, 0f, 0f), c, duration);
		Debug.DrawLine(position + new Vector3(0f, -halfSize, 0f), position + new Vector3(0f, halfSize, 0f), c, duration);
		Debug.DrawLine(position + new Vector3(0f, 0f, -halfSize), position + new Vector3(0f, 0f, halfSize), c, duration);
	}

	private void SpawnWorldHitMarker(Vector3 position)
	{
		if (!showVisualCollider) return;
		// Prefer the assigned container, otherwise create/find a default one
		GameObject container = WorldHitmarkerContainer;
		if (container == null)
		{
			container = GameObject.Find("WorldHitmarkerContainer") ?? new GameObject("WorldHitmarkerContainer");
			WorldHitmarkerContainer = container;
		}
		string crossName = $"crosshit_{++hitMarkerCounter}";
		GameObject parent = new GameObject(crossName);
		parent.transform.position = position;
		parent.transform.SetParent(container.transform, true);
		void SetupLine(Vector3 a, Vector3 b)
		{
			var lineGo = new GameObject(crossName + "_line");
			lineGo.transform.SetParent(parent.transform, false);
			var lr = lineGo.AddComponent<LineRenderer>();
			lr.positionCount = 2;
			lr.useWorldSpace = true;
			lr.SetPosition(0, a);
			lr.SetPosition(1, b);
			lr.startWidth = lr.endWidth = Mathf.Max(0.003f, hitMarkerSize * 0.25f);
			if (hitMaterial != null)
			{
				lr.material = hitMaterial;
			}
			else
			{
				var shader = Shader.Find("Unlit/Color");
				if (shader != null)
				{
					var mat = new Material(shader);
					mat.color = hitColor;
					lr.material = mat;
				}
			}
			lr.startColor = lr.endColor = hitColor;
			lr.numCapVertices = 2;
			lr.textureMode = LineTextureMode.Stretch;
		}
		float s = hitMarkerSize;
		SetupLine(position + new Vector3(-s, 0f, 0f), position + new Vector3(s, 0f, 0f));
		SetupLine(position + new Vector3(0f, -s, 0f), position + new Vector3(0f, s, 0f));
		//SetupLine(position + new Vector3(0f, 0f, -s), position + new Vector3(0f, 0f, s));
		Destroy(parent, Mathf.Max(0.05f, hitMarkerDuration));
	}

	private class WeaponHitReporter : MonoBehaviour
	{
		public MyPlayerWeaponSystem owner;
		private Transform tip;
		private Vector3 lastTipWorldPos;
		private bool knifeWindowActive;
		private int selfLayer;

		public void SetTipTransform(Transform t)
		{
			tip = t;
			lastTipWorldPos = tip != null ? tip.position : transform.position;
			selfLayer = gameObject.layer;
		}

		public void StartKnifeWindow()
		{
			knifeWindowActive = true;
		}

		public void EndKnifeWindow()
		{
			knifeWindowActive = false;
		}
		private void OnCollisionEnter(Collision collision)
		{
			if (owner != null && knifeWindowActive) owner.HandleKnifeHit(collision);
		}
		private void OnTriggerEnter(Collider other)
		{
			if (owner != null && knifeWindowActive)
			{
				Vector3 origin = tip != null ? tip.position : transform.position;
				Vector3 p = other != null ? other.ClosestPoint(origin) : origin;
				owner.HandleKnifeHitAt(p, other);
			}
		}

		private void Update()
		{
			if (!knifeWindowActive) { lastTipWorldPos = tip != null ? tip.position : transform.position; return; }
			Vector3 currentPos = tip != null ? tip.position : transform.position;
			Vector3 dir = currentPos - lastTipWorldPos;
			float dist = dir.magnitude;
			if (dist > 0.0001f)
			{
				// Cast a sphere along the tip movement to detect contacts without rigidbody
				Ray ray = new Ray(lastTipWorldPos, dir.normalized);
				RaycastHit hit;
				if (Physics.SphereCast(ray, 0.035f, out hit, dist, ~0, QueryTriggerInteraction.Collide))
				{
					// ignore self hits
					if (hit.collider != null && hit.collider.gameObject != null)
					{
						if (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform))
						{
							// skip
						}
						else if (owner != null)
						{
							owner.HandleKnifeHitAt(hit.point, hit.collider);
						}
					}
				}
			}
			lastTipWorldPos = currentPos;
		}
	}

	/// <summary>
	/// Load the SoF2_Weapons.json via shared JsonDataReader and parse it as JArray
	/// </summary>
	public JArray LoadWeaponsData()
	{
		string json = JsonDataReader.TryLoadJsonText("SoF2_Weapons");
		if (string.IsNullOrEmpty(json))
		{
			Debug.Log("[MyPlayerWeaponSystem] Keine SoF2_Weapons.json gefunden!");
			return null;
		}

		try
		{
			return JArray.Parse(json);
		}
		catch (Exception ex)
		{
			Debug.LogError("[MyPlayerWeaponSystem] JSON Parse Error: " + ex);
			return null;
		}
	}

	/// <summary>
	/// Get the material type from the hit collider.
	/// </summary>
	public string GetMaterialType(Collider hitCollider)
	{
		if (hitCollider == null) return "concrete"; // Default

		string detectedMaterial = "concrete"; // Default
		string detectionMethod = "default";

		// Try to get Ghoul2Meta component from the hit object
		if (hitCollider.TryGetComponent<Ghoul2Meta>(out var meta))
		{
			// Try to get q3map_material property
			string q3MapMaterialName = meta.Q3MapMaterial;
			if (!string.IsNullOrEmpty(q3MapMaterialName) && materialInfos.ContainsKey(q3MapMaterialName))
			{
				detectedMaterial = q3MapMaterialName;
				detectionMethod = $"q3MapMaterialName '{q3MapMaterialName}' found in materialInfos";
			}
		}
		else
		{
			detectionMethod = $"no Ghoul2Meta, using default for object name: '{hitCollider.gameObject.name}'";
		}
		// Debug.Log($"Material detected as '{detectedMaterial}' via {detectionMethod}");
		return detectedMaterial;
	}

	/// <summary>
	/// Load all surface properties from Data/SoF2_data_per_surface.json
	/// </summary>
	private void LoadSurfaceData()
	{
		materialInfos.Clear();

		string json = JsonDataReader.TryLoadJsonText("SoF2_data_per_surface");
		if (string.IsNullOrEmpty(json))
		{
			Debug.LogWarning("[LoadSurfaceData] Keine JSON-Datei für Oberflächen-Daten gefunden!");
			return;
		}

		JObject root;
		try
		{
			root = JObject.Parse(json);
		}
		catch (Exception ex)
		{
			Debug.LogError("[LoadSurfaceData] JSON Parse Error: " + ex);
			return;
		}

		foreach (var prop in root.Properties())
		{
			string materialName = prop.Name;
			if (prop.Value is JObject matObj)
			{
				var info = new MaterialInfo
				{
					loudness = TryGetDouble(matObj, "loudness"),
					density = TryGetDouble(matObj, "density"),
					projectileBounce = TryGetDouble(matObj, "projectileBounce"),
					friction = TryGetDouble(matObj, "friction"),
					damage = TryGetDouble(matObj, "damage")
				};
				materialInfos[materialName] = info;
			}
		}
		materialsLoaded = true;
		Debug.Log($"[LoadSurfaceData] {materialInfos.Count} material definitions loaded.");
	}

	private double? TryGetDouble(JObject obj, string key)
	{
		if (obj != null && obj.TryGetValue(key, StringComparison.OrdinalIgnoreCase, out JToken tok))
		{
			if (tok.Type == JTokenType.Float || tok.Type == JTokenType.Integer)
				return tok.Value<double>();
			if (tok.Type == JTokenType.String && double.TryParse(tok.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double v))
				return v;
		}
		return null;
	}

	public void OnGUICustom()
	{
		GUIStyle valueStyle = new GUIStyle(GUI.skin.label)
		{
			fontSize = 20,
			normal = { textColor = Color.white }
		};

		GUI.Label(new Rect(10, Screen.height - 40, 600, 24), $"Last Material Hit: {lastMaterialHit}", valueStyle);
	}
}
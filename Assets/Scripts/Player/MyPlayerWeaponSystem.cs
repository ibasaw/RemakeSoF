using UnityEngine;
using System;
using Newtonsoft.Json.Linq;
using Utils;

[DisallowMultipleComponent]
public class MyPlayerWeaponSystem : MonoBehaviour
{
    [SerializeField] private float attachedWeaponZOverride = -90f;
	[SerializeField] private float attachedWeaponScaleOverride = 0.01f;

    /// <summary>
	/// Attach the weapon to the hand bolt
	/// </summary>
	public void AttachWeapon(GameObject weaponPrefab, Transform handBolt)
	{
		if (handBolt == null)
		{
			Debug.LogWarning("Right hand bolt is not assigned in the inspector!");
			return;
		}

		// Instantiate from prefab (no existing Transform reference)
		if (weaponPrefab != null)
		{
			GameObject weaponInstance = Instantiate(weaponPrefab, handBolt);
			Transform weaponTransform = weaponInstance.transform;
			weaponTransform.localPosition = Vector3.zero;
			// Apply Z rotation override to fix SoF2 weapon axis issues
			weaponTransform.localRotation = Quaternion.Euler(0f, 0f, attachedWeaponZOverride);
			// Apply scale override to fix SoF2 weapon size issues
			weaponTransform.localScale = Vector3.one * attachedWeaponScaleOverride;
			Debug.Log($"Instantiated and attached start weapon prefab '{weaponPrefab.name}' to right hand bolt '{handBolt.name}' with Z-rotation: {attachedWeaponZOverride}° and scale: {attachedWeaponScaleOverride}");
		}
		else
		{
			Debug.LogWarning("No weaponPrefab set in the inspector!");
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
}
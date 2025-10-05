using UnityEngine;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;
using Newtonsoft.Json.Linq;
using System.Linq;
using SoF2Remake.Data;

namespace SoF2Remake.Utils
{
	public static class JsonDataReader
	{
		// Load a JSON file as text. Searches Resources/Data (TextAsset) first, then Assets/Data on disk
		public static string TryLoadJsonText(string fileNameWithoutExtension)
		{
			if (string.IsNullOrEmpty(fileNameWithoutExtension)) return null;

			// 1) Resources/Data/<name> (TextAsset)
			TextAsset ta = Resources.Load<TextAsset>("Data/" + fileNameWithoutExtension);
			if (ta != null) return ta.text;

			// 2) Assets/Data/<name>.json (Editor & Standalone)
			string path = Path.Combine(Application.dataPath, "Data", fileNameWithoutExtension + ".json");
			if (File.Exists(path)) return File.ReadAllText(path);

			return null;
		}

		/// <summary>
		/// Load all surface properties from Data/SoF2_data_per_surface.json
		/// </summary>
		public static Dictionary<string, MaterialInfo> LoadSurfaceData()
		{
			string json = TryLoadJsonText("SoF2_data_per_surface");
			if (string.IsNullOrEmpty(json))
			{
				Debug.LogWarning("[LoadSurfaceData] Keine JSON-Datei für Oberflächen-Daten gefunden!");
				return null;
			}

			JObject root;
			try
			{
				root = JObject.Parse(json);
			}
			catch (Exception ex)
			{
				Debug.LogError("[LoadSurfaceData] JSON Parse Error: " + ex);
				return null;
			}

			Dictionary<string, MaterialInfo> materialInfos = new();
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

					foreach (var key in new[] { "footstep", "footstepStealth", "footstepProne" })
					{
						if (matObj.TryGetValue(key, out JToken token) && token is JObject obj)
						{
							info.footstepData[key] = obj.ToObject<FootstepData>();
						}
					}

					foreach (var key in new[] { "land", "land_pain", "land_death" })
					{
						if (matObj.TryGetValue(key, out JToken token) && token is JObject obj)
						{
							info.landingData[key] = obj.ToObject<LandingData>();
						}
					}

					if (matObj.TryGetValue("ammoTypes", out JToken ammoToken) && ammoToken is JObject ammoObj)
					{
						foreach (var ammoProp in ammoObj.Properties())
						{
							string ammoName = ammoProp.Name;
							if (ammoProp.Value is JObject ammoDataObj)
							{
								info.ammoData[ammoName] = ammoDataObj.ToObject<AmmoData>();
							}
						}
					}
					materialInfos[materialName] = info;
				}
			}
			Debug.Log($"[LoadSurfaceData] {materialInfos.Count} material definitions loaded.");

			return materialInfos;
		}


		public static double? TryGetDouble(JObject obj, string key)
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
	}

}



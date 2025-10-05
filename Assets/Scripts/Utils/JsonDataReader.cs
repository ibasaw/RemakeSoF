using UnityEngine;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

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
	}

	#region Root / Top-level
	public class SWeaponInfo
	{
		[JsonProperty("name")]
		public string Name { get; set; }

		// Json liefert oft Zahlen als "string" — Json.NET versucht die Konvertierung automatisch.
		[JsonProperty("foreshorten")]
		public float Foreshorten { get; set; }

		[JsonProperty("viewoffset")]
		public ViewOffset ViewOffset { get; set; }

		// sounds: beliebige Keys (fire, ready, ..) -> Unterobjekt mit sound1, sound2, ...
		[JsonProperty("sounds")]
		public Dictionary<string, Dictionary<string, string>> Sounds { get; set; }

		// forcefeedback wird auf SSurfaceCallback gemappt
		[JsonProperty("forcefeedback")]
		public SSurfaceCallback ForceFeedback { get; set; }

		// surfaces (optionale, in JSON vorhanden)
		[JsonProperty("surfaces")]
		public Dictionary<string, Dictionary<string, object>> Surfaces { get; set; }

		// weaponmodel -> SWeaponModel
		[JsonProperty("weaponmodel")]
		public SWeaponModel WeaponModel { get; set; }

		// anim -> Liste von SAnimWeapon Einträgen
		[JsonProperty("anim")]
		public List<SAnimWeapon> Anim { get; set; }

		// weitere Felder existieren (z.B. wpn) — said to ignore for now
	}
	#endregion

	#region ViewOffset
	public class ViewOffset
	{
		[JsonProperty("forward")]
		public float Forward { get; set; }

		[JsonProperty("right")]
		public float Right { get; set; }

		[JsonProperty("up")]
		public float Up { get; set; }
	}
	#endregion

	#region ForceFeedback / SSurfaceCallback
	// In JSON sind unter "forcefeedback" Einträge wie "fire": { "force1": "path" }
	// Wir bieten eine flexible Repräsentation: Dictionary<string, ForceFeedbackEntry>
	public class SSurfaceCallback
	{
		// Key = z.B. "fire", "ready", ... Value = z.B. {"force1": "fffx/.."}
		[JsonExtensionData]
		// JsonExtensionData liefert alle zusätzlichen Felder in einem Dictionary (Newtonsoft.Json.Linq.JToken).
		// Für einfachen Zugriff kannst du stattdessen die "Entries" Map füllen.
		public IDictionary<string, Newtonsoft.Json.Linq.JToken> ExtensionData { get; set; }

		// Hilfs-API (nicht zwingend) zum sicheren Auslesen als string map:
		public Dictionary<string, Dictionary<string, string>> GetEntries()
		{
			var result = new Dictionary<string, Dictionary<string, string>>();
			if (ExtensionData == null) return result;
			foreach (var kv in ExtensionData)
			{
				var map = new Dictionary<string, string>();
				if (kv.Value != null && kv.Value.Type == Newtonsoft.Json.Linq.JTokenType.Object)
				{
					foreach (var child in kv.Value.Children<Newtonsoft.Json.Linq.JProperty>())
					{
						map[child.Name] = child.Value.ToString();
					}
				}
				result[kv.Key] = map;
			}
			return result;
		}
	}
	#endregion

	#region WeaponModel (SWeaponModel) and nested
	public class SWeaponModel
	{
		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("model")]
		public string Model { get; set; }

		[JsonProperty("frames")]
		public string Frames { get; set; }

		[JsonProperty("buffer")]
		public WeaponBuffer Buffer { get; set; }

		[JsonProperty("hands")]
		public WeaponHands Hands { get; set; }

		[JsonProperty("bolton")]
		public WeaponBolton Bolton { get; set; }

		// variable side/front/left entries in JSON are usually keyed "rightside"/"leftside"/"front"
		// We map them to flexible dictionaries (surface1..surfaceN)
		[JsonProperty("rightside")]
		public Dictionary<string, string> RightSide { get; set; }

		[JsonProperty("leftside")]
		public Dictionary<string, string> LeftSide { get; set; }

		[JsonProperty("front")]
		public Dictionary<string, string> Front { get; set; }

		[JsonProperty("optionalpart")]
		public WeaponOptionalPart OptionalPart { get; set; }

		// weitere optionale Felder: buffer alt muzzle etc. können direkt ergänzt werden falls nötig
	}

	public class WeaponBuffer
	{
		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("model")]
		public string Model { get; set; }

		[JsonProperty("bolttobone")]
		public string BoltToBone { get; set; }

		[JsonProperty("muzzle")]
		public string Muzzle { get; set; }
	}

	public class WeaponHands
	{
		[JsonProperty("left")]
		public WeaponHand Left { get; set; }

		[JsonProperty("right")]
		public WeaponHand Right { get; set; }
	}

	public class WeaponHand
	{
		[JsonProperty("bolttobone")]
		public string BoltToBone { get; set; }
	}

	public class WeaponBolton
	{
		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("model")]
		public string Model { get; set; }

		[JsonProperty("frames")]
		public string Frames { get; set; }

		[JsonProperty("parent")]
		public string Parent { get; set; }

		[JsonProperty("bolttobone")]
		public string BoltToBone { get; set; }

		[JsonProperty("rightside")]
		public Dictionary<string, string> RightSide { get; set; }
	}

	public class WeaponOptionalPart
	{
		[JsonProperty("name")]
		public string Name { get; set; }

		// surfaces like surface1..surfaceN
		[JsonExtensionData]
		public IDictionary<string, Newtonsoft.Json.Linq.JToken> ExtraSurfaceData { get; set; }

		public Dictionary<string, string> GetSurfaceMap()
		{
			var outMap = new Dictionary<string, string>();
			if (ExtraSurfaceData == null) return outMap;
			foreach (var kv in ExtraSurfaceData)
				outMap[kv.Key] = kv.Value.ToString();
			return outMap;
		}
	}
	#endregion

	#region Anim (SAnimWeapon + SAnimInfoWeapon)
	public class SAnimWeapon
	{
		[JsonProperty("name")]
		public string Name { get; set; }

		// optional "muzzle"
		[JsonProperty("muzzle")]
		public string Muzzle { get; set; }

		// "info" ist die Liste der AnimInfoEntries
		[JsonProperty("info")]
		public List<SAnimInfoWeapon> Info { get; set; }
	}

	public class SAnimInfoWeapon
	{
		// z.B. "type": "weaponmodel" | "hands" | "bolton"
		[JsonProperty("type")]
		public string Type { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }

		// verschiedene mögliche anim-felder in der JSON (anim, anim1..anim4, animNoLerp1..)
		[JsonProperty("anim")]
		public string Anim { get; set; }

		[JsonProperty("anim1")]
		public string Anim1 { get; set; }
		[JsonProperty("anim2")]
		public string Anim2 { get; set; }
		[JsonProperty("anim3")]
		public string Anim3 { get; set; }
		[JsonProperty("anim4")]
		public string Anim4 { get; set; }

		[JsonProperty("animNoLerp1")]
		public string AnimNoLerp1 { get; set; }
		[JsonProperty("animNoLerp2")]
		public string AnimNoLerp2 { get; set; }
		[JsonProperty("animNoLerp3")]
		public string AnimNoLerp3 { get; set; }
		[JsonProperty("animNoLerp4")]
		public string AnimNoLerp4 { get; set; }

		// optional extras
		[JsonProperty("extra1")]
		public string Extra1 { get; set; }
		[JsonProperty("extra2")]
		public string Extra2 { get; set; }
		[JsonProperty("extra3")]
		public string Extra3 { get; set; }

		// speed kann als number oder string in der JSON stehen -> float
		[JsonProperty("speed")]
		public float? Speed { get; set; }

		[JsonProperty("lodbias")]
		public int? LODBias { get; set; }

		[JsonProperty("mp_speed")]
		public float? MultiplayerSpeed { get; set; }

		// beliebige zusätzliche Eigenschaften (z.B. startframe etc.) fangen wir flexibel ab
		[JsonExtensionData]
		public IDictionary<string, Newtonsoft.Json.Linq.JToken> Extra { get; set; }
	}
	#endregion

	#region Beispiel-Deserializer (klein)
	public static class SoF2WeaponLoader
	{
		public static List<SWeaponInfo> LoadFromJson(string json)
		{
			// Einfach: JsonConvert.DeserializeObject<List<SWeaponInfo>>(json)
			// Da wir viele optionale / variierende Felder haben, verwendet das Modell ExtensionData-Patterns
			return JsonConvert.DeserializeObject<List<SWeaponInfo>>(json);
		}
	}
	#endregion

}



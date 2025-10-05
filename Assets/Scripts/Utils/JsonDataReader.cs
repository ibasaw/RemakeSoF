using UnityEngine;
using System.IO;

namespace Utils
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
}



using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Tolik.RemakeSoF.Runtime.WeaponManagement;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.DataManagement
{
    /// <summary>
    /// Pure Service für das Laden und Parsen von Waffendefinitionen aus SoF2_Weapons_New.json.
    /// Stellt statische Referenzdaten bereit — das NetworkList-Inventory bleibt bei String-IDs.
    /// Zugreifbar über ServiceLocator.
    /// </summary>
    public class WeaponDataLoader
    {
        private const string DATA_PATH = "Data/SoF2_Weapons_New.json";

        private readonly Dictionary<string, WeaponDefinition> m_WeaponsById = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<int, WeaponDefinition> m_WeaponsByAnimatorIndex = new();

        /// <summary>
        /// Initialisiert den Loader und lädt alle Waffen automatisch.
        /// </summary>
        public WeaponDataLoader()
        {
            LoadFromResources();
        }

        /// <summary>
        /// Lädt alle Waffendefinitionen aus der JSON-Datei in Resources.
        /// </summary>
        private void LoadFromResources()
        {
            m_WeaponsById.Clear();
            m_WeaponsByAnimatorIndex.Clear();

            string filePath = Path.Combine(Application.streamingAssetsPath, DATA_PATH);

            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[WeaponDataLoader] Could not load weapons file at {filePath}");
                return;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                List<WeaponDefinition> weapons = JsonConvert.DeserializeObject<List<WeaponDefinition>>(json);

                if (weapons == null)
                {
                    Debug.LogWarning("[WeaponDataLoader] Weapons array is null or empty");
                    return;
                }

                foreach (WeaponDefinition weapon in weapons)
                {
                    if (string.IsNullOrEmpty(weapon.Id))
                    {
                        continue;
                    }

                    m_WeaponsById[weapon.Id] = weapon;
                    m_WeaponsByAnimatorIndex[weapon.AnimatorIndex] = weapon;
                }

                Debug.Log($"[WeaponDataLoader] Loaded {m_WeaponsById.Count} weapons from {filePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WeaponDataLoader] Error parsing weapons file: {ex.Message}");
            }
        }

        /// <summary>
        /// Gibt die Waffendefinition anhand der ID zurück (z.B. "knife", "m4").
        /// </summary>
        public WeaponDefinition GetById(string weaponId)
        {
            m_WeaponsById.TryGetValue(weaponId, out WeaponDefinition weapon);
            return weapon;
        }

        /// <summary>
        /// Gibt die Waffendefinition anhand des Animator-Index zurück.
        /// </summary>
        public WeaponDefinition GetByAnimatorIndex(int animatorIndex)
        {
            m_WeaponsByAnimatorIndex.TryGetValue(animatorIndex, out WeaponDefinition weapon);
            return weapon;
        }

        /// <summary>
        /// Prüft ob eine Waffe mit der angegebenen ID existiert.
        /// </summary>
        public bool HasWeapon(string weaponId)
        {
            return m_WeaponsById.ContainsKey(weaponId);
        }

        /// <summary>
        /// Gibt den Animator-Index für eine Waffen-ID zurück. Gibt 0 zurück wenn unbekannt.
        /// </summary>
        public int GetAnimatorIndex(string weaponId)
        {
            if (m_WeaponsById.TryGetValue(weaponId, out WeaponDefinition weapon))
            {
                return weapon.AnimatorIndex;
            }

            return 0;
        }

        /// <summary>
        /// Gibt alle geladenen Waffen-IDs zurück.
        /// </summary>
        public IEnumerable<string> GetAllWeaponIds()
        {
            return m_WeaponsById.Keys;
        }

        /// <summary>
        /// Gibt alle geladenen Waffendefinitionen zurück.
        /// </summary>
        public IEnumerable<WeaponDefinition> GetAllWeapons()
        {
            return m_WeaponsById.Values;
        }

        /// <summary>
        /// Gibt die Anzahl geladener Waffen zurück.
        /// </summary>
        public int WeaponCount => m_WeaponsById.Count;

        /// <summary>
        /// Leert den Cache. Wird von ServiceLocator.ClearAll() aufgerufen.
        /// </summary>
        public void ClearCache()
        {
            m_WeaponsById.Clear();
            m_WeaponsByAnimatorIndex.Clear();
            Debug.Log("[WeaponDataLoader] Cache cleared.");
        }
    }
}

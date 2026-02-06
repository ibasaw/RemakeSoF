using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Tolik.RemakeSoF.Runtime.PlayerSkinManagement;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.DataManagement
{
    /// <summary>
    /// Pure Service für das Laden und Parsen von Item-Definitionen aus SoF2_Items.json.
    /// Zugreifbar über ServiceLocator.
    /// </summary>
    public class ItemDefinitionLoader
    {
        private readonly Dictionary<string, Item> m_ItemsByName = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initialisiert den Loader und lädt alle Items automatisch
        /// </summary>
        public ItemDefinitionLoader()
        {
            LoadFromResources();
        }

        /// <summary>
        /// Loads all available items from SoF2_Items.json (weapons are ignored)
        /// </summary>
        private void LoadFromResources(string resourcePath = "Data/SoF2_Items")
        {
            m_ItemsByName.Clear();

            TextAsset itemsFile = Resources.Load<TextAsset>(resourcePath);

            if (itemsFile == null)
            {
                Debug.LogWarning($"[ItemDefinitionLoader] Could not load items file at {resourcePath}");
                return;
            }

            try
            {
                ItemsContainer container = JsonConvert.DeserializeObject<ItemsContainer>(itemsFile.text);

                if (container?.items == null)
                {
                    Debug.LogWarning("[ItemDefinitionLoader] Items array is null or empty");
                    return;
                }

                foreach (Item item in container.items)
                {
                    if (!string.IsNullOrEmpty(item.name))
                    {
                        m_ItemsByName[item.name] = item;
                    }
                }

                Debug.Log($"[ItemDefinitionLoader] Loaded {m_ItemsByName.Count} items from {resourcePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ItemDefinitionLoader] Error parsing items file: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets an item by name
        /// </summary>
        public Item GetByName(string itemName)
        {
            m_ItemsByName.TryGetValue(itemName, out Item item);
            return item;
        }

        /// <summary>
        /// Checks if an item exists
        /// </summary>
        public bool HasItem(string itemName)
        {
            return m_ItemsByName.ContainsKey(itemName);
        }

        /// <summary>
        /// Gets all item names
        /// </summary>
        public string[] GetAllItemNames()
        {
            string[] names = new string[m_ItemsByName.Count];
            m_ItemsByName.Keys.CopyTo(names, 0);
            return names;
        }

        /// <summary>
        /// Gets all items
        /// </summary>
        public List<Item> GetAllItems()
        {
            return new List<Item>(m_ItemsByName.Values);
        }

        /// <summary>
        /// Total number of loaded items
        /// </summary>
        public int TotalItemCount => m_ItemsByName.Count;

        /// <summary>
        /// Clears all loaded items
        /// </summary>
        public void ClearCache()
        {
            m_ItemsByName.Clear();
            Debug.Log("[ItemDefinitionLoader] Cache cleared.");
        }

        /// <summary>
        /// Container class for deserializing the SoF2_Items.json structure
        /// </summary>
        [Serializable]
        private class ItemsContainer
        {
            [JsonProperty("items")]
            public List<Item> items;
        }
    }
}

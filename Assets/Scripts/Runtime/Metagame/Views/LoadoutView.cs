using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.Core;
using Tolik.RemakeSoF.Runtime.DataManagement;
using Tolik.RemakeSoF.Runtime.TextureManagement;
namespace Tolik.RemakeSoF.Runtime
{
    [RequireComponent(typeof(UIDocument))]
    internal class LoadoutView : View<MetagameApplication>
    {
        UIDocument m_UIDocument;

        Label m_PlayerNameLabel;
        Label m_PlayerIdLabel;
        Label m_SkinNameLabel;
        Label m_SkinRarityLabel;
        Label m_SkinDescriptionLabel;

        VisualElement m_LoadoutRoot;
        VisualElement m_CharacterPreviewContainer;
        GameObject m_CharacterPrefab;
        RenderTexture m_CharacterPreviewRenderTexture;
        Camera m_CharacterPreviewCamera;
        GameObject m_CharacterPreviewStage;
        GameObject m_CharacterPreviewInstance;

        Button m_LoadPreviousSkinButton;
        Button m_LoadNextSkinButton;
        Button m_EquipButton;
        TextField m_DisplayNameInput;
        Label m_PlayerNameOverlay;

        HorizontalScrollView m_SkinListScroll;
        Dictionary<string, VisualElement> m_SkinThumbnails = new();
        Dictionary<string, Texture2D> m_LoadedIconTextures = new();
        string m_SelectedSkinName;

        readonly int m_PreviewLayer = 30;
        Vector3 m_CameraOffset = new(6f, 0.3f, 0);
        readonly float m_CameraFov = 34f;
        Color m_ClearColor = new(0, 0, 0, 0);
        Vector3 m_CharacterRotation = new(0, 90, 0);
        Vector3 m_CharacterPosition = new(0, -0.95f, 0);

        void Awake()
        {
            m_UIDocument = GetComponent<UIDocument>();
        }

        void OnEnable()
        {
            VisualElement root = m_UIDocument.rootVisualElement;

            m_LoadoutRoot = root.Q<VisualElement>("loadoutRoot");
            m_PlayerNameLabel = root.Q<Label>("playerName");
            m_PlayerIdLabel = root.Q<Label>("playerId");
            m_SkinNameLabel = root.Q<Label>("skinName");
            m_SkinRarityLabel = root.Q<Label>("skinRarity");
            m_SkinDescriptionLabel = root.Q<Label>("skinDescription");
            m_LoadPreviousSkinButton = root.Q<Button>("loadPrevious");
            m_LoadNextSkinButton = root.Q<Button>("loadNext");
            m_EquipButton = root.Q<Button>("equipButton");
            m_CharacterPreviewContainer = root.Q<VisualElement>("previewArea");
            m_CharacterPreviewContainer.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            m_PlayerNameOverlay = root.Q<Label>("playerNameOverlay");
            m_DisplayNameInput = root.Q<TextField>("displayNameInput");

            m_PlayerNameLabel.text = App.Model.PlayerData.PlayerName;
            m_PlayerIdLabel.text = App.Model.PlayerData.PlayerId;
            if (m_PlayerNameOverlay != null)
            {
                m_PlayerNameOverlay.text = App.Model.PlayerData.PlayerName;
            }

            if (m_DisplayNameInput != null)
            {
                m_DisplayNameInput.RegisterValueChangedCallback(OnDisplayNameChanged);
            }

            m_SkinListScroll = root.Q<HorizontalScrollView>("skinListScroll");
            PopulateSkinList();

            LoadAndApplyTextures(root);

            string initialSkinName = App.Model.PlayerData.CurrentSelectedSkinName;
            if (!string.IsNullOrEmpty(initialSkinName))
            {
                UpdateSkinInfo(initialSkinName);
            }

            m_LoadNextSkinButton.RegisterCallback<ClickEvent>(OnClickLoadNextSkin);
            m_LoadPreviousSkinButton.RegisterCallback<ClickEvent>(OnClickLoadPreviousSkin);
            m_LoadNextSkinButton.RegisterCallback<PointerEnterEvent>(_ => UIMenuSoundPlayer.Play(UIMenuSoundPlayer.Hilite));
            m_LoadPreviousSkinButton.RegisterCallback<PointerEnterEvent>(_ => UIMenuSoundPlayer.Play(UIMenuSoundPlayer.Hilite));

            CreateStage();
            UpdateRenderTexture();
        }

        void OnClickLoadNextSkin(ClickEvent evt)
        {
            UIMenuSoundPlayer.Play(UIMenuSoundPlayer.Click);
            Debug.Log("Load Next Skin clicked");
            Broadcast(new LoadNextSkinEvent());
        }
        void OnClickLoadPreviousSkin(ClickEvent evt)
        {
            UIMenuSoundPlayer.Play(UIMenuSoundPlayer.Click);
            Debug.Log("Load Previous Skin clicked");
            Broadcast(new LoadPreviousSkinEvent());
        }

        /// <summary>
        /// Updates the player name overlay when the display name input changes.
        /// Falls back to the player name if the input is empty.
        /// </summary>
        void OnDisplayNameChanged(ChangeEvent<string> evt)
        {
            if (m_PlayerNameOverlay == null) return;

            string displayName = string.IsNullOrWhiteSpace(evt.newValue)
                ? App.Model.PlayerData.PlayerName
                : evt.newValue.Trim();

            m_PlayerNameOverlay.text = displayName;
        }

        public void SetCharacterPrefab(GameObject prefab)
        {
            m_CharacterPrefab = prefab;
            if (m_CharacterPreviewStage != null)
            {
                RefreshCharacterPreview();
            }
        }

        /// <summary>
        /// Updates the skin info labels displayed in the info panel.
        /// </summary>
        public void UpdateSkinInfo(string skinName)
        {
            if (m_SkinNameLabel != null)
            {
                m_SkinNameLabel.text = skinName;
            }

            UpdateSkinListSelection(skinName);
        }

        /// <summary>
        /// Loads textures via TextureManager/TextureConfiguration and applies them to icon tabs and scrollbar elements.
        /// </summary>
        void LoadAndApplyTextures(VisualElement root)
        {
            TextureManager textureManager = ServiceLocator.Get<TextureManager>();
            TextureConfiguration.MetagameConfiguration.LoadoutTextures config = textureManager.Configuration?.metagame?.loadout;
            if (config == null) return;

            ApplyTexture(textureManager, root.Q<Button>("tabSkin"), config.iconPlayer);
            ApplyTexture(textureManager, root.Q<Button>("tabGun"), config.iconScale);
            ApplyTexture(textureManager, root.Q<Button>("tabGear"), config.iconMisc);
            ApplyTexture(textureManager, root.Q<Button>("tabEmote"), config.iconBack);
            ApplyTexture(textureManager, root.Q<Button>("tabKeys"), config.iconKeys);
            ApplyTexture(textureManager, root.Q<Button>("tabDisplay"), config.iconDisplay);
            ApplyTexture(textureManager, root.Q<Button>("tabSound"), config.iconSound);
            ApplyTexture(textureManager, root.Q<Button>("tabNetwork"), config.iconNetwork);

            // Hover sounds for icon tabs
            string[] tabNames = { "tabSkin", "tabGun", "tabGear", "tabEmote", "tabKeys", "tabDisplay", "tabSound", "tabNetwork" };
            foreach (string tabName in tabNames)
            {
                Button tab = root.Q<Button>(tabName);
                if (tab != null)
                {
                    tab.RegisterCallback<PointerEnterEvent>(_ => UIMenuSoundPlayer.Play(UIMenuSoundPlayer.Hilite));
                }
            }

            if (m_SkinListScroll != null)
            {
                ApplyTexture(textureManager, m_SkinListScroll.ArrowLeft, config.scrollbarArrowLeft);
                ApplyTexture(textureManager, m_SkinListScroll.ArrowRight, config.scrollbarArrowRight);
                ApplyTexture(textureManager, m_SkinListScroll.Track, config.scrollbarTrack);
                ApplyTexture(textureManager, m_SkinListScroll.Thumb, config.scrollbarThumb);

                m_SkinListScroll.ArrowLeft.RegisterCallback<PointerEnterEvent>(_ => UIMenuSoundPlayer.Play(UIMenuSoundPlayer.Hilite));
                m_SkinListScroll.ArrowRight.RegisterCallback<PointerEnterEvent>(_ => UIMenuSoundPlayer.Play(UIMenuSoundPlayer.Hilite));
                m_SkinListScroll.ArrowLeft.RegisterCallback<ClickEvent>(_ => UIMenuSoundPlayer.Play(UIMenuSoundPlayer.Click));
                m_SkinListScroll.ArrowRight.RegisterCallback<ClickEvent>(_ => UIMenuSoundPlayer.Play(UIMenuSoundPlayer.Click));
            }
        }

        /// <summary>
        /// Applies a texture from the TextureManager to the background-image of a VisualElement.
        /// </summary>
        void ApplyTexture(TextureManager textureManager, VisualElement element, string textureKey)
        {
            if (element == null || string.IsNullOrEmpty(textureKey)) return;

            TextureData textureData = textureManager.GetTextureData(textureKey);
            if (textureData?.Texture != null)
            {
                element.style.backgroundImage = new StyleBackground(textureData.Texture);
            }
        }

        /// <summary>
        /// Populates the horizontal skin thumbnail list with player icons from disk.
        /// </summary>
        void PopulateSkinList()
        {
            if (m_SkinListScroll == null) return;

            m_SkinListScroll.Clear();
            m_SkinThumbnails.Clear();

            SkinDefinitionLoader skinLoader = ServiceLocator.Get<SkinDefinitionLoader>();
            List<string> allSkinNames = skinLoader.GetAllSkinNames();

            string iconDir = Path.Combine(Application.dataPath, "Art", "Textures", "gfx", "playericons");
            Dictionary<string, string> skinToIconPath = BuildSkinIconMap(iconDir);

            foreach (string skinName in allSkinNames)
            {
                VisualElement thumbnail = new();
                thumbnail.AddToClassList("loadout-skin-thumbnail");

                if (skinToIconPath.TryGetValue(skinName, out string iconPath))
                {
                    Texture2D tex = LoadIconTexture(iconPath);
                    if (tex != null)
                    {
                        thumbnail.style.backgroundImage = new StyleBackground(tex);
                    }
                }

                string capturedName = skinName;
                thumbnail.RegisterCallback<ClickEvent>(_ => OnSkinThumbnailClicked(capturedName));
                thumbnail.RegisterCallback<PointerEnterEvent>(_ => UIMenuSoundPlayer.Play(UIMenuSoundPlayer.Hilite));

                m_SkinThumbnails[skinName] = thumbnail;
                m_SkinListScroll.Add(thumbnail);
            }

            string currentSkin = App.Model.PlayerData.CurrentSelectedSkinName;
            if (!string.IsNullOrEmpty(currentSkin))
            {
                UpdateSkinListSelection(currentSkin);
            }
        }

        /// <summary>
        /// Builds a mapping from skin_name to icon file path by parsing filenames.
        /// Icon files have format: "NPC_DisplayName ( skin_name ).jpg"
        /// </summary>
        Dictionary<string, string> BuildSkinIconMap(string iconDir)
        {
            Dictionary<string, string> map = new(System.StringComparer.OrdinalIgnoreCase);

            if (!Directory.Exists(iconDir)) return map;

            string[] files = Directory.GetFiles(iconDir, "*.jpg");
            Regex regex = new(@"\(\s*(.+?)\s*\)");

            foreach (string file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                Match match = regex.Match(fileName);
                if (match.Success)
                {
                    string skinKey = match.Groups[1].Value;
                    map[skinKey] = file;
                }
            }

            return map;
        }

        /// <summary>
        /// Loads a texture from disk for use as a thumbnail icon.
        /// </summary>
        Texture2D LoadIconTexture(string filePath)
        {
            if (m_LoadedIconTextures.TryGetValue(filePath, out Texture2D cached))
            {
                return cached;
            }

            if (!File.Exists(filePath)) return null;

            byte[] data = File.ReadAllBytes(filePath);
            Texture2D tex = new(2, 2, TextureFormat.RGB24, false);
            if (tex.LoadImage(data))
            {
                tex.name = Path.GetFileNameWithoutExtension(filePath);
                m_LoadedIconTextures[filePath] = tex;
                return tex;
            }

            Object.Destroy(tex);
            return null;
        }

        /// <summary>
        /// Handles a click on a skin thumbnail, broadcasting a ChangeSkinByNameEvent.
        /// </summary>
        void OnSkinThumbnailClicked(string skinName)
        {
            UIMenuSoundPlayer.Play(UIMenuSoundPlayer.Select);
            Debug.Log($"[LoadoutView] Skin thumbnail clicked: {skinName}");
            Broadcast(new ChangeSkinByNameEvent { skinName = skinName });
        }

        /// <summary>
        /// Updates the visual selection state in the skin thumbnail list.
        /// </summary>
        void UpdateSkinListSelection(string skinName)
        {
            if (m_SkinThumbnails == null || m_SkinThumbnails.Count == 0) return;

            if (!string.IsNullOrEmpty(m_SelectedSkinName) && m_SkinThumbnails.TryGetValue(m_SelectedSkinName, out VisualElement oldThumb))
            {
                oldThumb.RemoveFromClassList("loadout-skin-thumbnail-selected");
            }

            m_SelectedSkinName = skinName;

            if (m_SkinThumbnails.TryGetValue(skinName, out VisualElement newThumb))
            {
                newThumb.AddToClassList("loadout-skin-thumbnail-selected");
                m_SkinListScroll?.ScrollTo(newThumb);
            }
        }

        void CreateStage()
        {
            m_CharacterPreviewStage = new GameObject("PreviewStage") { hideFlags = HideFlags.HideAndDontSave };

            m_CharacterPreviewCamera = new GameObject("PreviewCamera").AddComponent<Camera>();
            m_CharacterPreviewCamera.transform.SetParent(m_CharacterPreviewStage.transform, false);
            m_CharacterPreviewCamera.transform.position = m_CameraOffset;
            m_CharacterPreviewCamera.transform.LookAt(Vector3.zero);
            m_CharacterPreviewCamera.clearFlags = CameraClearFlags.SolidColor;
            m_CharacterPreviewCamera.backgroundColor = m_ClearColor;
            m_CharacterPreviewCamera.cullingMask = 1 << m_PreviewLayer;
            m_CharacterPreviewCamera.fieldOfView = m_CameraFov;
            m_CharacterPreviewCamera.nearClipPlane = 0.1f;
            m_CharacterPreviewCamera.farClipPlane = 20f;

            // PreviewLight deaktiviert — kann globale Lichteinstellungen stoeren.
            var light = new GameObject("PreviewLight").AddComponent<Light>();
            light.transform.SetParent(m_CharacterPreviewStage.transform, false);
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
            light.intensity = 1.2f;
            light.cullingMask = 1 << m_PreviewLayer;
            RefreshCharacterPreview();
        }

        void RefreshCharacterPreview()
        {
            if (m_CharacterPreviewStage == null) return;

            if (m_CharacterPreviewInstance != null)
            {
                Destroy(m_CharacterPreviewInstance);
                m_CharacterPreviewInstance = null;
            }

            if (m_CharacterPrefab == null) return;

            m_CharacterPreviewInstance = Instantiate(m_CharacterPrefab, m_CharacterPreviewStage.transform);
            m_CharacterPreviewInstance.transform.SetPositionAndRotation(m_CharacterPosition, Quaternion.Euler(m_CharacterRotation));
            SetLayerRecursively(m_CharacterPreviewInstance, m_PreviewLayer);
        }

        void LateUpdate()
        {
            // Force render every frame
            if (m_CharacterPreviewCamera != null && m_CharacterPreviewRenderTexture != null)
            {
                m_CharacterPreviewCamera.Render();
            }
        }

        void OnGeometryChanged(GeometryChangedEvent _) => UpdateRenderTexture();

        void UpdateRenderTexture()
        {
            if (m_CharacterPreviewContainer == null || m_CharacterPreviewCamera == null) return;

            Vector2 size = m_CharacterPreviewContainer.contentRect.size;
            int w = Mathf.Max(1, Mathf.RoundToInt(size.x));
            int h = Mathf.Max(1, Mathf.RoundToInt(size.y));

            if (m_CharacterPreviewRenderTexture != null && (m_CharacterPreviewRenderTexture.width != w || m_CharacterPreviewRenderTexture.height != h))
            {
                m_CharacterPreviewRenderTexture.Release();
                Destroy(m_CharacterPreviewRenderTexture);
                m_CharacterPreviewRenderTexture = null;
            }

            if (m_CharacterPreviewRenderTexture == null)
            {
                m_CharacterPreviewRenderTexture = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32)
                {
                    name = "MainMenuCharacterPreviewRT"
                };
            }

            m_CharacterPreviewCamera.targetTexture = m_CharacterPreviewRenderTexture;
            m_CharacterPreviewContainer.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(m_CharacterPreviewRenderTexture));

            // Force immediate render after RT change
            m_CharacterPreviewCamera.Render();
        }

        void OnDisable()
        {
            m_CharacterPreviewContainer?.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            if (m_CharacterPreviewRenderTexture != null) { m_CharacterPreviewRenderTexture.Release(); Destroy(m_CharacterPreviewRenderTexture); }
            if (m_CharacterPreviewStage != null) Destroy(m_CharacterPreviewStage);
            m_LoadNextSkinButton?.UnregisterCallback<ClickEvent>(OnClickLoadNextSkin);
            m_LoadPreviousSkinButton?.UnregisterCallback<ClickEvent>(OnClickLoadPreviousSkin);
            m_DisplayNameInput?.UnregisterValueChangedCallback(OnDisplayNameChanged);

            foreach (Texture2D tex in m_LoadedIconTextures.Values)
            {
                if (tex != null) Destroy(tex);
            }
            m_LoadedIconTextures.Clear();
            m_SkinThumbnails.Clear();
        }

        void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
                SetLayerRecursively(child.gameObject, layer);
        }
    }
}
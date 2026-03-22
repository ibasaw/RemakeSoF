using UnityEngine;
using UnityEngine.UIElements;
namespace Tolik.RemakeSoF.Runtime
{
    [RequireComponent(typeof(UIDocument))]
    internal class LoadoutView : View<MetagameApplication>
    {
        UIDocument m_UIDocument;

        Label m_PlayerNameLabel;

        Label m_PlayerIdLabel;

        VisualElement m_CharacterPreviewContainer;
        GameObject m_CharacterPrefab;
        RenderTexture m_CharacterPreviewRenderTexture;
        Camera m_CharacterPreviewCamera;
        GameObject m_CharacterPreviewStage;
        GameObject m_CharacterPreviewInstance;

        Button m_LoadPreviousSkinButton;
        Button m_LoadNextSkinButton;

        readonly int m_PreviewLayer = 30;
        Vector3 m_CameraOffset = new(15f, 0, 0);
        readonly float m_CameraFov = 40f;
        Color m_ClearColor = new(0, 0, 0, 0);
        Vector3 m_CharacterRotation = new(0, 90, 0); // Charakter-Rotation in Grad
        Vector3 m_CharacterPosition = new(0, -5f, 0); // Charakter-Position (Y nach unten)

        void Awake()
        {
            m_UIDocument = GetComponent<UIDocument>();
        }

        void OnEnable()
        {
            var root = m_UIDocument.rootVisualElement;

            m_PlayerNameLabel = root.Q<Label>("playerName");
            m_PlayerIdLabel = root.Q<Label>("playerId");
            m_LoadPreviousSkinButton = root.Q<Button>("loadPrevious");
            m_LoadNextSkinButton = root.Q<Button>("loadNext");
            m_CharacterPreviewContainer = root.Q<VisualElement>("previewArea");
            m_CharacterPreviewContainer.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            m_PlayerNameLabel.text = App.Model.PlayerData.PlayerName;
            m_PlayerIdLabel.text = App.Model.PlayerData.PlayerId;

            m_LoadNextSkinButton.RegisterCallback<ClickEvent>(OnClickLoadNextSkin);
            m_LoadPreviousSkinButton.RegisterCallback<ClickEvent>(OnClickLoadPreviousSkin);

            CreateStage();
            UpdateRenderTexture();
        }

        void OnClickLoadNextSkin(ClickEvent evt)
        {
            Debug.Log("Load Next Skin clicked");
            Broadcast(new LoadNextSkinEvent());
        }
        void OnClickLoadPreviousSkin(ClickEvent evt)
        {
            Debug.Log("Load Previous Skin clicked");
            Broadcast(new LoadPreviousSkinEvent());
        }

        public void SetCharacterPrefab(GameObject prefab)
        {
            m_CharacterPrefab = prefab;
            if (m_CharacterPreviewStage != null)
            {
                RefreshCharacterPreview();
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
            // var light = new GameObject("PreviewLight").AddComponent<Light>();
            // light.transform.SetParent(m_CharacterPreviewStage.transform, false);
            // light.type = LightType.Directional;
            // light.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
            // light.intensity = 1.2f;
            // light.cullingMask = 1 << m_PreviewLayer;
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
            m_LoadNextSkinButton.UnregisterCallback<ClickEvent>(OnClickLoadNextSkin);
            m_LoadPreviousSkinButton.UnregisterCallback<ClickEvent>(OnClickLoadPreviousSkin);
        }

        void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
                SetLayerRecursively(child.gameObject, layer);
        }
    }
}
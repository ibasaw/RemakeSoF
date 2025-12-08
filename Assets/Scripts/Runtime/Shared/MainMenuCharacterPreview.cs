using UnityEngine;
using UnityEngine.UIElements;

namespace Tolik.RemakeSoF.Runtime
{
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuCharacterPreview : MonoBehaviour
    {
        [SerializeField] GameObject characterPrefab;
        [SerializeField] int previewLayer = 30;
        [SerializeField] Vector3 cameraOffset = new(0, 1.6f, 3.2f);
        [SerializeField] float cameraFov = 25f;
        [SerializeField] Color clearColor = new(0, 0, 0, 0);
        [SerializeField] Vector3 nameOffset = new(0, 2.2f, 0); // Über dem Kopf
        [SerializeField] Vector3 characterRotation = new(0, 180, 0); // Charakter-Rotation in Grad
        [SerializeField] Vector3 characterPosition = new(0, -1f, 0); // Charakter-Position (Y nach unten)

        VisualElement _target;
        RenderTexture _rt;
        Camera _cam;
        GameObject _stage;
        GameObject _instance;
        MetagameApplication _app;

        void OnEnable()
        {
            _app = FindFirstObjectByType<MetagameApplication>();
            
            var root = GetComponent<UIDocument>().rootVisualElement;
            _target = root.Q<VisualElement>("previewArea");
            _target?.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            CreateStage();
            UpdateRenderTexture();
        }

        void CreateStage()
        {
            _stage = new GameObject("PreviewStage") { hideFlags = HideFlags.HideAndDontSave };

            _cam = new GameObject("PreviewCamera").AddComponent<Camera>();
            _cam.transform.SetParent(_stage.transform, false);
            _cam.transform.position = cameraOffset;
            _cam.transform.LookAt(Vector3.zero);
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = clearColor;
            _cam.cullingMask = 1 << previewLayer;
            _cam.fieldOfView = cameraFov;
            _cam.nearClipPlane = 0.1f;
            _cam.farClipPlane = 20f;

            var light = new GameObject("PreviewLight").AddComponent<Light>();
            light.transform.SetParent(_stage.transform, false);
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
            light.intensity = 1.2f;
            light.cullingMask = 1 << previewLayer;

            if (characterPrefab != null)
            {
                _instance = Instantiate(characterPrefab, _stage.transform);
                _instance.transform.position = characterPosition;
                _instance.transform.rotation = Quaternion.Euler(characterRotation);
                SetLayerRecursively(_instance, previewLayer);
            }
        }

        void LateUpdate()
        {
            // Force render every frame
            if (_cam != null && _rt != null)
            {
                _cam.Render();
            }
        }

        void OnGeometryChanged(GeometryChangedEvent _) => UpdateRenderTexture();

        void UpdateRenderTexture()
        {
            if (_target == null || _cam == null) return;

            Vector2 size = _target.contentRect.size;
            int w = Mathf.Max(1, Mathf.RoundToInt(size.x));
            int h = Mathf.Max(1, Mathf.RoundToInt(size.y));

            if (_rt != null && (_rt.width != w || _rt.height != h))
            {
                _rt.Release();
                Destroy(_rt);
                _rt = null;
            }

            if (_rt == null)
            {
                _rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32)
                {
                    name = "MainMenuCharacterPreviewRT"
                };
            }

            _cam.targetTexture = _rt;
            _target.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(_rt));
            
            // Force immediate render after RT change
            _cam.Render();
        }

        void OnDisable()
        {
            _target?.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            if (_rt != null) { _rt.Release(); Destroy(_rt); }
            if (_stage != null) Destroy(_stage);
        }

        void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
                SetLayerRecursively(child.gameObject, layer);
        }
    }
}
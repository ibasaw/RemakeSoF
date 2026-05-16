using System.Collections.Generic;
using System.Reflection;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.TextureManagement;
using UnityEngine;
using UnityEngine.UIElements;
namespace Tolik.RemakeSoF.Runtime
{
    [UnityEngine.RequireComponent(typeof(UIDocument))]

    internal class MainMenuView : View<MetagameApplication>
    {
        private VisualElement m_ContentBackground;
        private VisualElement m_MainMenuBackground;
        UIDocument m_UIDocument;

        // Ein Container für alle Button-Infos
        class ButtonConfig
        {
            public string Name;
            public Texture2D HoverIconPath;
            public System.Action<ButtonConfig> OnClick;
            public View<MetagameApplication> TargetView;
            public Button ButtonRef;
            public Texture2D ActiveIconPath => HoverIconPath;
            public bool IsActive;
        }
        readonly List<ButtonConfig> m_ButtonConfigs = new();

        void Awake()
        {
            m_UIDocument = GetComponent<UIDocument>();

            TextureConfiguration configuration = ServiceLocator.Get<TextureManager>().Configuration;

            m_ButtonConfigs.Add(new ButtonConfig
            {
                Name = "joinServerButton",
                HoverIconPath = ServiceLocator.Get<TextureManager>().GetTextureData(configuration.metagame.mainMenu.joinServerButtonGlow)?.Texture,
                TargetView = App.View.JoinServerView
            });
            m_ButtonConfigs.Add(new ButtonConfig
            {
                Name = "createServerButton",
                HoverIconPath = ServiceLocator.Get<TextureManager>().GetTextureData(configuration.metagame.mainMenu.createServerButtonGlow)?.Texture,
                TargetView = App.View.CreateServerView
            });
            m_ButtonConfigs.Add(new ButtonConfig
            {
                Name = "optionsButton",
                HoverIconPath = ServiceLocator.Get<TextureManager>().GetTextureData(configuration.metagame.mainMenu.optionsButtonGlow)?.Texture,
                TargetView = App.View.OptionsView
            });
            m_ButtonConfigs.Add(new ButtonConfig
            {
                Name = "loadoutButton",
                HoverIconPath = ServiceLocator.Get<TextureManager>().GetTextureData(configuration.metagame.mainMenu.loadoutButtonGlow)?.Texture,
                TargetView = App.View.LoadoutView
            });
            m_ButtonConfigs.Add(new ButtonConfig
            {
                Name = "logoutButton",
                HoverIconPath = ServiceLocator.Get<TextureManager>().GetTextureData(configuration.metagame.mainMenu.logoutButtonGlow)?.Texture,
                OnClick = (cfg) =>
                {
                    ApplicationEntryPoint.Singleton.AuthenticationManager.Logout();
                }
            });
        }

        void OnEnable()
        {
            var root = m_UIDocument.rootVisualElement;

            m_ContentBackground = root.Q<VisualElement>("contentBackground");
            m_MainMenuBackground = root.Q<VisualElement>("mainMenu");
            TextureConfiguration configuration = ServiceLocator.Get<TextureManager>().Configuration;

            Texture2D mainMenuBackgroundTexture = ServiceLocator.Get<TextureManager>().GetTextureData(configuration.metagame.mainMenu.background)?.Texture;
            m_MainMenuBackground.style.backgroundImage = new StyleBackground(mainMenuBackgroundTexture);
            
            // Buttons + callbacks
            foreach (ButtonConfig cfg in m_ButtonConfigs)
            {
                cfg.ButtonRef = root.Q<Button>(cfg.Name);
                // Defensive: if the matching field is missing or its value is null, skip the icon — button shows text only.
                FieldInfo iconField = configuration.metagame.mainMenu.GetType().GetField(cfg.Name);
                string iconPath = iconField?.GetValue(configuration.metagame.mainMenu) as string;
                if (!string.IsNullOrEmpty(iconPath))
                {
                    cfg.ButtonRef.iconImage = ServiceLocator.Get<TextureManager>().GetTextureData(iconPath)?.Texture;
                }

                if (cfg.TargetView != null)
                {
                    cfg.ButtonRef.RegisterCallback<ClickEvent>(evt =>
                    {
                        UIMenuSoundPlayer.Play(UIMenuSoundPlayer.Click);
                        LoadSubView(cfg);
                    });
                }
                else if (cfg.OnClick != null)
                {
                    cfg.ButtonRef.RegisterCallback<ClickEvent>(evt =>
                    {
                        UIMenuSoundPlayer.Play(UIMenuSoundPlayer.Click);
                        cfg.OnClick(cfg);
                    });
                }

                cfg.ButtonRef.RegisterCallback<PointerEnterEvent>(evt => OnPointerEnterEvent(evt, cfg));

                cfg.ButtonRef.RegisterCallback<PointerLeaveEvent>(evt => OnPointerLeaveEvent(evt, cfg));
            }
        }

        public void LoadSubViewByName(string viewName)
        {
            var cfg = m_ButtonConfigs.Find(b => b.Name == viewName);
            if (cfg != null)
            {
                LoadSubView(cfg);
            }
        }

        private void LoadSubView(ButtonConfig cfg)
        {
            if (cfg.IsActive) return;
            Debug.Log($"Loading subview: {cfg.Name}");
            m_ContentBackground.Clear();
            HideAllSubViews();
            if (cfg.TargetView != null)
            {
                // Zuerst Show() aufrufen, damit OnEnable() ausgeführt wird
                cfg.TargetView.Show();

                // Dann erst nach dem nächsten Frame das VisualElement laden
                VisualElement element = cfg.TargetView.LoadVisualElement();
                element.style.flexGrow = 1;
                element.style.flexShrink = 1;
                m_ContentBackground.Add(element);

                SetButtonActive(cfg.Name, true);
            }
        }

        void OnPointerEnterEvent(PointerEnterEvent evt, ButtonConfig cfg)
        {
            if (cfg.IsActive) return;
            cfg.ButtonRef.style.backgroundImage = new StyleBackground(cfg.HoverIconPath);
            UIMenuSoundPlayer.Play(UIMenuSoundPlayer.Hilite);
        }

        void OnPointerLeaveEvent(PointerLeaveEvent evt, ButtonConfig cfg)
        {
            if (cfg.IsActive) return;
            cfg.ButtonRef.style.backgroundImage = StyleKeyword.Null;
        }

        private void SetButtonActive(string buttonName, bool isActive)
        {
            ClearAllActiveButtons();

            var cfg = m_ButtonConfigs.Find(b => b.Name == buttonName);
            if (cfg == null || cfg.ButtonRef == null)
                return;

            if (isActive)
            {
                cfg.ButtonRef.style.backgroundImage = new StyleBackground(cfg.ActiveIconPath);
                cfg.IsActive = true;
            }
            else
            {
                cfg.ButtonRef.style.backgroundImage = StyleKeyword.Null;
                cfg.IsActive = false;
            }
        }

        private void ClearAllActiveButtons()
        {
            //clear all buttons first
            foreach (var cfgItem in m_ButtonConfigs)
            {
                cfgItem.ButtonRef.style.backgroundImage = StyleKeyword.Null;
                cfgItem.IsActive = false;
            }
        }

        private void HideAllSubViews()
        {
            foreach (var cfg in m_ButtonConfigs)
            {
                if (cfg.TargetView != null)
                {
                    cfg.TargetView.Hide();
                }
            }
        }

        void OnDisable()
        {
            foreach (var cfg in m_ButtonConfigs)
            {
                if (cfg.ButtonRef == null) continue;

                cfg.ButtonRef.UnregisterCallback<ClickEvent>(evt => cfg.OnClick(cfg));
                cfg.ButtonRef.UnregisterCallback<ClickEvent>(evt => LoadSubView(cfg));
                cfg.ButtonRef.UnregisterCallback<PointerEnterEvent>(evt => OnPointerEnterEvent(evt, cfg));
                cfg.ButtonRef.UnregisterCallback<PointerLeaveEvent>(evt => OnPointerLeaveEvent(evt, cfg));
            }
        }

    }
}

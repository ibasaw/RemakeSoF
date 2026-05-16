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
        VisualElement m_LogoutConfirmPanel;
        Button m_LogoutConfirmYes;
        Button m_LogoutConfirmQuit;
        Button m_LogoutConfirmNo;
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
                    ShowLogoutConfirmation();
                }
            });
        }

        void OnEnable()
        {
            var root = m_UIDocument.rootVisualElement;

            m_ContentBackground = root.Q<VisualElement>("contentBackground");
            m_MainMenuBackground = root.Q<VisualElement>("mainMenu");

            // Defensive: TextureManager is registered in ApplicationEntryPoint AFTER multiplayer-role
            // gating. OnEnable can fire earlier — typically during an Editor domain reload while in
            // MetagameScene — at which point ServiceLocator.Get<TextureManager>() returns null and the
            // .Configuration access NREs. Skip texture-dependent init; the view will repaint correctly
            // on the next legitimate Show()/Hide() cycle once the service is registered.
            TextureManager textureManager = ServiceLocator.Get<TextureManager>();
            if (textureManager == null)
            {
                Debug.LogWarning("[MainMenuView] OnEnable ran before TextureManager was registered. Skipping texture init.");
                return;
            }
            TextureConfiguration configuration = textureManager.Configuration;

            Texture2D mainMenuBackgroundTexture = textureManager.GetTextureData(configuration.metagame.mainMenu.background)?.Texture;
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
                    cfg.ButtonRef.iconImage = textureManager.GetTextureData(iconPath)?.Texture;
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

            m_LogoutConfirmPanel = root.Q<VisualElement>("logoutConfirmPanel");
            m_LogoutConfirmYes = root.Q<Button>("logoutConfirmYes");
            m_LogoutConfirmQuit = root.Q<Button>("logoutConfirmQuit");
            m_LogoutConfirmNo = root.Q<Button>("logoutConfirmNo");

            if (m_LogoutConfirmYes != null)
            {
                m_LogoutConfirmYes.RegisterCallback<ClickEvent>(OnLogoutConfirmYes);
                m_LogoutConfirmYes.RegisterCallback<PointerEnterEvent>(_ => UIMenuSoundPlayer.Play(UIMenuSoundPlayer.Hilite));
            }
            if (m_LogoutConfirmQuit != null)
            {
                m_LogoutConfirmQuit.RegisterCallback<ClickEvent>(OnLogoutConfirmQuit);
                m_LogoutConfirmQuit.RegisterCallback<PointerEnterEvent>(_ => UIMenuSoundPlayer.Play(UIMenuSoundPlayer.Hilite));
            }
            if (m_LogoutConfirmNo != null)
            {
                m_LogoutConfirmNo.RegisterCallback<ClickEvent>(OnLogoutConfirmNo);
                m_LogoutConfirmNo.RegisterCallback<PointerEnterEvent>(_ => UIMenuSoundPlayer.Play(UIMenuSoundPlayer.Hilite));
            }
        }

        /// <summary>
        /// Reveals the inline logout confirmation overlay. Hidden by default in the UXML.
        /// </summary>
        void ShowLogoutConfirmation()
        {
            if (m_LogoutConfirmPanel == null) return;
            m_LogoutConfirmPanel.style.display = DisplayStyle.Flex;
        }

        void HideLogoutConfirmation()
        {
            if (m_LogoutConfirmPanel == null) return;
            m_LogoutConfirmPanel.style.display = DisplayStyle.None;
        }

        void OnLogoutConfirmYes(ClickEvent evt)
        {
            UIMenuSoundPlayer.Play(UIMenuSoundPlayer.ApplyChanges);
            HideLogoutConfirmation();
            Broadcast(new UserRequestedLogoutEvent());
        }

        void OnLogoutConfirmQuit(ClickEvent evt)
        {
            UIMenuSoundPlayer.Play(UIMenuSoundPlayer.ApplyChanges);
            HideLogoutConfirmation();
            // Quit closes the process without clearing the saved session — next launch
            // restores the current identity automatically via AuthSessionStore.TryLoad.
            // In the Editor, Application.Quit is a no-op while in Play mode, so explicitly
            // exit Play mode for the developer feedback loop.
            Debug.Log("[MainMenuView] Quit to desktop chosen — exiting application (session preserved).");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#else
            UnityEngine.Application.Quit();
#endif
        }

        void OnLogoutConfirmNo(ClickEvent evt)
        {
            UIMenuSoundPlayer.Play(UIMenuSoundPlayer.Click);
            HideLogoutConfirmation();
        }

        /// <summary>
        /// Enables/disables the logout button. Called by the controller while a network
        /// connection is in flight, to avoid logging out mid-handshake.
        /// </summary>
        public void SetLogoutEnabled(bool enabled)
        {
            ButtonConfig logoutCfg = m_ButtonConfigs.Find(b => b.Name == "logoutButton");
            if (logoutCfg == null || logoutCfg.ButtonRef == null) return;
            logoutCfg.ButtonRef.SetEnabled(enabled);
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

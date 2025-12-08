using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
namespace Tolik.RemakeSoF.Runtime
{
    [UnityEngine.RequireComponent(typeof(UIDocument))]

    internal class MainMenuView : View<MetagameApplication>
    {
        private VisualElement m_ContentBackground;
        UIDocument m_UIDocument;

        // Ein Container für alle Button-Infos
        class ButtonConfig
        {
            public string Name;
            public string HoverIconPath;
            public System.Action<ButtonConfig> OnClick;
            public View<MetagameApplication> TargetView;
            public Button ButtonRef;
            public string ActiveIconPath => HoverIconPath;
            public bool IsActive;
        }
        readonly List<ButtonConfig> m_ButtonConfigs = new();

        void Awake()
        {
            m_UIDocument = GetComponent<UIDocument>();

            m_ButtonConfigs.Add(new ButtonConfig
            {
                Name = "joinServerButton",
                HoverIconPath = "uQuake/gfx/menus/icons/icon_join_server_glow_mp",
                TargetView = App.View.JoinServerView
            });
            m_ButtonConfigs.Add(new ButtonConfig
            {
                Name = "createServerButton",
                HoverIconPath = "uQuake/gfx/menus/icons/icon_create_server_glow_mp",
                TargetView = App.View.CreateServerView
            });
            m_ButtonConfigs.Add(new ButtonConfig
            {
                Name = "optionsButton",
                HoverIconPath = "uQuake/gfx/menus/icons/icon_options_glow_mp",
                TargetView = App.View.OptionsView
            });
            m_ButtonConfigs.Add(new ButtonConfig
            {
                Name = "loadoutButton",
                HoverIconPath = "uQuake/gfx/menus/icons/icon_credits_glow_mp",
                TargetView = App.View.LoadoutView
            });
            m_ButtonConfigs.Add(new ButtonConfig
            {
                Name = "logoutButton",
                HoverIconPath = "uQuake/gfx/menus/icons/icon_quit_glow_mp",
                TargetView = App.View.LogoutView
            });
        }

        void OnEnable()
        {
            var root = m_UIDocument.rootVisualElement;

            m_ContentBackground = root.Q<VisualElement>("contentBackground");
            // Buttons finden + callbacks registrieren
            foreach (var cfg in m_ButtonConfigs)
            {
                cfg.ButtonRef = root.Q<Button>(cfg.Name);

                if (cfg.TargetView != null)
                {
                    cfg.ButtonRef.RegisterCallback<ClickEvent>(evt => LoadSubView(cfg));
                }
                else if (cfg.OnClick != null)
                {
                    cfg.ButtonRef.RegisterCallback<ClickEvent>(evt => cfg.OnClick(cfg));
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
                m_ContentBackground.Add(element);

                SetButtonActive(cfg.Name, true);
            }
        }

        void OnPointerEnterEvent(PointerEnterEvent evt, ButtonConfig cfg)
        {
            if (cfg.IsActive) return;
            cfg.ButtonRef.style.backgroundImage = new StyleBackground(
                Resources.Load<Texture2D>(cfg.HoverIconPath));
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
                cfg.ButtonRef.style.backgroundImage = new StyleBackground(
                    Resources.Load<Texture2D>(cfg.ActiveIconPath));
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

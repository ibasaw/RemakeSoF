using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
namespace Unity.DedicatedGameServerSample.Runtime
{
    [UnityEngine.RequireComponent(typeof(UIDocument))]

    internal class MainMenuView : View<MetagameApplication>
    {
        Button m_CurrentButton;
        UIDocument m_UIDocument;

        // Ein Container für alle Button-Infos
        class ButtonConfig
        {
            public string Name;
            public string HoverIconPath;
            public EventCallback<ClickEvent> OnClick;
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
                OnClick = evt => OnJoinServerButtonClick(evt)
            });
            m_ButtonConfigs.Add(new ButtonConfig
            {
                Name = "createServerButton",
                HoverIconPath = "uQuake/gfx/menus/icons/icon_create_server_glow_mp",
                OnClick = evt => OnCreateServerButtonClick(evt)
            });
            m_ButtonConfigs.Add(new ButtonConfig
            {
                Name = "optionsButton",
                HoverIconPath = "uQuake/gfx/menus/icons/icon_options_glow_mp",
                OnClick = evt => OnOptionsButtonClick(evt)
            });
            m_ButtonConfigs.Add(new ButtonConfig
            {
                Name = "loadoutButton",
                HoverIconPath = "uQuake/gfx/menus/icons/icon_credits_glow_mp",
                OnClick = evt => OnLoadoutButtonClick(evt)
            });
            m_ButtonConfigs.Add(new ButtonConfig
            {
                Name = "logoutButton",
                HoverIconPath = "uQuake/gfx/menus/icons/icon_quit_glow_mp",
                OnClick = evt => OnLogoutButtonClick(evt)
            });
        }

        void OnEnable()
        {
            var root = m_UIDocument.rootVisualElement;
            // Buttons finden + callbacks registrieren
            foreach (var cfg in m_ButtonConfigs)
            {
                cfg.ButtonRef = root.Q<Button>(cfg.Name);

                cfg.ButtonRef.RegisterCallback(cfg.OnClick);

                cfg.ButtonRef.RegisterCallback<PointerEnterEvent>(evt => OnPointerEnterEvent(evt, cfg));

                cfg.ButtonRef.RegisterCallback<PointerLeaveEvent>(evt => OnPointerLeaveEvent(evt, cfg));
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

        void OnOptionsButtonClick(ClickEvent evt)
        {
            Debug.Log("Options Button Clicked");
            SetButtonActive("optionsButton", true);
        }

        void OnLoadoutButtonClick(ClickEvent evt)
        {
            Debug.Log("Loadout Button Clicked");
            SetButtonActive("loadoutButton", true);
        }

        void OnCreateServerButtonClick(ClickEvent evt)
        {
            Debug.Log("Create Server Button Clicked");
            SetButtonActive("createServerButton", true);
        }

        void OnJoinServerButtonClick(ClickEvent evt)
        {
            Debug.Log("Join Server Button Clicked");
            SetButtonActive("joinServerButton", true);
            //Broadcast(new EnterMatchmakerQueueEvent("Queue01"));
        }
        void OnLogoutButtonClick(ClickEvent evt)
        {
            Debug.Log("Logout Button Clicked");
            SetButtonActive("logoutButton", true);
        }

        public void SetButtonActive(string buttonName, bool isActive)
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

        void OnDisable()
        {
            foreach (var cfg in m_ButtonConfigs)
            {
                if (cfg.ButtonRef == null) continue;

                cfg.ButtonRef.UnregisterCallback(cfg.OnClick);
                cfg.ButtonRef.UnregisterCallback<PointerEnterEvent>(evt => OnPointerEnterEvent(evt, cfg));
                cfg.ButtonRef.UnregisterCallback<PointerLeaveEvent>(evt => OnPointerLeaveEvent(evt, cfg));
            }
        }
    }
}

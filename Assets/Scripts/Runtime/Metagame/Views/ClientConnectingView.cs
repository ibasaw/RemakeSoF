using System;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.Core;
using Tolik.RemakeSoF.Runtime.TextureManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tolik.RemakeSoF.Runtime
{
    public class ClientConnectingView : View<MetagameApplication>
    {
        Button m_QuitButton;
        Label m_TimerLabel;
        Label m_ServerAddressLabel;
        QuakeColorLabel m_ServerNameLabel;
        UIDocument m_UIDocument;

        void Awake()
        {
            m_UIDocument = GetComponent<UIDocument>();
        }

        void OnEnable()
        {
            VisualElement root = m_UIDocument.rootVisualElement;
            m_QuitButton = root.Q<Button>("quitButton");
            m_TimerLabel = root.Q<Label>("timerLabel");
            m_ServerAddressLabel = root.Q<Label>("serverAddressLabel");

            Label serverNamePlaceholder = root.Q<Label>("serverNameLabel");
            if (serverNamePlaceholder != null)
            {
                m_ServerNameLabel = new QuakeColorLabel();
                m_ServerNameLabel.name = "serverNameLabel";
                m_ServerNameLabel.CharWidth = 18f;
                m_ServerNameLabel.CharHeight = 28f;
                m_ServerNameLabel.style.alignSelf = Align.Center;
                m_ServerNameLabel.style.marginBottom = 6;
                serverNamePlaceholder.parent.Insert(
                    serverNamePlaceholder.parent.IndexOf(serverNamePlaceholder),
                    m_ServerNameLabel);
                serverNamePlaceholder.RemoveFromHierarchy();
            }

            LoadBigcharsAtlas();
            LoadMenuTextures();
            m_QuitButton.RegisterCallback<ClickEvent>(OnClickQuit);
        }

        void OnDisable()
        {
            m_QuitButton.UnregisterCallback<ClickEvent>(OnClickQuit);
        }

        void OnClickQuit(ClickEvent evt)
        {
            Broadcast(new CancelConnectionEvent());
        }

        void Update()
        {
            TimeSpan elapsedTime = TimeSpan.FromSeconds(App.Model.ClientConnecting.ElapsedTime);
            m_TimerLabel.text = $"{elapsedTime.Minutes:D2}:{elapsedTime.Seconds:D2}";

            string serverAddress = App.Model.ClientConnecting.ServerAddress;
            if (!string.IsNullOrEmpty(serverAddress))
            {
                m_ServerAddressLabel.text = serverAddress;
            }

            string serverName = App.Model.ClientConnecting.ServerName;
            if (m_ServerNameLabel != null)
            {
                if (!string.IsNullOrEmpty(serverName))
                {
                    m_ServerNameLabel.Text = serverName;
                    m_ServerNameLabel.style.display = DisplayStyle.Flex;
                }
                else
                {
                    m_ServerNameLabel.style.display = DisplayStyle.None;
                }
            }
        }

        /// <summary>
        /// Laedt den bigchars-Atlas fuer das QuakeColorLabel ueber den TextureManager.
        /// </summary>
        void LoadBigcharsAtlas()
        {
            TextureManager textureManager = ServiceLocator.Get<TextureManager>();
            TextureConfiguration.MetagameConfiguration.ClientConnectingTextures config =
                textureManager.Configuration?.metagame?.clientConnecting;

            if (config == null || string.IsNullOrEmpty(config.bigcharsAtlas))
            {
                return;
            }

            TextureData atlasData = textureManager.GetTextureData(config.bigcharsAtlas);
            if (atlasData?.Texture != null && m_ServerNameLabel != null)
            {
                m_ServerNameLabel.Atlas = atlasData.Texture;
            }
        }

        /// <summary>
        /// Laedt alle Menu-Texturen fuer das ClientConnecting-Panel ueber den TextureManager.
        /// </summary>
        void LoadMenuTextures()
        {
            TextureManager textureManager = ServiceLocator.Get<TextureManager>();
            TextureConfiguration.MetagameConfiguration.ClientConnectingTextures config =
                textureManager.Configuration?.metagame?.clientConnecting;

            if (config == null)
            {
                return;
            }

            VisualElement root = m_UIDocument.rootVisualElement;

            ApplyTexture(textureManager, root.Q<VisualElement>("matchmaker"), config.background);
            ApplyTexture(textureManager, root.Q<VisualElement>("sof2Logo"), config.logo);
            ApplyTexture(textureManager, root.Q<VisualElement>("joinIcon"), config.joinIcon);
            ApplyTexture(textureManager, root.Q<VisualElement>("bulletIcon"), config.loadBullet);
            ApplyTexture(textureManager, root.Q<VisualElement>("clipIcon"), config.loadClip);
            ApplyTexture(textureManager, root.Q<VisualElement>("bulletIcon2"), config.loadBullet);
        }

        /// <summary>
        /// Setzt die Background-Textur eines VisualElements ueber den TextureManager.
        /// </summary>
        void ApplyTexture(TextureManager textureManager, VisualElement element, string key)
        {
            if (element == null || string.IsNullOrEmpty(key))
            {
                return;
            }

            TextureData textureData = textureManager.GetTextureData(key);
            if (textureData?.Texture != null)
            {
                element.style.backgroundImage = new StyleBackground(textureData.Texture);
            }
        }
    }
}

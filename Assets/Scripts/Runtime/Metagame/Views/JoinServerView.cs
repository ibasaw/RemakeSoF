using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.DataManagement;
using Tolik.RemakeSoF.Runtime.TextureManagement;
using UnityEngine;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

namespace Tolik.RemakeSoF.Runtime
{
    /// <summary>
    /// SoF2-authentische Server-Browser View mit Original-Texturen aus gfx/menus/.
    /// Zeigt eine Liste aller registrierten Server an und erlaubt Auswahl + Verbindung.
    /// </summary>
    [UnityEngine.RequireComponent(typeof(UIDocument))]
    internal class JoinServerView : View<MetagameApplication>
    {
        /// <summary>Timeout fuer ICMP Ping in Millisekunden.</summary>
        const int k_PingTimeoutMs = 2000;

        UIDocument m_UIDocument;

        VisualElement m_ServerListContainer;
        VisualElement m_ServerListScrollView;
        Label m_StatusLabel;
        Button m_GetListButton;
        Button m_RefreshButton;
        Button m_ConnectButton;

        /// <summary>Aktuell selektierter Server-Eintrag (oder null).</summary>
        ServerBrowserEntry m_SelectedEntry;

        /// <summary>Aktuell selektiertes Row-Element fuer Styling.</summary>
        VisualElement m_SelectedRow;

        /// <summary>Gecachte Server-Liste fuer Doppelklick-Erkennung.</summary>
        ServerBrowserEntry[] m_CachedServers;

        /// <summary>Gecachte Lock-Textur fuer Server-Zeilen mit Passwort.</summary>
        Texture2D m_LockTexture;

        void Awake()
        {
            m_UIDocument = GetComponent<UIDocument>();
        }

        void OnEnable()
        {
            VisualElement root = m_UIDocument.rootVisualElement;

            // UI-Elemente finden
            m_ServerListContainer = root.Q<VisualElement>("serverListContainer");
            m_ServerListScrollView = root.Q<VisualElement>("serverListScrollView");
            m_StatusLabel = root.Q<Label>("statusLabel");
            m_GetListButton = root.Q<Button>("getListButton");
            m_RefreshButton = root.Q<Button>("refreshButton");
            m_ConnectButton = root.Q<Button>("connectButton");

            // SoF2-Texturen laden und anwenden
            LoadAndApplyMenuTextures(root);

            // Button-Callbacks registrieren
            m_GetListButton.RegisterCallback<ClickEvent>(OnClickRefresh);
            m_RefreshButton.RegisterCallback<ClickEvent>(OnClickRefresh);
            m_ConnectButton.RegisterCallback<ClickEvent>(OnClickConnect);

            // Tooltips fuer Buttons (sichtbar beim Hovern)
            m_GetListButton.tooltip = "Get Server List";
            m_RefreshButton.tooltip = "Refresh Server List";
            m_ConnectButton.tooltip = "Join Selected Server";

            m_ConnectButton.SetEnabled(false);
            m_SelectedEntry = null;
            m_SelectedRow = null;

            // Auto-Fetch beim Anzeigen der View
            Broadcast(new RefreshServerListEvent());
        }

        void OnDisable()
        {
            if (m_GetListButton != null)
            {
                m_GetListButton.UnregisterCallback<ClickEvent>(OnClickRefresh);
            }
            if (m_RefreshButton != null)
            {
                m_RefreshButton.UnregisterCallback<ClickEvent>(OnClickRefresh);
            }
            if (m_ConnectButton != null)
            {
                m_ConnectButton.UnregisterCallback<ClickEvent>(OnClickConnect);
            }
        }

        /// <summary>
        /// Laedt alle SoF2-Menutexturen via TextureManager + TextureConfiguration und wendet sie auf UI-Elemente an.
        /// Textur-Keys werden aus TextureConfiguration.metagame.joinServer gelesen.
        /// </summary>
        void LoadAndApplyMenuTextures(VisualElement root)
        {
            TextureManager textureManager = ServiceLocator.Get<TextureManager>();
            if (textureManager == null)
            {
                Debug.LogWarning("[JoinServerView] TextureManager nicht verfuegbar, ueberspringe Texture-Loading.");
                return;
            }

            TextureConfiguration.MetagameConfiguration.JoinServerTextures config = textureManager.Configuration?.metagame?.joinServer;
            if (config == null)
            {
                Debug.LogWarning("[JoinServerView] TextureConfiguration.metagame.joinServer nicht konfiguriert.");
                return;
            }

            // Scanline CRT Overlay
            VisualElement scanlineOverlay = root.Q<VisualElement>("scanlineOverlay");
            Texture2D scanlineTex = LoadMenuTexture(textureManager, config.scanline);
            if (scanlineTex != null && scanlineOverlay != null)
            {
                scanlineOverlay.style.backgroundImage = new StyleBackground(scanlineTex);
            }

            // ScrollView Background-Textur
            Texture2D bgTex = LoadMenuTexture(textureManager, config.background);
            if (bgTex != null && m_ServerListScrollView != null)
            {
                m_ServerListScrollView.style.backgroundImage = new StyleBackground(bgTex);
            }

            // Lock-Textur cachen fuer Server-Zeilen
            m_LockTexture = LoadMenuTexture(textureManager, config.lockIcon);

            // Button-Texturen setzen
            SetupButtonTexture(textureManager, m_GetListButton, config.getListButton, config.getListButtonAlt);
            SetupButtonTexture(textureManager, m_RefreshButton, config.refreshButton, config.refreshButtonAlt);
            SetupButtonTexture(textureManager, m_ConnectButton, config.joinButton, config.joinButtonAlt);
        }

        /// <summary>
        /// Laedt eine einzelne Menu-Textur ueber den TextureManager.
        /// </summary>
        Texture2D LoadMenuTexture(TextureManager textureManager, string key)
        {
            TextureData textureData = textureManager.GetTextureData(key);
            if (textureData == null || !textureData.HasTexture())
            {
                Debug.LogWarning($"[JoinServerView] Menu-Textur nicht gefunden: {key}");
                return null;
            }

            return textureData.Texture;
        }

        /// <summary>
        /// Setzt Normal- und Hover-Textur auf einen SoF2-Submenu-Button.
        /// </summary>
        void SetupButtonTexture(TextureManager textureManager, Button button, string normalKey, string hoverKey)
        {
            if (button == null)
            {
                return;
            }

            Texture2D normalTex = LoadMenuTexture(textureManager, normalKey);
            Texture2D hoverTex = LoadMenuTexture(textureManager, hoverKey);

            if (normalTex != null)
            {
                button.style.backgroundImage = new StyleBackground(normalTex);
            }

            if (hoverTex != null)
            {
                button.RegisterCallback<PointerEnterEvent>(evt =>
                {
                    button.style.backgroundImage = new StyleBackground(hoverTex);
                });

                button.RegisterCallback<PointerLeaveEvent>(evt =>
                {
                    if (normalTex != null)
                    {
                        button.style.backgroundImage = new StyleBackground(normalTex);
                    }
                });
            }
        }

        void OnClickRefresh(ClickEvent evt)
        {
            Broadcast(new RefreshServerListEvent());
        }

        void OnClickConnect(ClickEvent evt)
        {
            if (m_SelectedEntry == null)
            {
                return;
            }

            Broadcast(new ConnectToServerEvent
            {
                ipAddress = m_SelectedEntry.ip,
                port = (ushort)m_SelectedEntry.port
            });
        }

        /// <summary>
        /// Zeigt den Lade-Status an.
        /// </summary>
        internal void ShowLoading()
        {
            m_StatusLabel.text = "Fetching servers...";
            m_GetListButton.SetEnabled(false);
            m_RefreshButton.SetEnabled(false);
        }

        /// <summary>
        /// Zeigt eine Fehlermeldung an.
        /// </summary>
        internal void ShowError(string message)
        {
            m_StatusLabel.text = message;
            m_GetListButton.SetEnabled(true);
            m_RefreshButton.SetEnabled(true);
        }

        /// <summary>
        /// Aktualisiert die Server-Liste in der UI.
        /// </summary>
        /// <param name="servers">Array von Server-Eintraegen vom Master-Server.</param>
        internal void PopulateServerList(ServerBrowserEntry[] servers)
        {
            m_CachedServers = servers;
            m_ServerListContainer.Clear();
            m_SelectedEntry = null;
            m_SelectedRow = null;
            m_ConnectButton.SetEnabled(false);
            m_GetListButton.SetEnabled(true);
            m_RefreshButton.SetEnabled(true);

            if (servers == null || servers.Length == 0)
            {
                m_StatusLabel.text = "No servers found.";
                return;
            }

            m_StatusLabel.text = $"{servers.Length} server(s) found.";

            for (int i = 0; i < servers.Length; i++)
            {
                ServerBrowserEntry entry = servers[i];
                Debug.Log($"[JoinServerView] Server[{i}]: {entry.hostname}, hasPassword={entry.hasPassword}, ip={entry.ip}");
                VisualElement row = CreateServerRow(entry);
                m_ServerListContainer.Add(row);
            }

            // Ping asynchron messen und in den Zeilen aktualisieren
            _ = MeasurePingsAsync(servers);
        }

        /// <summary>
        /// Misst den ICMP-Ping zu jedem Server und aktualisiert die Ping-Labels in der UI.
        /// Laeuft asynchron nach dem Aufbau der Liste.
        /// </summary>
        async Task MeasurePingsAsync(ServerBrowserEntry[] servers)
        {
            Task<long>[] pingTasks = new Task<long>[servers.Length];
            for (int i = 0; i < servers.Length; i++)
            {
                pingTasks[i] = PingHostAsync(servers[i].ip);
            }

            long[] results = await Task.WhenAll(pingTasks);

            // Zurueck auf Main-Thread: Labels aktualisieren
            for (int i = 0; i < results.Length; i++)
            {
                if (i >= m_ServerListContainer.childCount)
                {
                    break;
                }

                long pingMs = results[i];
                servers[i].ping = (int)pingMs;

                VisualElement row = m_ServerListContainer[i];
                Label pingLabel = row.Q<Label>(className: "server-cell-ping");
                if (pingLabel != null)
                {
                    pingLabel.text = pingMs >= 0 ? pingMs.ToString() : "timeout";
                }
            }
        }

        /// <summary>
        /// Sendet einen ICMP-Ping an den angegebenen Host und gibt die Roundtrip-Zeit in ms zurueck.
        /// Gibt -1 zurueck bei Timeout oder Fehler.
        /// </summary>
        async Task<long> PingHostAsync(string host)
        {
            if (string.IsNullOrEmpty(host))
            {
                return -1;
            }

            try
            {
                using System.Net.NetworkInformation.Ping pingSender = new();
                PingReply reply = await pingSender.SendPingAsync(host, k_PingTimeoutMs);
                if (reply.Status == IPStatus.Success)
                {
                    return reply.RoundtripTime;
                }

                return -1;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        /// <summary>
        /// Erstellt eine einzelne Server-Zeile fuer den Browser.
        /// Lock-Icon wird als Texture aus gfx/menus/icons/icon_lock_mp angezeigt.
        /// </summary>
        VisualElement CreateServerRow(ServerBrowserEntry entry)
        {
            VisualElement row = new();
            row.AddToClassList("server-row");
            row.AddToClassList("server-row-entry");

            // Schloss-Icon: VisualElement mit Textur-Hintergrund fuer Passwort-geschuetzte Server
            VisualElement lockIcon = new();
            lockIcon.AddToClassList("server-cell");
            lockIcon.AddToClassList("server-cell-lock");
            if (entry.hasPassword)
            {
                if (m_LockTexture != null)
                {
                    lockIcon.style.backgroundImage = new StyleBackground(m_LockTexture);
                    lockIcon.style.unityBackgroundImageTintColor = new StyleColor(new Color(1f, 0.3f, 0.3f, 1f));
                }
                else
                {
                    Debug.LogWarning("[JoinServerView] Lock texture is null but server hasPassword=true");
                    lockIcon.style.backgroundColor = new StyleColor(new Color(0.8f, 0.15f, 0.15f, 0.8f));
                }
                lockIcon.tooltip = "Password protected";
            }
            row.Add(lockIcon);

            Label nameLabel = new(entry.hostname ?? "Unknown");
            nameLabel.AddToClassList("server-cell");
            nameLabel.AddToClassList("server-cell-name");
            row.Add(nameLabel);

            // Map-Name: Nur den letzten Teil anzeigen (z.B. "mp_col1" statt "maps/mp_col1")
            string displayMap = entry.mapName ?? "";
            if (displayMap.Contains("/"))
            {
                displayMap = displayMap.Substring(displayMap.LastIndexOf('/') + 1);
            }
            Label mapLabel = new(displayMap);
            mapLabel.AddToClassList("server-cell");
            mapLabel.AddToClassList("server-cell-map");
            row.Add(mapLabel);

            Label playersLabel = new($"{entry.currentPlayers}/{entry.maxPlayers}");
            playersLabel.AddToClassList("server-cell");
            playersLabel.AddToClassList("server-cell-players");
            row.Add(playersLabel);

            Label gametypeLabel = new(entry.gametype ?? "");
            gametypeLabel.AddToClassList("server-cell");
            gametypeLabel.AddToClassList("server-cell-gametype");
            row.Add(gametypeLabel);

            string pingText = "...";
            Label pingLabel = new(pingText);
            pingLabel.AddToClassList("server-cell");
            pingLabel.AddToClassList("server-cell-ping");
            row.Add(pingLabel);

            // Klick-Handler: Auswahl
            row.RegisterCallback<ClickEvent>(evt => SelectRow(row, entry));

            // Doppelklick: Direkt verbinden
            row.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.clickCount == 2)
                {
                    SelectRow(row, entry);
                    Broadcast(new ConnectToServerEvent
                    {
                        ipAddress = entry.ip,
                        port = (ushort)entry.port
                    });
                }
            });

            return row;
        }

        /// <summary>
        /// Selektiert eine Server-Zeile visuell und speichert den Eintrag.
        /// </summary>
        void SelectRow(VisualElement row, ServerBrowserEntry entry)
        {
            // Vorherige Selektion entfernen
            if (m_SelectedRow != null)
            {
                m_SelectedRow.RemoveFromClassList("server-row-selected");
            }

            m_SelectedRow = row;
            m_SelectedEntry = entry;
            row.AddToClassList("server-row-selected");
            m_ConnectButton.SetEnabled(true);
        }
    }
}
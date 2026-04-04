using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.Core;
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

        /// <summary>Scroll-Schwelle in Pixel: wenn weniger als dieser Abstand zum Ende, naechstes Paket laden.</summary>
        const float k_ScrollThreshold = 50f;

        /// <summary>Mindestdistanz in Pixel bevor ein Drag erkannt wird (damit Klicks durchkommen).</summary>
        const float k_DragThreshold = 5f;

        UIDocument m_UIDocument;

        VisualElement m_ServerListContainer;
        ScrollView m_ServerListScrollView;
        Label m_StatusLabel;
        Button m_GetListButton;
        Button m_RefreshButton;
        Button m_ConnectButton;
        Button m_NewFavoriteButton;
        Button m_AddFavoriteButton;
        Button m_ServerInfoButton;
        Button m_FindFriendButton;
        QuakeColorLabel m_SelectedServerLabel;

        /// <summary>Aktuell selektierter Server-Eintrag (oder null).</summary>
        ServerBrowserEntry m_SelectedEntry;

        /// <summary>Aktuell selektiertes Row-Element fuer Styling.</summary>
        VisualElement m_SelectedRow;

        /// <summary>Gecachte Lock-Textur fuer Server-Zeilen mit Passwort.</summary>
        Texture2D m_LockTexture;

        /// <summary>Gecachte bigchars-Atlas-Textur fuer Quake-Color-Labels.</summary>
        Texture2D m_BigcharsAtlas;

        /// <summary>Ob weitere Server-Eintraege verfuegbar sind.</summary>
        bool m_HasMore;

        /// <summary>Ob ein Content-Drag aussteht (Maus gedrueckt, Schwelle noch nicht erreicht).</summary>
        bool m_ContentDragPending;

        /// <summary>Ob gerade aktiv per Drag gescrollt wird.</summary>
        bool m_IsDraggingContent;

        /// <summary>Pointer-ID fuer den aktiven Content-Drag.</summary>
        int m_ContentDragPointerId;

        /// <summary>Y-Position bei Drag-Start.</summary>
        float m_ContentDragStartY;

        /// <summary>Scroll-Wert bei Drag-Start.</summary>
        float m_ContentDragStartScroll;

        void Awake()
        {
            m_UIDocument = GetComponent<UIDocument>();
        }

        void OnEnable()
        {
            VisualElement root = m_UIDocument.rootVisualElement;

            // UI-Elemente finden
            m_ServerListContainer = root.Q<VisualElement>("serverListContainer");
            m_ServerListScrollView = root.Q<ScrollView>("serverListScrollView");
            m_StatusLabel = root.Q<Label>("statusLabel");
            m_GetListButton = root.Q<Button>("getListButton");
            m_RefreshButton = root.Q<Button>("refreshButton");
            m_ConnectButton = root.Q<Button>("connectButton");
            m_NewFavoriteButton = root.Q<Button>("newFavoriteButton");
            m_AddFavoriteButton = root.Q<Button>("addFavoriteButton");
            m_ServerInfoButton = root.Q<Button>("serverInfoButton");
            m_FindFriendButton = root.Q<Button>("findFriendButton");
            // Selected-Server-Label durch QuakeColorLabel ersetzen
            Label selectedLabelPlaceholder = root.Q<Label>("selectedServerLabel");
            if (selectedLabelPlaceholder != null)
            {
                m_SelectedServerLabel = new QuakeColorLabel();
                m_SelectedServerLabel.name = "selectedServerLabel";
                m_SelectedServerLabel.AddToClassList("sof2-selected-server");
                selectedLabelPlaceholder.parent.Insert(selectedLabelPlaceholder.parent.IndexOf(selectedLabelPlaceholder), m_SelectedServerLabel);
                selectedLabelPlaceholder.RemoveFromHierarchy();
            }

            // SoF2-Texturen laden und anwenden
            LoadAndApplyMenuTextures(root);

            // Button-Callbacks registrieren
            m_GetListButton.RegisterCallback<ClickEvent>(OnClickRefresh);
            m_RefreshButton.RegisterCallback<ClickEvent>(OnClickRefresh);
            m_ConnectButton.RegisterCallback<ClickEvent>(OnClickConnect);

            // Hover-Sounds fuer Buttons
            m_GetListButton.RegisterCallback<PointerEnterEvent>(_ => UIMenuSoundPlayer.Play(UIMenuSoundPlayer.Hilite));
            m_RefreshButton.RegisterCallback<PointerEnterEvent>(_ => UIMenuSoundPlayer.Play(UIMenuSoundPlayer.Hilite));
            m_ConnectButton.RegisterCallback<PointerEnterEvent>(_ => UIMenuSoundPlayer.Play(UIMenuSoundPlayer.Hilite));
            m_NewFavoriteButton.RegisterCallback<PointerEnterEvent>(_ => UIMenuSoundPlayer.Play(UIMenuSoundPlayer.Hilite));
            m_AddFavoriteButton.RegisterCallback<PointerEnterEvent>(_ => UIMenuSoundPlayer.Play(UIMenuSoundPlayer.Hilite));
            m_ServerInfoButton.RegisterCallback<PointerEnterEvent>(_ => UIMenuSoundPlayer.Play(UIMenuSoundPlayer.Hilite));
            m_FindFriendButton.RegisterCallback<PointerEnterEvent>(_ => UIMenuSoundPlayer.Play(UIMenuSoundPlayer.Hilite));

            // Click-Sounds fuer Toolbar-Buttons
            m_NewFavoriteButton.RegisterCallback<ClickEvent>(_ => UIMenuSoundPlayer.Play(UIMenuSoundPlayer.Click));
            m_AddFavoriteButton.RegisterCallback<ClickEvent>(_ => UIMenuSoundPlayer.Play(UIMenuSoundPlayer.Click));
            m_ServerInfoButton.RegisterCallback<ClickEvent>(_ => UIMenuSoundPlayer.Play(UIMenuSoundPlayer.Click));
            m_FindFriendButton.RegisterCallback<ClickEvent>(_ => UIMenuSoundPlayer.Play(UIMenuSoundPlayer.Click));

            // Tooltips fuer Buttons (sichtbar beim Hovern)
            m_GetListButton.tooltip = "Get Server List";
            m_RefreshButton.tooltip = "Refresh Server List";
            m_ConnectButton.tooltip = "Join Selected Server";

            m_ConnectButton.SetEnabled(false);
            m_SelectedEntry = null;
            m_SelectedRow = null;
            m_HasMore = false;
            m_ContentDragPending = false;
            m_IsDraggingContent = false;

            // Infinite-Scroll: Scroll-Event abonnieren
            m_ServerListScrollView.verticalScroller.valueChanged += OnScrollValueChanged;

            // Content-Drag fuer vertikales Scrollen per Maus-Drag
            m_ServerListScrollView.contentContainer.RegisterCallback<PointerDownEvent>(OnContentDragDown);
            m_ServerListScrollView.contentContainer.RegisterCallback<PointerMoveEvent>(OnContentDragMove);
            m_ServerListScrollView.contentContainer.RegisterCallback<PointerUpEvent>(OnContentDragUp);
            m_ServerListScrollView.contentContainer.RegisterCallback<PointerCaptureOutEvent>(OnContentDragCaptureOut);

            // Auto-Fetch beim Anzeigen der View
            Broadcast(new RefreshServerListEvent());
        }

        void OnDisable()
        {
            if (m_ServerListScrollView != null)
            {
                m_ServerListScrollView.verticalScroller.valueChanged -= OnScrollValueChanged;
                m_ServerListScrollView.contentContainer.UnregisterCallback<PointerDownEvent>(OnContentDragDown);
                m_ServerListScrollView.contentContainer.UnregisterCallback<PointerMoveEvent>(OnContentDragMove);
                m_ServerListScrollView.contentContainer.UnregisterCallback<PointerUpEvent>(OnContentDragUp);
                m_ServerListScrollView.contentContainer.UnregisterCallback<PointerCaptureOutEvent>(OnContentDragCaptureOut);
            }
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

            // bigchars-Atlas fuer Quake-Color-Labels cachen
            m_BigcharsAtlas = LoadMenuTexture(textureManager, config.bigcharsAtlas);

            // Selected-Server-Label bekommt auch den Atlas
            if (m_SelectedServerLabel != null)
            {
                m_SelectedServerLabel.Atlas = m_BigcharsAtlas;
            }

            // Lock-Icon im Column-Header setzen
            VisualElement headerLock = root.Q<VisualElement>(className: "server-cell-lock");
            if (m_LockTexture != null && headerLock != null)
            {
                headerLock.style.backgroundImage = new StyleBackground(m_LockTexture);
            }

            // Button-Texturen setzen
            SetupButtonTexture(textureManager, m_GetListButton, config.getListButton, config.getListButtonAlt);
            SetupButtonTexture(textureManager, m_RefreshButton, config.refreshButton, config.refreshButtonAlt);
            SetupButtonTexture(textureManager, m_ConnectButton, config.joinButton, config.joinButtonAlt);

            // Vertikale Scrollbar-Texturen anwenden
            ApplyScrollbarTextures(textureManager, config);
        }

        /// <summary>
        /// Wendet SoF2-Texturen auf die vertikale Scrollbar der Server-Liste an.
        /// </summary>
        void ApplyScrollbarTextures(TextureManager textureManager, TextureConfiguration.MetagameConfiguration.JoinServerTextures config)
        {
            if (m_ServerListScrollView == null)
            {
                return;
            }

            Scroller verticalScroller = m_ServerListScrollView.verticalScroller;

            // Up Arrow
            Texture2D arrowUpTex = LoadMenuTexture(textureManager, config.scrollbarArrowUp);
            if (arrowUpTex != null)
            {
                RepeatButton lowButton = verticalScroller.lowButton;
                lowButton.style.backgroundImage = new StyleBackground(arrowUpTex);
                lowButton.text = "";
            }

            // Down Arrow
            Texture2D arrowDownTex = LoadMenuTexture(textureManager, config.scrollbarArrowDown);
            if (arrowDownTex != null)
            {
                RepeatButton highButton = verticalScroller.highButton;
                highButton.style.backgroundImage = new StyleBackground(arrowDownTex);
                highButton.text = "";
            }

            // Track
            Texture2D trackTex = LoadMenuTexture(textureManager, config.scrollbarTrack);
            if (trackTex != null)
            {
                VisualElement tracker = verticalScroller.slider.Q<VisualElement>("unity-tracker");
                if (tracker != null)
                {
                    tracker.style.backgroundImage = new StyleBackground(trackTex);
                }
            }

            // Thumb (Dragger)
            Texture2D thumbTex = LoadMenuTexture(textureManager, config.scrollbarThumb);
            if (thumbTex != null)
            {
                VisualElement dragger = verticalScroller.slider.Q<VisualElement>("unity-dragger");
                if (dragger != null)
                {
                    dragger.style.backgroundImage = new StyleBackground(thumbTex);
                }
            }
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
            UIMenuSoundPlayer.Play(UIMenuSoundPlayer.Click);
            Broadcast(new RefreshServerListEvent());
        }

        void OnClickConnect(ClickEvent evt)
        {
            if (m_SelectedEntry == null)
            {
                return;
            }

            UIMenuSoundPlayer.Play(UIMenuSoundPlayer.ApplyChanges);

            Broadcast(new ConnectToServerEvent
            {
                ipAddress = m_SelectedEntry.ip,
                port = (ushort)m_SelectedEntry.port,
                serverName = m_SelectedEntry.hostname
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
        /// Beginnt eine neue Server-Liste. Loescht vorherige Eintraege und setzt Status.
        /// </summary>
        /// <param name="totalCount">Gesamtanzahl aller verfuegbaren Server.</param>
        internal void BeginServerList(int totalCount)
        {
            m_ServerListContainer.Clear();
            m_SelectedEntry = null;
            m_SelectedRow = null;
            m_ConnectButton.SetEnabled(false);
            m_GetListButton.SetEnabled(true);

            if (m_SelectedServerLabel != null)
            {
                m_SelectedServerLabel.Text = "";
            }
            m_RefreshButton.SetEnabled(true);
            m_HasMore = false;

            if (totalCount == 0)
            {
                m_StatusLabel.text = "No servers found.";
                return;
            }

            m_StatusLabel.text = $"{totalCount} server(s) found.";
        }

        /// <summary>
        /// Haengt ein Paket von Server-Eintraegen an die bestehende Liste an (infinite scroll).
        /// </summary>
        /// <param name="batch">Die naechsten Server-Eintraege.</param>
        /// <param name="loadedSoFar">Bisher insgesamt geladene Anzahl.</param>
        /// <param name="totalCount">Gesamtanzahl aller Server.</param>
        /// <param name="hasMore">Ob noch weitere Eintraege verfuegbar sind.</param>
        internal void AppendServerBatch(ServerBrowserEntry[] batch, int loadedSoFar, int totalCount, bool hasMore)
        {
            m_HasMore = hasMore;

            for (int i = 0; i < batch.Length; i++)
            {
                ServerBrowserEntry entry = batch[i];
                VisualElement row = CreateServerRow(entry);
                m_ServerListContainer.Add(row);
            }

            m_StatusLabel.text = hasMore
                ? $"{loadedSoFar} / {totalCount} server(s) loaded..."
                : $"{totalCount} server(s) found.";

            // Wenn der Content den Viewport noch nicht fuellt, sofort naechstes Paket anfordern
            if (hasMore)
            {
                m_ServerListScrollView.schedule.Execute(() =>
                {
                    float scrollMax = m_ServerListScrollView.verticalScroller.highValue;
                    if (scrollMax <= 0f)
                    {
                        Broadcast(new LoadMoreServersEvent());
                    }
                });
            }
        }

        /// <summary>
        /// Prueft ob der Nutzer nah genug am Ende der Liste gescrollt hat und laedt das naechste Paket.
        /// </summary>
        void OnScrollValueChanged(float value)
        {
            if (!m_HasMore)
            {
                return;
            }

            float scrollMax = m_ServerListScrollView.verticalScroller.highValue;
            if (scrollMax <= 0f)
            {
                return;
            }

            if (value >= scrollMax - k_ScrollThreshold)
            {
                Broadcast(new LoadMoreServersEvent());
            }
        }

        // ── Content Drag (threshold-basiert, Klicks kommen durch) ──

        /// <summary>
        /// Startet einen potenziellen Drag wenn die linke Maustaste gedrueckt wird.
        /// </summary>
        void OnContentDragDown(PointerDownEvent evt)
        {
            if (evt.button != 0)
            {
                return;
            }

            m_ContentDragPending = true;
            m_IsDraggingContent = false;
            m_ContentDragPointerId = evt.pointerId;
            m_ContentDragStartY = evt.position.y;
            m_ContentDragStartScroll = m_ServerListScrollView.verticalScroller.value;
        }

        /// <summary>
        /// Prueft ob die Drag-Schwelle ueberschritten ist und scrollt die Liste vertikal.
        /// </summary>
        void OnContentDragMove(PointerMoveEvent evt)
        {
            if (!m_ContentDragPending && !m_IsDraggingContent)
            {
                return;
            }

            if (m_ContentDragPending)
            {
                float distance = Mathf.Abs(evt.position.y - m_ContentDragStartY);
                if (distance < k_DragThreshold)
                {
                    return;
                }

                m_ContentDragPending = false;
                m_IsDraggingContent = true;
                m_ServerListScrollView.contentContainer.CapturePointer(m_ContentDragPointerId);
            }

            if (m_IsDraggingContent)
            {
                float delta = m_ContentDragStartY - evt.position.y;
                m_ServerListScrollView.verticalScroller.value = m_ContentDragStartScroll + delta;
                evt.StopPropagation();
            }
        }

        /// <summary>
        /// Beendet den Content-Drag und gibt den Pointer frei.
        /// </summary>
        void OnContentDragUp(PointerUpEvent evt)
        {
            if (m_IsDraggingContent)
            {
                m_IsDraggingContent = false;
                m_ServerListScrollView.contentContainer.ReleasePointer(evt.pointerId);
                evt.StopPropagation();
            }

            m_ContentDragPending = false;
        }

        /// <summary>
        /// Setzt den Drag-State zurueck wenn der Pointer-Capture extern verloren geht.
        /// </summary>
        void OnContentDragCaptureOut(PointerCaptureOutEvent evt)
        {
            m_IsDraggingContent = false;
            m_ContentDragPending = false;
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
                    lockIcon.style.unityBackgroundImageTintColor = new StyleColor(new Color(0.78f, 0.12f, 0.12f, 1f));
                }
                else
                {
                    Debug.LogWarning("[JoinServerView] Lock texture is null but server hasPassword=true");
                    lockIcon.style.backgroundColor = new StyleColor(new Color(0.8f, 0.15f, 0.15f, 0.8f));
                }
                lockIcon.tooltip = "Password protected";
            }
            row.Add(lockIcon);

            // Quake-Color-Label: rendert farbige Servernamen via bigchars-Atlas
            QuakeColorLabel colorName = new();
            colorName.AddToClassList("server-cell");
            colorName.AddToClassList("server-cell-name");
            colorName.Atlas = m_BigcharsAtlas;
            colorName.Text = entry.hostname ?? "Unknown";
            row.Add(colorName);

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

            Label gametypeLabel = new(entry.gametype ?? "");
            gametypeLabel.AddToClassList("server-cell");
            gametypeLabel.AddToClassList("server-cell-gametype");
            row.Add(gametypeLabel);

            Label playersLabel = new($"{entry.currentPlayers}/{entry.maxPlayers}");
            playersLabel.AddToClassList("server-cell");
            playersLabel.AddToClassList("server-cell-players");
            row.Add(playersLabel);

            string pingText = entry.ping > -1 ? entry.ping.ToString() : "...";
            Label pingLabel = new(pingText);
            pingLabel.AddToClassList("server-cell");
            pingLabel.AddToClassList("server-cell-ping");
            row.Add(pingLabel);

            // Klick-Handler: Auswahl
            row.RegisterCallback<ClickEvent>(evt =>
            {
                UIMenuSoundPlayer.Play(UIMenuSoundPlayer.Select);
                SelectRow(row, entry);
            });

            // Hover-Sound
            row.RegisterCallback<PointerEnterEvent>(_ => UIMenuSoundPlayer.Play(UIMenuSoundPlayer.Hilite));

            // Doppelklick: Direkt verbinden
            row.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.clickCount == 2)
                {
                    SelectRow(row, entry);
                    Broadcast(new ConnectToServerEvent
                    {
                        ipAddress = entry.ip,
                        port = (ushort)entry.port,
                        serverName = entry.hostname
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

            if (m_SelectedServerLabel != null)
            {
                m_SelectedServerLabel.Text = entry.hostname ?? "Unknown";
            }
        }
    }
}
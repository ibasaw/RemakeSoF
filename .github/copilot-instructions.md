# Copilot Instructions for RemakeSoF


## Important Developer Coding Rules
- NEVER use a `var` declaration. ALWAYS use explicit types for better readability and maintainability.
- Always include XML documentation comments (`/// <summary>...</summary>`) for all classes, methods, and public members to ensure clarity of purpose and usage.
- Always use `new(TypeName)` syntax for object instantiation instead of `new TypeName()`. This improves performance by reducing IL code size.
- Always follow the established project architecture and design patterns as outlined below.

## Clean Coding Standards
- **KISS (Keep It Simple, Stupid)**: Bevorzuge einfache, klare Lösungen gegenüber komplexen; vermeide Over-Engineering; jede Klasse/Methode sollte eine klare, verständliche Aufgabe haben.
- **DRY (Don't Repeat Yourself)**: Keine Code-Duplikation; extrahiere wiederholte Logik in gemeinsame Methoden/Klassen; nutze Vererbung/Composition sinnvoll.
- **YAGNI (You Aren't Gonna Need It)**: Implementiere nur Features, die aktuell benötigt werden; keine spekulativen Erweiterungen; halte Code fokussiert auf aktuelle Requirements.
- **Single Responsibility Principle (SRP)**: Jede Klasse hat genau eine Verantwortung; Manager orchestrieren, Loader laden Daten, Applier wenden Assets an; keine Mixed Concerns.
- **Separation of Concerns**: Klare Trennung zwischen Datenlogik (Loader), Asset-Anwendung (Applier), Orchestrierung (Manager), UI (View/Controller); siehe Service Decomposition Pattern.
- **Clean Architecture**: Abhängigkeiten zeigen immer nach innen; Pure Services haben keine MonoBehaviour-Dependencies; Applier bekommen nur Daten, keine Loader-Referenzen; Manager orchestrieren, delegieren nicht ihre Verantwortung.
- **Explicit over Implicit**: Keine magischen Strings/Numbers; explizite Typen statt `var`; klare Methodennamen; Konstanten für wiederholte Werte.
- **Fail Fast**: Validierung früh durchführen; klare Error-Messages; Guard Clauses am Anfang von Methoden.
- **Composition over Inheritance**: Bevorzuge Komposition (Service Decomposition) statt tiefe Vererbungshierarchien.
- **Immutability where possible**: Readonly Fields/Properties wo sinnvoll; private Setter für interne State-Änderungen; keine unerwarteten Side Effects.

## Project Architecture
- **Core Movement**: Quake III/SoF2 Bewegung mit Unity-Anpassungen (manuelle Physik bevorzugt).
- **MVC Architecture (Runtime/Core)**:
  - **BaseApplication**: Root-Klasse für Scene-Scripts; verwaltet EventManager und findet Model/View/Controller-Instanzen per DFS; generisch typisierbar `BaseApplication<M,V,C>`.
  - **Model**: Basisklasse für Datenhaltung; `Model<T>` ermöglicht typsichere App-Referenzen.
  - **View**: Basisklasse für UI-Darstellung (MonoBehaviour); `View<T>` mit `LoadVisualElement()` für UIToolkit-Integration, `Show()`/`Hide()` für Activation.
  - **Controller**: Basisklasse für MVC-Bridge; `Controller<T>` handheld Event-Listeners auf App-EventManager; `AddListener<E>()` / `RemoveListener<E>()` / `RemoveListeners()`.
  - **Element**: Gemeinsame Basis für alle MVC-Klassen; lazy-loaded App-Reference, `Find<T>()` für Component-Suche, `Broadcast(evt)` für Event-Versand.
  - **EventManager**: Typ-sichere Event-Broadcasting; zentrales Kommunikationssystem für MVC-Komponenten.
- **State Machine Architecture (Runtime/Core)**:
  - **StateMachine<TState, TSelf>**: Generische Base-Klasse für Manager; CRTP-Pattern ermöglicht States Rückreferenz zum Manager; `ChangeState()` ruft Exit/Enter auf; `EventManager` für State-Events.
  - **State<TManager>**: Abstrakte Base-Klasse für konkrete States; implementiert `Enter()` und `Exit()`; Manager wird per Property gesetzt.
- **Pure Services (keine MonoBehaviours, via ServiceLocator)**:
  - **PrefabManager**: Cache-first Addressables, delegiert an `PrefabRegistry`, nur über ServiceLocator/DI.
  - **PrefabRegistry**: Interner Cache + Addressables, released Handles on `ClearCache()`, keine Custom-Prefab-Loader.
  - **TextureManager**: Baut Materialien aus Skin-Definitionen, nutzt `TextureRegistry`, zugreifbar via ServiceLocator, `ClearCache()` delegiert.
  - **TextureRegistry**: Cache + Custom Loader (Default Lazy Loader), kein Observer-Pattern, `ClearCache()` zerstört Texturen/Materialien.
- **MonoBehaviour Manager / State Machines**:
  - **PlayerSkinManager**: StateMachine (Idle/Loading/Applied/Error); orchestriert Skin-Laden & -Anwenden; lädt Daten via 4 interne Loader, übergibt konkrete Daten an `PlayerSkinApplier`; Input via `IPlayerSkinChangeHandler` je State; Output-Events zentral im Manager (`OnSkinApplied()`, `OnSkinLoadFailure()` via `EventManager`); nutzt `PrefabManager`, `TextureManager` via ServiceLocator.
  - **SkinDefinitionLoader** (intern): Lädt Skin-Definitionen aus Resources; Methoden: `GetByName()`, `GetNextSkinName()`, `GetPreviousSkinName()`, `GetSkinsForModel()`, `GetAvailableModels()`.
  - **SurfaceDefinitionLoader** (intern): Lädt NPC_definition.json; Methode: `GetByModelName()` liefert Surface-Definitionen.
  - **CharacterTemplateLoader** (intern): Lädt SoF2_NPCs.json; Methoden: `GetBySkinName()`, `GetByName()`.
  - **LegacyShaderLoader** (intern): Lädt .shader Files von Disk; Methode: `GetForModel()` liefert Shader-Definitionen.
  - **PlayerSkinApplier** (intern): Reine Asset-Anwendung; bekommt nur konkrete Daten (keine Loader-Referenzen); Methoden: `ApplyAnimatorController()`, `ApplySurfaceDefinitions()`, `DisableAndEnableSurfaces()`; findet Renderer per Match-Logik, verwaltet Aktivierung.
  - **ConnectionManager**: StateMachine für NGO; leitet NetworkManager-Callbacks (OnConnectionEvent, OnServerStarted, ApprovalCheck, OnTransportFailure, OnServerStopped) an den aktuellen State weiter; Abos in `Awake`, Deregistrierung in `OnDestroy`.
  - **AuthenticationManager**: StateMachine (Unauthenticated/Authenticating/Authenticated/SessionExpired); Input via `IAuthenticationHandler` je State; Output-Events zentral im Manager (`OnAuthenticationSuccess()`, `OnAuthenticationFailure()`, `OnSessionExpired()`); verwaltet Authentifizierungsverlauf und Token-Refresh.
  - **ConsoleManager**: StateMachine (ConsoleInactive/ConsoleActive); leitet Kommandos und Aktivierungszustände an den aktuellen State; verwaltet Konsolen-UI und Befehlsausführung.
  - **ApplicationEntryPoint**: Registriert Pure Services im `ServiceLocator` (z. B. TextureManager/PrefabManager) in `Awake`; hält serialisierte MonoBehaviour-Manager (Connection/Authentication/PlayerSkin/Console); ruft `ServiceLocator.ClearAll()` in `OnDestroy` auf.
- **Sonstiges**: `PrefabDataFactory` bleibt als Helper für PrefabManager.

## Scene Architecture
- **Metagame Scene**: Nutzt MVC-Pattern mit generischem `MetagameApplication<MetagameModel, MetagameView, MetagameController>`. Ist die Hub-Scene für Spieler vor dem Joinen eines Games (Login, Skin-Auswahl, etc.).
- **Game Scene**: Nutzt MVC-Pattern mit generischem `GameApplication<GameModel, GameView, GameController>`. Ist die Main-Gameplay-Scene mit Netcode-Integration, Server/Client-Character-Synchronisation, und networked game state.

## Application Lifecycle & Initialization
- **ServiceLocator**: Typ-sicherer Service-Container für Pure Services; `Register<T>(T service)` registriert, `Get<T>()` ruft ab; `ClearAll()` räumt auf und ruft `ClearCache()` bei Managern auf; initialisiert in `ApplicationEntryPoint.Awake()`.
- **ApplicationEntryPoint**: Singleton, DontDestroyOnLoad; registriert Pure Services (`TextureManager`, `PrefabManager`) in `Awake`; hält MonoBehaviour-Manager (Connection/Authentication/PlayerSkin) als serialisierte Felder; initialisiert Network via `InitializeNetworkLogic()` in `[RuntimeInitializeOnLoadMethod]`.
- **Network Initialization**: Server startet Port-Listening, setzt Framerate/VSync, lädt GameScene nach erfolgreicher Initialisierung; Client lädt MetagameScene, verbindet sich optional auto via `AutoConnectOnStartup`; CommandLineArgumentsParser liest `--port` und `--target-framerate`.

## Patterns & Conventions
- **Single Source of Truth**: Manager halten immer aktuelle State-Daten (z. B. `m_CurrentPlayerPrefab`, `m_CurrentSkinName`); Events sind nur Trigger (minimal Payload); Views/Controller fragen Daten beim Manager an statt aus Events zu lesen; keine State-Duplikation in UI; Manager garantiert State-Konsistenz.
- **Klare Trennungen (Separation of Concerns)**:
  - **Manager**: Hält State, orchestriert, sendet Events, ist Single Source of Truth.
  - **State**: Nur Orchestrierung (Enter/Exit), keine Business-Logik, delegiert an Manager.
  - **Interne Services** (Loader, Applier): Spezifische Operationen (Asset-Laden, Material-Anwendung), keine State-Haltung.
  - **Views**: Nur UI-Rendering und User-Input, fragen State beim Manager ab, senden Events für Actions.
  - **Controller**: UI-Bridge; abonniert Manager-Events, leitet zu Views weiter, triggert Manager-Actions.
  - **Pure Services** (PrefabManager, TextureManager): Global verfügbar, Cache-first, Lifecycle-Management.
- **Dependency Injection**: 
  - Pure Services (global verfügbar) immer über `ServiceLocator.Get<T>()` beziehen (z. B. `PrefabManager`, `TextureManager`).
  - Interne Services (nur von einem Manager genutzt): Manager instanziiert Loader direkt; keine ServiceLocator-Nutzung.
  - Daten-Services (Applier): Bekommen keine Loader-Referenzen, nur konkrete Daten als Parameter; Manager lädt Daten, übergibt sie an Applier.
  - Keine Singleton-Zugriffe über `ApplicationEntryPoint.Singleton` für Services.
- **Cache-First**: Immer über `PrefabManager`/`TextureManager`; Registries nicht umgehen; Cache-Flush via `ClearCache()`/`ServiceLocator.ClearAll()`.
- **State Machines**: Input über State-spezifische Interfaces; Events/Output werden vom Manager (nicht vom State) via `EventManager` gesendet; States steuern nur Transitionen.
- **Service Decomposition**: Komplexe Manager können interne Services (keine MonoBehaviours) nutzen für bessere Separation of Concerns; z. B. `PlayerSkinManager` → 4 Loader (SkinDefinition, SurfaceDefinition, CharacterTemplate, LegacyShader) + `PlayerSkinApplier`. Manager orchestriert, Loader laden Daten, Applier wendet Assets an.
- **Addressables Only**: Prefab-Loading ausschließlich Addressables, keine Custom Prefab Loader; Custom Loader nur in `TextureRegistry` erlaubt.
- **Lifecycle**: Externe Callbacks in `Awake` abonnieren und in `OnDestroy` sauber deregistrieren; `ServiceLocator.ClearAll()` beim Teardown.
- **Manual Physics**: Für Kernbewegung explizite Physik-/Kollisionslogik bevorzugen.
- **Logging**: Kurze Warnungen/Errors; keine Observer-Benachrichtigungen in TextureRegistry.

## Dedicated Server & Networking
- **Multiplayer Roles**: Server/Client-Rollen via `Unity.DedicatedServer.MultiplayerRoles`; konfigurierbar über Command-Line-Arguments (`--port`, `--target-framerate`).
- **CommandLineArgumentsParser**: Parst Server-Startparameter (Default Port: 7777, Default TargetFramerate: 30).
- **NetworkedGameState**: Zentrale Synchronisation des Game-State zwischen Server und Clients.

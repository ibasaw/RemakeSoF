# Dedicated Server Build — Referenz

Dokumentation der Server-Build-Konfiguration, bekannte Probleme und Fixes.

---

## Server-Build Architektur

Der Dedicated Server nutzt dieselben Scenes und Prefabs wie der Client, stripped aber
visuelle Systeme (Shader, Texturen, Audio, Partikel) per `Dedicated Server Optimizations`.

### Was der Server lädt

| Asset-Typ | Geladen? | Grund |
|-----------|----------|-------|
| Map-Prefabs (Addressables) | Ja | Collider-Erstellung |
| Character-Skeletons | Ja | Hitbox-Tracking (Bone-Animation) |
| Animator-Controller | Ja | Server-Bones korrekt animieren |
| Waffen-Definitionen (JSON) | Ja | Gameplay-Logik (Damage, Ammo, Fire-Rate) |
| Skin-Definitionen (JSON) | Ja | Model-Name → Skeleton-Prefab Lookup |
| Texturen / Art-Ordner | Nein | Keine Visuals auf Server |
| Shader / Materials | Nein | Gestripped durch Dedicated Server Optimizations |
| Sound / Audio | Nein | SoundManager nicht registriert auf Server |
| Effekte / Partikel | Nein | EffectFactory nicht registriert auf Server |
| Gore-System | Nein | GoreManager nicht registriert auf Server |

### Service-Registrierung (ApplicationEntryPoint)

**Server registriert:**
- SkinDefinitionLoader
- WeaponDataLoader
- EffectDataLoader (nur für Definition-Lookup, nicht Rendering)
- SurfaceImpactDataLoader
- MapDataLoader
- PrefabManager
- PrefabRegistry

**Server registriert NICHT:**
- TextureManager / TextureRegistry
- SoundManager
- EffectFactory
- GoreManager

---

## Server-Guards in ClientRpcs

Alle `[Rpc(SendTo.Everyone)]` RPCs die visuelle/Audio-Effekte erzeugen müssen einen
Dedicated-Server-Guard haben, weil `SendTo.Everyone` den RPC auch auf dem Server ausführt.

### Pattern

```csharp
[Rpc(SendTo.Everyone)]
private void SomeVisualClientRpc(...)
{
    // Dedicated Server: keine visuellen Effekte — Shader sind gestripped.
    if (IsServer && !IsHost)
    {
        return;
    }
    // ... Client-only Visual-Code
}
```

### Geschützte RPCs (NetworkedPlayerCharacter)

| RPC | Guard | Grund |
|-----|-------|-------|
| TracerClientRpc | `IsServer && !IsHost` | TracerVisual → Shader.Find() → Material |
| ProjectileSpawnClientRpc | `IsServer && !IsHost` | ClientProjectileVisual → Shader/Material |
| MuzzleEffectsClientRpc | `IsServer && !IsHost` | EffectFactory → Partikel → Shader |
| WeaponReadySoundClientRpc | ServiceLocator null-check | SoundManager nicht registriert |
| WeaponEmptySoundClientRpc | ServiceLocator null-check | SoundManager nicht registriert |
| GrenadeSoundClientRpc | ServiceLocator null-check | SoundManager nicht registriert |
| ReloadSoundClientRpc | ServiceLocator null-check | SoundManager nicht registriert |

### ClientPlayerCharacter Server-Guard

`ClientPlayerCharacter.OnNetworkSpawn()` hat einen Early-Return für den Dedicated Server:

```csharp
if (m_NetworkedPlayerCharacter.IsServer && !m_NetworkedPlayerCharacter.IsHost)
{
    return; // Keine Waffen-Visuals, kein Input, keine Kamera
}
```

Hitboxen werden weiterhin von `ClientCharacterSkinHandler` + `ClientHitboxSystem` verwaltet,
die eigene Server-Guards mit Skeleton-only-Loading haben.

---

## MeshCollider Cooking Options

### Problem

Explizites Setzen von `cookingOptions` (auch wenn identisch mit Unity-Defaults) erzwingt
`Read/Write Enabled` auf den Meshes. Ohne Read/Write: CollisionMeshData kann nicht erstellt
werden → Spieler fällt durch die Map.

### Fix

`cookingOptions`-Zuweisung in `MapColliderApplier.cs` entfernt. Unity nutzt intern
dieselben Defaults (`CookForFasterSimulation | EnableMeshCleaning | WeldColocatedVertices`),
aber ohne die Read/Write-Anforderung.

**Vorher:**
```csharp
MeshCollider collider = go.AddComponent<MeshCollider>();
collider.cookingOptions = MeshColliderCookingOptions.CookForFasterSimulation
                        | MeshColliderCookingOptions.EnableMeshCleaning
                        | MeshColliderCookingOptions.WeldColocatedVertices;
collider.sharedMesh = mesh;
```

**Nachher:**
```csharp
MeshCollider collider = go.AddComponent<MeshCollider>();
collider.sharedMesh = mesh;
```

### Alternative

Falls custom cooking options tatsächlich benötigt werden: Mesh-Import-Settings auf
`Read/Write Enabled = true` setzen (Model Import Settings → Read/Write).

---

## Bekannte harmlose Warnings

| Warning | Ursache | Auswirkung |
|---------|---------|------------|
| `Built-in Resource Error: dereference potentially before BuiltinResourceManager` | Unity Startup lädt Built-in Resources die im Server-Build gestripped sind | Keine — bekanntes Unity-Problem |
| `Trying to access a shader` (wenige beim Startup) | Map-Prefab hat Material-Referenzen auf Renderern die beim Instantiate kurzzeitig Shader referenzieren | Keine — Renderer werden vom Server nicht genutzt |
| `Microsoft Media Foundation video decoding disabled` | Server hat kein Graphics Device | Keine |

---

## Betroffene Dateien

- `Assets/Scripts/Runtime/Game/MapLoader/MapColliderApplier.cs` — cookingOptions entfernt
- `Assets/Scripts/Runtime/Game/Characters/Client/ClientPlayerCharacter.cs` — Server-Guard
- `Assets/Scripts/Runtime/Game/Characters/Networked/NetworkedPlayerCharacter.cs` — RPC Guards
- `Assets/Scripts/Runtime/Game/Characters/Client/ClientCharacterSkinHandler.cs` — Server Skeleton-Loading (existierte bereits)

# SoF2 Custom Shader — Übersicht

## Shader-Dateien (Assets/Shaders/)

| Datei | Shader-Name | Typ | Zweck |
|-------|------------|-----|-------|
| SoF2MapSurface.shader | `SoF2/MapSurface` | Opaque | Standard-Shader: Unlit-Basis + anteilige Lambert-Beleuchtung (_LightBlend). Simuliert idTech3 gebackene Lightmaps + Realtime-Licht. |
| SoF2Water.shader | `SoF2/Water` | Transparent | Wellen-Animation, UV-Turbulenz, Texture-Scroll, Depth-Fade, Fresnel. |
| SoF2Glass.shader | `SoF2/Glass` | Transparent | Fresnel-Reflektion, Blinn-Phong Specular, Depth-Fade, konfigurierbare Opacity. |
| SoF2Metal.shader | `SoF2/Metal` | Opaque | Metallisches Specular, Fresnel-Rim, hohe Metallic-Werte. |
| SoF2Ice.shader | `SoF2/Ice` | Transparent | Sub-Surface Scattering, starkes Specular, Fresnel, Depth-Fade. |
| SoF2Polished.shader | `SoF2/Polished` | Opaque | Hartes Specular für Marmor/Fliesen, Fresnel-Rim. |
| SoF2Decal.shader | `SoF2/Decal` | Transparent | Scorch Marks, Einschüsse, Blutflecken. Alpha-Blending, Depth-Bias gegen Z-Fighting. |
| SoF2EffectParticle.shader | `SoF2/EffectParticle` | Transparent | Soft Particles + HDR Emission für Bloom. Konfigurierbares Blending. |
| SoF2ProjectileTrail.shader | `SoF2/ProjectileTrail` | Transparent | Soft-Edge Glow, HDR Core-Brightness, Vertex-Color vom TrailRenderer. |
| SoF2Distortion.shader | `SoF2/Distortion` | Transparent | Hitze-Schlieren per Normal-Map UV-Verschiebung auf Scene-Color. |

## Material-Mapping (MapTextureApplier)

| q3map_material Wert | → Unity Shader |
|---------------------|----------------|
| WATER | SoF2/Water |
| GLASS, BPGLASS, SHATTERGLASS | SoF2/Glass |
| SOLIDMETAL, HOLLOWMETAL, ARMOR | SoF2/Metal |
| ICE | SoF2/Ice |
| MARBLE, TILES | SoF2/Polished |
| (alles andere) | SoF2/MapSurface (Default) |

## Verwendung im Code

| Shader | Referenziert von |
|--------|-----------------|
| MapSurface | TextureManager, PrefabTextureApplier, WeaponLoader, MapTextureApplier, ClientProjectileVisual |
| Water, Glass, Metal, Ice, Polished | MapTextureApplier (per q3map_material) |
| Decal | PGoreDecalApplier, EffectFactory |
| EffectParticle | EffectFactory (Partikel + Gore-Fallback) |
| ProjectileTrail | EffectFactory (Trail-Effekte) |
| Distortion | EffectFactory (Hitze-Schlieren nach Explosionen) |

## BuildProcessor (Always Included Shaders)
`Assets/Scripts/Editor/BuildProcessor.cs` stellt sicher, dass `SoF2/MapSurface` + URP-Fallbacks in Always Included Shaders stehen (wegen `Shader.Find()` zur Laufzeit).

**Hinweis**: Nicht alle SoF2-Shader stehen im BuildProcessor — nur `SoF2/MapSurface`. Die anderen werden über Material-Referenzen in Szenen/Prefabs automatisch eingeschlossen, oder müssten bei Bedarf ergänzt werden.

## Vergleich zu SoF2 Original
- **Mehr als idTech3**: Fresnel, Depth-Fade, Sub-Surface, Soft Particles, HDR Emission — alles Extras
- **Ersetzt**: `tcMod scroll/rotate/turb` → direkt im Water-Shader; `tcGen environment` → Fresnel; `deformVertexes` → _WaveAmplitude; Multi-Pass-Blending → URP Single-Pass
- **Nicht implementiert (nicht nötig)**: `animMap` (animierte Texturen, kaum genutzt in SoF2 MP)

## Status: 100% Abdeckung ✅
Jede `Shader.Find("SoF2/...")` Referenz im Code hat eine passende .shader Datei.

# URP/Graphics Settings — RemakeSoF

## Aktuelle Pipeline
- **Render Pipeline**: URP (Universal Render Pipeline)
- **Pipeline Asset**: `Assets/Settings/PC_RPAsset.asset`
- **Renderer**: `Assets/Settings/PC_Renderer.asset` (Deferred Rendering)
- **Volume Profile**: `Assets/Settings/DefaultVolumeProfile.asset`
- **Quality Level**: 1 Level ("PC"), kein Mobile

## Bereits optimal konfiguriert
- Deferred Rendering (`m_RenderingMode: 2`)
- HDR aktiviert (`m_SupportsHDR: 1`)
- 4x MSAA (`m_MSAA: 4`)
- 4 Shadow Cascades (`m_ShadowCascadeCount: 4`)
- Soft Shadows High Quality (`m_SoftShadowQuality: 3`)
- Anisotropic Textures Force Enable (`anisotropicTextures: 2`)
- Textur-Mipmap volle Auflösung (`globalTextureMipmapLimit: 0`)
- Reflection Probe Blending + Box Projection
- SRP Batcher aktiv
- LOD Bias 2.0
- SSAO aktiv (als Renderer Feature)
- Light Layers + Light Cookies aktiviert

## Verbesserungspotential
- **Shadow Resolution**: Main/Additional Light Shadowmap = 2048 → 4096 möglich
- **QualitySettings shadowResolution**: 1 (Medium) → 3 (Very High)
- **Color Grading Mode**: 0 (LDR) → 1 (HDR) — HDR Pipeline aktiv aber Color Grading nutzt es nicht
- **Tonemapping**: Mode 0 (None) → 2 (ACES) oder 1 (Neutral)
- **SSAO**: Samples=1, BlurQuality=0 → höher für bessere AO
- **Bloom**: Intensity=0 (aus) → 0.2-0.5 mit highQualityFiltering=1
- **Realtime Reflection Probes**: aus → ein (Performance-Kosten beachten)
- **Pixel Light Count**: 2 → 4-8 (für Forward-Pass-Objekte)

## Wichtig: Settings vs. Assets
- Settings bestimmen WIE gerendert wird, nicht WAS
- ~80% der visuellen Qualität kommt von den Assets (Texturen, Materialien, Beleuchtung)
- SoF2-Texturen sind Diffuse-only (keine PBR Normal/Roughness Maps)
- Für modernen Look: PBR-Texturen + höhere Auflösungen nötig

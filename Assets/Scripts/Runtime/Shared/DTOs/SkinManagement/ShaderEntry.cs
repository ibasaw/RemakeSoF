namespace Tolik.RemakeSoF.Runtime.PlayerSkinManagement
{
    public class ShaderEntry
    {
        public string HitLocation { get; set; }
        public string HitMaterial { get; set; }
        public string EditorImage { get; set; }
        public bool CullDisabled { get; set; }
        public string MainTexture { get; set; }

        /// <summary>Alias-Shader-Name aus der g2shader-Datei (aliasShader Direktive).</summary>
        public string AliasShader { get; set; }
    }
}
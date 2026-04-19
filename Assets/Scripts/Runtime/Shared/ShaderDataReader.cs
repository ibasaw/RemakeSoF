using System;
using System.Collections.Generic;

using Tolik.RemakeSoF.Runtime.PlayerSkinManagement;

namespace Tolik.RemakeSoF.Runtime
{
    public static class ShaderDataReader
    {
        public static Dictionary<string, ShaderEntry> ParseShaderEntries(string content)
        {
            var entries = new Dictionary<string, ShaderEntry>();

            // Normalize line endings and split into lines
            var lines = content.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');

            string currentShader = null;
            ShaderEntry currentEntry = null;
            bool inShaderBlock = false;
            int braceDepth = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();

                // Skip empty lines and comments
                if (string.IsNullOrEmpty(line) || line.StartsWith("//"))
                    continue;

                // Check if this is a shader name (starts with models/ and is followed by { on next line)
                if (line.StartsWith("models/") && !line.EndsWith("{"))
                {
                    // Check if next line is just "{"
                    if (i + 1 < lines.Length && lines[i + 1].Trim() == "{")
                    {
                        // Save previous entry if exists
                        if (currentShader != null && currentEntry != null)
                        {
                            entries[currentShader] = currentEntry;
                        }

                        // Start new shader entry
                        currentShader = line.Trim();
                        currentEntry = new ShaderEntry();
                        inShaderBlock = true;
                        braceDepth = 1;
                        i++; // Skip the next line (the opening brace)
                        continue;
                    }
                }

                // Handle opening braces
                if (line == "{" && inShaderBlock)
                {
                    braceDepth++;
                    continue;
                }

                // Handle closing braces
                if (line == "}" && inShaderBlock)
                {
                    braceDepth--;

                    // If we're back to depth 0, we've closed the shader block
                    if (braceDepth == 0)
                    {
                        if (currentShader != null && currentEntry != null)
                        {
                            entries[currentShader] = currentEntry;
                        }
                        currentShader = null;
                        currentEntry = null;
                        inShaderBlock = false;
                    }
                    continue;
                }

                // Parse shader properties (at any level within the shader block)
                if (inShaderBlock && currentEntry != null)
                {
                    ParseShaderProperty(line, currentEntry);
                }
            }

            // Don't forget the last entry
            if (currentShader != null && currentEntry != null)
            {
                entries[currentShader] = currentEntry;
            }

            return entries;
        }

        private static void ParseShaderProperty(string line, ShaderEntry entry)
        {
            // Parse hitLocation
            if (line.StartsWith("hitLocation"))
            {
                var parts = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    entry.HitLocation = parts[1];
                }
            }

            // Parse hitMaterial
            if (line.StartsWith("hitMaterial"))
            {
                var parts = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    entry.HitMaterial = parts[1];
                }
            }

            // Parse qer_editorimage
            if (line.StartsWith("qer_editorimage"))
            {
                var parts = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    entry.EditorImage = parts[1];
                }
            }

            // Parse aliasShader
            if (line.StartsWith("aliasShader"))
            {
                var parts = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    entry.AliasShader = parts[1];
                }
            }

            // Parse cull disable
            if (line.Trim() == "cull\tdisable" || line.Trim() == "cull disable")
            {
                entry.CullDisabled = true;
            }

            // Parse map or clampmap (main texture) - this is inside nested blocks, so we need to handle it differently
            if (line.StartsWith("map") || line.StartsWith("clampmap"))
            {
                var parts = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    entry.MainTexture = parts[1];
                }
            }

            // Parse q3map_nolightmap and q3map_onlyvertexlighting (these are just flags)
            // These don't need special handling, they're just shader directives
        }
    }
}
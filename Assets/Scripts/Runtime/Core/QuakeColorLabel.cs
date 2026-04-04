using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tolik.RemakeSoF.Runtime.Core
{
    /// <summary>
    /// Custom VisualElement that renders Quake/SoF2 color-coded text using the bigchars atlas.
    /// Supports ^0-^9 and ^a-^z (case-insensitive) color escape sequences = 36 colors.
    /// Supports \XX hex escapes for atlas symbols (e.g. \01 = char 1, \FF = char 255).
    /// Each character is a child VisualElement showing a cropped sprite from the 16x16 atlas grid.
    /// Use ^^ to render a literal ^ character, \\ to render a literal backslash.
    /// </summary>
    [UxmlElement]
    internal partial class QuakeColorLabel : VisualElement
    {
        /// <summary>Atlas grid dimensions (16x16 = 256 ASCII characters).</summary>
        const int k_AtlasColumns = 16;
        const int k_AtlasRows = 16;

        /// <summary>SoF2/Quake III color escape character.</summary>
        const char k_ColorEscape = '^';

        /// <summary>
        /// Extended Quake color table: ^0-^9 + ^a-^z (case-insensitive) = 36 colors.
        /// Lookup by lowercase char key.
        /// </summary>
        static readonly Dictionary<char, Color> s_ColorMap = new()
        {
            // ^0-^9: Original Quake III + Extended
            { '0', new(0f, 0f, 0f, 1f) },            // Black
            { '1', new(1f, 0.2f, 0.2f, 1f) },        // Red
            { '2', new(0.2f, 1f, 0.2f, 1f) },        // Green
            { '3', new(1f, 1f, 0.2f, 1f) },          // Yellow
            { '4', new(0.3f, 0.3f, 1f, 1f) },        // Blue
            { '5', new(0.2f, 1f, 1f, 1f) },          // Cyan
            { '6', new(1f, 0.2f, 1f, 1f) },          // Magenta
            { '7', new(1f, 1f, 1f, 1f) },            // White
            { '8', new(1f, 0.5f, 0f, 1f) },          // Orange
            { '9', new(0.5f, 0.5f, 0.5f, 1f) },     // Grey
            // ^a-^z: Extended palette
            { 'a', new(1f, 0.5f, 0.5f, 1f) },       // Salmon
            { 'b', new(0.5f, 1f, 0.5f, 1f) },       // Light Green
            { 'c', new(0.5f, 0.5f, 1f, 1f) },       // Light Blue
            { 'd', new(0.6f, 0.6f, 0f, 1f) },       // Olive
            { 'e', new(1f, 0.4f, 0.7f, 1f) },       // Pink
            { 'f', new(0.6f, 0.2f, 0.8f, 1f) },     // Purple
            { 'g', new(0f, 0.5f, 0f, 1f) },          // Dark Green
            { 'h', new(0.6f, 0.1f, 0.1f, 1f) },     // Maroon
            { 'i', new(0.6f, 0.4f, 0.2f, 1f) },     // Brown
            { 'j', new(1f, 0.84f, 0f, 1f) },         // Gold
            { 'k', new(0.3f, 0.3f, 0.3f, 1f) },     // Dark Grey
            { 'l', new(0.8f, 0.8f, 0.8f, 1f) },     // Light Grey
            { 'm', new(0.6f, 0.6f, 0.6f, 1f) },     // Medium Grey
            { 'n', new(0f, 0.5f, 0.5f, 1f) },        // Teal
            { 'o', new(0.5f, 0.6f, 0.2f, 1f) },     // Olive Green
            { 'p', new(1f, 0.8f, 0.6f, 1f) },       // Peach
            { 'q', new(1f, 0.4f, 0.4f, 1f) },       // Rose
            { 'r', new(0.1f, 0.1f, 0.5f, 1f) },     // Navy
            { 's', new(0.75f, 0.75f, 0.75f, 1f) },   // Silver
            { 't', new(0.82f, 0.71f, 0.55f, 1f) },   // Tan
            { 'u', new(0.3f, 0f, 0.5f, 1f) },        // Indigo
            { 'v', new(0.56f, 0f, 1f, 1f) },          // Violet
            { 'w', new(0.96f, 0.87f, 0.7f, 1f) },    // Wheat
            { 'x', new(0f, 0.4f, 0.4f, 1f) },        // Dark Cyan
            { 'y', new(0.6f, 1f, 0.2f, 1f) },        // Lime
            { 'z', new(1f, 0.5f, 0.31f, 1f) },       // Coral
        };

        /// <summary>Default color (white, same as ^7).</summary>
        static readonly Color s_DefaultColor = new(1f, 1f, 1f, 1f);

        /// <summary>Cursor blink interval in milliseconds.</summary>
        const long k_CursorBlinkMs = 530;

        Texture2D m_Atlas;
        string m_Text = "";
        float m_CharWidth = 24f;
        float m_CharHeight = 24f;
        bool m_ShowCursor;
        VisualElement m_CursorElement;
        IVisualElementScheduledItem m_CursorBlink;
        bool m_CursorVisible;

        /// <summary>Bigchars atlas texture (16x16 grid of ASCII glyphs, white on transparent).</summary>
        public Texture2D Atlas
        {
            get => m_Atlas;
            set
            {
                m_Atlas = value;
                Rebuild();
            }
        }

        /// <summary>Raw text with ^X color codes (e.g. "^1[EU] ^3Warzone ^7#1").</summary>
        public string Text
        {
            get => m_Text;
            set
            {
                m_Text = value ?? "";
                Rebuild();
            }
        }

        /// <summary>Shows a blinking cursor at the end of the text (for input overlay).</summary>
        public bool ShowCursor
        {
            get => m_ShowCursor;
            set
            {
                m_ShowCursor = value;
                Rebuild();
            }
        }

        /// <summary>Rendered width of each character in pixels.</summary>
        public float CharWidth
        {
            get => m_CharWidth;
            set
            {
                m_CharWidth = value;
                Rebuild();
            }
        }

        /// <summary>Rendered height of each character in pixels.</summary>
        public float CharHeight
        {
            get => m_CharHeight;
            set
            {
                m_CharHeight = value;
                Rebuild();
            }
        }

        /// <summary>
        /// Initializes the label with horizontal layout for character elements.
        /// </summary>
        public QuakeColorLabel()
        {
            style.flexDirection = FlexDirection.Row;
            style.overflow = Overflow.Hidden;
            style.alignItems = Align.Center;
        }

        /// <summary>
        /// Rebuilds all child character elements from the current text and atlas.
        /// Each visible character becomes a VisualElement with the atlas as background,
        /// positioned via background-position to show the correct glyph, tinted with the active color.
        /// </summary>
        void Rebuild()
        {
            Clear();
            m_CursorElement = null;

            if (m_Atlas == null || string.IsNullOrEmpty(m_Text))
            {
                if (m_ShowCursor && m_Atlas != null)
                {
                    AppendCursor();
                }

                return;
            }

            // background-size: scale atlas so one cell = m_CharWidth x m_CharHeight
            float bgW = m_CharWidth * k_AtlasColumns;
            float bgH = m_CharHeight * k_AtlasRows;

            Color currentColor = s_DefaultColor;

            for (int i = 0; i < m_Text.Length; i++)
            {
                // ^^ = literal ^ character
                if (i + 1 < m_Text.Length && m_Text[i] == k_ColorEscape && m_Text[i + 1] == k_ColorEscape)
                {
                    i++;
                    // fall through to render '^' as glyph
                }
                else if (IsColorCode(m_Text, i))
                {
                    char key = char.ToLowerInvariant(m_Text[i + 1]);
                    currentColor = s_ColorMap[key];
                    i++;
                    continue;
                }
                // \\ = literal backslash
                else if (i + 1 < m_Text.Length && m_Text[i] == '\\' && m_Text[i + 1] == '\\')
                {
                    i++;
                    // fall through to render '\' as glyph
                }
                // \XX = hex escape for bigchars atlas symbol (00-FF)
                else if (TryParseHexEscape(m_Text, i, out int hexChar))
                {
                    AddGlyph(hexChar, currentColor, bgW, bgH);
                    i += 2; // skip the two hex digits
                    continue;
                }

                char c = m_Text[i];
                int asciiCode = c & 0xFF;
                AddGlyph(asciiCode, currentColor, bgW, bgH);
            }

            if (m_ShowCursor)
            {
                AppendCursor();
            }
        }

        /// <summary>
        /// Adds a single atlas glyph VisualElement for the given character code.
        /// </summary>
        void AddGlyph(int asciiCode, Color color, float bgW, float bgH)
        {
            int col = asciiCode % k_AtlasColumns;
            int row = asciiCode / k_AtlasColumns;

            VisualElement glyph = new();
            glyph.style.width = m_CharWidth;
            glyph.style.height = m_CharHeight;
            glyph.style.marginBottom = 0;
            glyph.style.paddingBottom = 0;
            glyph.style.flexShrink = 0;
            glyph.style.backgroundImage = new StyleBackground(m_Atlas);
            glyph.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
            glyph.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Left, new Length(-col * m_CharWidth, LengthUnit.Pixel));
            glyph.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Top, new Length(-row * m_CharHeight, LengthUnit.Pixel));
            glyph.style.backgroundSize = new BackgroundSize(new Length(bgW, LengthUnit.Pixel), new Length(bgH, LengthUnit.Pixel));
            glyph.style.unityBackgroundImageTintColor = new StyleColor(color);
            Add(glyph);
        }

        /// <summary>
        /// Appends a blinking cursor element at the end of the label.
        /// </summary>
        void AppendCursor()
        {
            m_CursorElement = new VisualElement();
            m_CursorElement.style.width = 2;
            m_CursorElement.style.height = m_CharHeight;
            m_CursorElement.style.flexShrink = 0;
            m_CursorElement.style.backgroundColor = new StyleColor(s_DefaultColor);
            m_CursorElement.style.marginLeft = 1;
            Add(m_CursorElement);

            m_CursorVisible = true;
            m_CursorBlink?.Pause();
            m_CursorBlink = schedule.Execute(() =>
            {
                if (m_CursorElement == null) return;
                m_CursorVisible = !m_CursorVisible;
                m_CursorElement.style.opacity = m_CursorVisible ? 1f : 0f;
            }).Every(k_CursorBlinkMs);
        }

        /// <summary>
        /// Tries to parse a \XX hex escape at the given index.
        /// Returns true if text[index] == '\' and text[index+1..index+2] are valid hex digits.
        /// </summary>
        static bool TryParseHexEscape(string text, int index, out int charCode)
        {
            charCode = 0;
            if (index + 2 >= text.Length || text[index] != '\\')
            {
                return false;
            }

            char h1 = text[index + 1];
            char h2 = text[index + 2];
            if (!IsHexDigit(h1) || !IsHexDigit(h2))
            {
                return false;
            }

            charCode = (HexVal(h1) << 4) | HexVal(h2);
            return true;
        }

        /// <summary>Returns true if the character is a valid hexadecimal digit (0-9, a-f, A-F).</summary>
        static bool IsHexDigit(char c)
        {
            return (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
        }

        /// <summary>Converts a single hex character to its integer value.</summary>
        static int HexVal(char c)
        {
            if (c >= '0' && c <= '9') return c - '0';
            if (c >= 'a' && c <= 'f') return c - 'a' + 10;
            return c - 'A' + 10;
        }

        /// <summary>
        /// Checks if the character at the given index is a Quake color escape sequence
        /// (^ followed by 0-9 or a-z/A-Z that exists in the color map).
        /// </summary>
        static bool IsColorCode(string text, int index)
        {
            if (index + 1 >= text.Length || text[index] != k_ColorEscape)
            {
                return false;
            }

            char next = char.ToLowerInvariant(text[index + 1]);
            return s_ColorMap.ContainsKey(next);
        }

        /// <summary>
        /// Strips all ^X color codes and resolves \XX hex escapes from a Quake-formatted string.
        /// Handles ^^ as literal ^ and \\ as literal \. Returns only the visible text.
        /// </summary>
        public static string StripColorCodes(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            System.Text.StringBuilder sb = new(text.Length);
            for (int i = 0; i < text.Length; i++)
            {
                if (i + 1 < text.Length && text[i] == k_ColorEscape && text[i + 1] == k_ColorEscape)
                {
                    sb.Append(k_ColorEscape);
                    i++;
                    continue;
                }

                if (IsColorCode(text, i))
                {
                    i++;
                    continue;
                }

                if (i + 1 < text.Length && text[i] == '\\' && text[i + 1] == '\\')
                {
                    sb.Append('\\');
                    i++;
                    continue;
                }

                if (TryParseHexEscape(text, i, out int hexChar))
                {
                    sb.Append((char)hexChar);
                    i += 2;
                    continue;
                }

                sb.Append(text[i]);
            }

            return sb.ToString();
        }
    }
}

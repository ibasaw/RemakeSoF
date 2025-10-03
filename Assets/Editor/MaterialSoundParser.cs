using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// Place this file in an Editor folder (e.g. Assets/Editor/MaterialFileParser.cs)
// Usage: Window -> Material Parser (or Tools -> Material Parser) -> select file -> Parse -> Save JSON

//Generischer generic.material Parser für die Sounds aus SoF2
//Erstellt json aus einem generic.material file
public class MaterialFileParserWindow : EditorWindow
{
    private TextAsset materialAsset;
    private string filePath = "";
    private string jsonOutput = "";
    private Vector2 scrollPos;

    [MenuItem("Tools/Material Parser")]
    public static void ShowWindow()
    {
        var w = GetWindow<MaterialFileParserWindow>("Material Parser");
        w.minSize = new Vector2(600, 400);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Material (.material) to JSON", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        materialAsset = (TextAsset)EditorGUILayout.ObjectField("TextAsset (optional)", materialAsset, typeof(TextAsset), false);
        if (GUILayout.Button("Browse file", GUILayout.Width(120)))
        {
            var p = EditorUtility.OpenFilePanel("Open material file", Application.dataPath, "material");
            if (!string.IsNullOrEmpty(p))
            {
                filePath = p;
                materialAsset = null; // use filePath instead
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Or paste file path:", GUILayout.Width(110));
        filePath = EditorGUILayout.TextField(filePath);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        if (GUILayout.Button("Parse to JSON", GUILayout.Height(30)))
        {
            string content = null;
            if (materialAsset != null)
            {
                content = materialAsset.text;
            }
            else if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                content = File.ReadAllText(filePath);
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Please provide a valid TextAsset or file path.", "OK");
            }

            if (!string.IsNullOrEmpty(content))
            {
                try
                {
                    var parser = new MaterialParser(content);
                    var doc = parser.Parse();
                    jsonOutput = JsonUtil.ToJson(doc, true);
                }
                catch (Exception ex)
                {
                    jsonOutput = "// Parse error:\n" + ex.ToString();
                }
            }
        }

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Save JSON to file", GUILayout.Height(24)))
        {
            if (string.IsNullOrEmpty(jsonOutput))
            {
                EditorUtility.DisplayDialog("Nothing to save", "Please parse a file first.", "OK");
            }
            else
            {
                var p = EditorUtility.SaveFilePanel("Save JSON", Application.dataPath, "materials.json", "json");
                if (!string.IsNullOrEmpty(p))
                {
                    File.WriteAllText(p, jsonOutput, Encoding.UTF8);
                    AssetDatabase.Refresh();
                    EditorUtility.RevealInFinder(p);
                }
            }
        }

        if (GUILayout.Button("Copy JSON to Clipboard", GUILayout.Height(24)))
        {
            if (!string.IsNullOrEmpty(jsonOutput))
            {
                EditorGUIUtility.systemCopyBuffer = jsonOutput;
                EditorUtility.DisplayDialog("Copied", "JSON copied to clipboard.", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Nothing to copy", "Please parse a file first.", "OK");
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("JSON preview:", EditorStyles.boldLabel);
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        EditorGUILayout.TextArea(jsonOutput, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }
}

// ---------- Parser implementation ----------

internal enum TokenType { Identifier, String, Number, LBrace, RBrace, EOF }

internal class Token
{
    public TokenType Type;
    public string Text;
    public Token(TokenType t, string text) { Type = t; Text = text; }
    public override string ToString() => $"{Type}: {Text}";
}

internal class MaterialScanner
{
    private readonly string src;
    private int idx;
    private int len;

    public MaterialScanner(string source)
    {
        src = source ?? string.Empty;
        idx = 0;
        len = src.Length;
    }

    private char Peek() => idx < len ? src[idx] : '\0';
    private char Read() => idx < len ? src[idx++] : '\0';

    private void SkipWhitespace()
    {
        while (char.IsWhiteSpace(Peek())) Read();
        // simple comment support: // to end of line
        if (Peek() == '/' && idx + 1 < len && src[idx + 1] == '/')
        {
            // skip to end of line
            Read(); Read();
            while (Peek() != '\n' && Peek() != '\0') Read();
            SkipWhitespace();
        }
    }

    public Token NextToken()
    {
        SkipWhitespace();
        char c = Peek();
        if (c == '\0') return new Token(TokenType.EOF, "");
        if (c == '{') { Read(); return new Token(TokenType.LBrace, "{"); }
        if (c == '}') { Read(); return new Token(TokenType.RBrace, "}"); }
        if (c == '"')
        {
            Read();
            var sb = new StringBuilder();
            while (Peek() != '"' && Peek() != '\0')
            {
                char ch = Read();
                if (ch == '\\' && Peek() != '\0') // escape sequences
                {
                    char esc = Read();
                    if (esc == 'n') sb.Append('\n');
                    else if (esc == 't') sb.Append('\t');
                    else sb.Append(esc);
                }
                else sb.Append(ch);
            }
            if (Peek() == '"') Read();
            return new Token(TokenType.String, sb.ToString());
        }

        // Improved numeric vs identifier handling:
        // If token starts with a digit but contains letters (e.g. "9mm" or "12.7mm"), treat it as an Identifier.
        // If token is purely numeric (optionally with a decimal point), it's a Number.
        if (char.IsDigit(c) || (c == '-' && idx + 1 < len && char.IsDigit(src[idx + 1])))
        {
            var sb = new StringBuilder();
            if (c == '-') sb.Append(Read());

            // read a sequence of letters, digits or dots (to capture things like 12.7mm)
            while (true)
            {
                char p = Peek();
                if (p == '\0' || char.IsWhiteSpace(p) || p == '{' || p == '}' || p == '"') break;
                // allow letters, digits, dot, and optionally other chars like % or _ if needed
                if (char.IsLetterOrDigit(p) || p == '.') sb.Append(Read());
                else break;
            }

            string token = sb.ToString();
            bool hasLetter = false;
            foreach (char ch in token) if (char.IsLetter(ch)) { hasLetter = true; break; }

            if (hasLetter)
            {
                return new Token(TokenType.Identifier, token);
            }

            // otherwise it's a pure number (maybe with dot)
            return new Token(TokenType.Number, token);
        }

        // identifier (allow many characters until whitespace or brace)
        {
            var sb = new StringBuilder();
            while (true)
            {
                char p = Peek();
                if (p == '\0' || char.IsWhiteSpace(p) || p == '{' || p == '}' || p == '"') break;
                sb.Append(Read());
            }
            return new Token(TokenType.Identifier, sb.ToString());
        }
    }
}

internal class MaterialParser
{
    private MaterialScanner scanner;
    private Token lookahead;

    public MaterialParser(string text)
    {
        scanner = new MaterialScanner(text);
        lookahead = scanner.NextToken();
    }

    private void Consume() { lookahead = scanner.NextToken(); }
    private bool Match(TokenType t) => lookahead.Type == t;

    public Dictionary<string, object> Parse()
    {
        var root = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        while (!Match(TokenType.EOF))
        {
            if (Match(TokenType.Identifier) || Match(TokenType.String))
            {
                string key = lookahead.Text; Consume();
                if (Match(TokenType.LBrace))
                {
                    Consume();
                    var obj = ParseBlock();
                    AddToDict(root, key, obj);
                }
                else if (Match(TokenType.Identifier) || Match(TokenType.String) || Match(TokenType.Number))
                {
                    // key value pair
                    object val = ParseValue();
                    AddToDict(root, key, val);
                }
                else
                {
                    // unexpected token, try continue
                    Consume();
                }
            }
            else if (Match(TokenType.LBrace))
            {
                // anonymous block? consume
                Consume();
                // ignore
                var _ = ParseBlock();
            }
            else
            {
                Consume();
            }
        }
        return root;
    }

    private Dictionary<string, object> ParseBlock()
    {
        var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        while (!Match(TokenType.RBrace) && !Match(TokenType.EOF))
        {
            if (Match(TokenType.Identifier) || Match(TokenType.String) || Match(TokenType.Number))
            {
                string key = lookahead.Text; Consume();
                if (Match(TokenType.LBrace))
                {
                    Consume();
                    var obj = ParseBlock();
                    AddToDict(dict, key, obj);
                }
                else if (Match(TokenType.Identifier) || Match(TokenType.String) || Match(TokenType.Number))
                {
                    var val = ParseValue();
                    AddToDict(dict, key, val);
                }
                else
                {
                    // sometimes keys are followed by newline-only; treat as empty string
                    AddToDict(dict, key, "");
                }
            }
            else
            {
                // skip tokens until next meaningful
                Consume();
            }
        }
        if (Match(TokenType.RBrace)) Consume();
        return dict;
    }

    private object ParseValue()
    {
        if (Match(TokenType.Number))
        {
            var t = lookahead.Text; Consume();
            if (double.TryParse(t, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double d))
                return d;
            return t;
        }
        else if (Match(TokenType.String) || Match(TokenType.Identifier))
        {
            var t = lookahead.Text; Consume();
            return t;
        }
        else
        {
            var t = lookahead.Text; Consume();
            return t;
        }
    }

    private void AddToDict(Dictionary<string, object> dict, string key, object val)
    {
        if (dict.TryGetValue(key, out object existing))
        {
            if (existing is List<object> list)
            {
                list.Add(val);
            }
            else
            {
                var newList = new List<object> { existing, val };
                dict[key] = newList;
            }
        }
        else
        {
            dict[key] = val;
        }
    }
}

// ---------- Simple JSON serializer (no external deps) ----------
internal static class JsonUtil
{
    public static string ToJson(object obj, bool pretty = false)
    {
        var sb = new StringBuilder();
        var writer = new JsonWriter(sb, pretty);
        writer.WriteValue(obj);
        return sb.ToString();
    }

    private class JsonWriter
    {
        private readonly StringBuilder sb;
        private readonly bool pretty;
        private int indent = 0;
        private bool newLine = false;
        public JsonWriter(StringBuilder sb, bool pretty)
        {
            this.sb = sb; this.pretty = pretty;
        }
        private void Indent() { if (pretty) { sb.Append('\n'); sb.Append(new string(' ', indent * 2)); } }
        private void WriteEscaped(string s)
        {
            sb.Append('"');
            foreach (var ch in s)
            {
                switch (ch)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default: sb.Append(ch); break;
                }
            }
            sb.Append('"');
        }

        public void WriteValue(object value)
        {
            if (value == null) { sb.Append("null"); return; }
            if (value is Dictionary<string, object> dict)
            {
                WriteObject(dict);
                return;
            }
            if (value is List<object> list)
            {
                WriteArray(list);
                return;
            }
            if (value is string s)
            {
                WriteEscaped(s);
                return;
            }
            if (value is double || value is float || value is int || value is long || value is decimal)
            {
                if (value is double d)
                    sb.Append(d.ToString(System.Globalization.CultureInfo.InvariantCulture));
                else
                    sb.Append(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture));
                return;
            }
            // fallback: try dictionary-like
            if (value is IDictionary<string, object> idict)
            {
                WriteObject(new Dictionary<string, object>(idict));
                return;
            }
            // unknown type -> string
            WriteEscaped(value.ToString());
        }

        private void WriteObject(Dictionary<string, object> dict)
        {
            sb.Append('{');
            indent++;
            bool first = true;
            foreach (var kv in dict)
            {
                if (!first) sb.Append(',');
                Indent();
                WriteEscaped(kv.Key);
                sb.Append(pretty ? ": " : ":");
                WriteValue(NormalizeValue(kv.Value));
                first = false;
            }
            indent--;
            if (!first) Indent();
            sb.Append('}');
        }

        private void WriteArray(List<object> list)
        {
            sb.Append('[');
            indent++;
            bool first = true;
            foreach (var it in list)
            {
                if (!first) sb.Append(',');
                Indent();
                WriteValue(NormalizeValue(it));
                first = false;
            }
            indent--;
            if (!first) Indent();
            sb.Append(']');
        }

        // If dicts inside lists are typed as Dictionary<string, object> it's fine. If we encounter Int64 from parsing, ensure it's double or int.
        private object NormalizeValue(object v)
        {
            if (v is Dictionary<string, object> d) return d;
            if (v is List<object> l) return l;
            if (v is double) return v;
            if (v is float) return Convert.ToDouble(v);
            if (v is int) return v;
            if (v is long) return Convert.ToDouble(v);
            if (v is string) return v;
            return v?.ToString();
        }
    }
}

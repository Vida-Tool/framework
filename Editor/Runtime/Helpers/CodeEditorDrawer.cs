using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using Vida.Framework.CodeEditor;
using Vida.Framework.Editor;

namespace Vida.Framework
{
    public static class CodeEditorDrawer
    {
        private const float CardPadding = 12f;
        private const float HeaderHeight = 34f;
        private const float CodePadding = 10f;
        private const float FallbackLineHeight = 16f;
        private const float MinCardWidth = 360f;
        private const string KeywordColor = "#CC7832";
        private const string TypeColor = "#A9B7C6";
        private const string StringColor = "#6A8759";
        private const string NumberColor = "#6897BB";
        private const string CommentColor = "#57A64A";
        private const string OperatorColor = "#A9B7C6";
        private const string AttributeColor = "#BBB529";

        private static readonly Color CodeBackgroundColor = new Color32(0x12, 0x17, 0x22, 0xFF);
        private static readonly Color GutterBackgroundColor = new Color32(0x0D, 0x12, 0x1B, 0xFF);
        private static readonly Color BorderColor = new Color32(0x2A, 0x36, 0x48, 0xFF);
        private static readonly Color AccentColor = new Color32(0x4C, 0xC6, 0xFF, 0xFF);
        private static readonly Color HeaderTextColor = new Color32(0xF0, 0xF5, 0xFF, 0xFF);
        private static readonly Color CodeTextColor = new Color32(0xD4, 0xD4, 0xD4, 0xFF);
        private static readonly Color MutedTextColor = new Color32(0x7D, 0x8A, 0x9D, 0xFF);

        private static readonly HashSet<string> Keywords = new HashSet<string>
        {
            "abstract", "as", "base", "break", "case", "catch", "checked", "class", "const", "continue",
            "default", "delegate", "do", "else", "enum", "event", "explicit", "extern", "finally", "fixed",
            "for", "foreach", "goto", "if", "implicit", "in", "interface", "internal", "is", "lock",
            "namespace", "new", "operator", "out", "override", "params", "private", "protected", "public",
            "readonly", "ref", "return", "sealed", "sizeof", "stackalloc", "static", "struct", "switch",
            "this", "throw", "try", "typeof", "unchecked", "unsafe", "using", "virtual", "void", "volatile",
            "while", "async", "await", "get", "set", "value", "var", "yield"
        };

        private static readonly HashSet<string> Types = new HashSet<string>
        {
            "bool", "byte", "char", "decimal", "double", "float", "int", "long", "object", "sbyte",
            "short", "string", "uint", "ulong", "ushort", "Color", "Color32", "GameObject", "Transform",
            "Vector2", "Vector3", "Vector4", "Quaternion", "Rect", "List", "Dictionary", "HashSet", "Action",
            "Func", "Task", "MonoBehaviour", "ScriptableObject", "EditorWindow", "GUILayout", "GUI", "GUIStyle"
        };

        private static GUIStyle _headerStyle;
        private static GUIStyle _codeLineStyle;
        private static GUIStyle _selectableCodeStyle;
        private static GUIStyle _lineNumberStyle;
        private static GUIStyle _copyButtonStyle;
        private static GUIStyle _chipStyle;

        public static void Reset()
        {
            _headerStyle = null;
            _codeLineStyle = null;
            _selectableCodeStyle = null;
            _lineNumberStyle = null;
            _copyButtonStyle = null;
            _chipStyle = null;
        }

        public static void DrawCodeLine(CodeData data, float width)
        {
            TryInit();

            string[] lines = GetCodeLines(data.data);
            float cardWidth = Mathf.Max(MinCardWidth, width);
            float lineHeight = GetCodeLineHeight();
            float lineNumberWidth = GetLineNumberWidth(lines.Length);
            float codeHeight = Mathf.Max(48f, lines.Length * lineHeight + CodePadding * 2f);
            float cardHeight = HeaderHeight + codeHeight + CardPadding * 2f;

            Rect cardRect = GUILayoutUtility.GetRect(cardWidth, cardHeight, GUILayout.Width(cardWidth), GUILayout.Height(cardHeight));
            VidaPremiumGUI.DrawFrame(cardRect, "frame-panel.png");
            DrawPremiumAccent(cardRect);

            Rect headerRect = new Rect(cardRect.x + CardPadding, cardRect.y + CardPadding, cardRect.width - CardPadding * 2f, HeaderHeight);
            DrawHeader(data, headerRect);

            Rect codeRect = new Rect(headerRect.x, headerRect.yMax, headerRect.width, codeHeight);
            DrawCodeBackground(codeRect, lineNumberWidth);
            DrawLines(lines, codeRect, lineNumberWidth, lineHeight);
            DrawSelectableCodeOverlay(data.data, codeRect, lineNumberWidth);
        }

        private static void TryInit()
        {
            if (_headerStyle != null)
            {
                return;
            }

            Font codeFont = Font.CreateDynamicFontFromOSFont(new[] { "JetBrains Mono", "Menlo", "Monaco", "Consolas" }, 12);

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                fontSize = 13
            };
            _headerStyle.normal.textColor = HeaderTextColor;

            _codeLineStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                font = codeFont,
                fontSize = 12,
                richText = true,
                wordWrap = false
            };
            _codeLineStyle.normal.textColor = CodeTextColor;

            _selectableCodeStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.UpperLeft,
                clipping = TextClipping.Clip,
                font = codeFont,
                fontSize = 12,
                richText = false,
                wordWrap = false,
                padding = new RectOffset(0, 0, 0, 0)
            };
            _selectableCodeStyle.normal.textColor = new Color(1f, 1f, 1f, 0f);
            _selectableCodeStyle.active.textColor = new Color(1f, 1f, 1f, 0f);
            _selectableCodeStyle.focused.textColor = new Color(1f, 1f, 1f, 0f);

            _lineNumberStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight,
                clipping = TextClipping.Clip,
                font = codeFont,
                fontSize = 11
            };
            _lineNumberStyle.normal.textColor = MutedTextColor;

            _copyButtonStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Clip
            };
            _copyButtonStyle.normal.textColor = HeaderTextColor;

            _chipStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Clip
            };
            _chipStyle.normal.textColor = MutedTextColor;
        }

        private static void DrawHeader(CodeData data, Rect headerRect)
        {
            Rect titleRect = new Rect(headerRect.x, headerRect.y, headerRect.width - 124f, headerRect.height);
            GUI.Label(titleRect, data.header, _headerStyle);

            string extension = GetExtensionLabel(data.fileName);
            if (!string.IsNullOrEmpty(extension))
            {
                Rect chipRect = new Rect(headerRect.xMax - 116f, headerRect.y + 4f, 48f, 24f);
                VidaPremiumGUI.DrawFrame(chipRect, "frame-chip.png");
                GUI.Label(chipRect, extension, _chipStyle);
            }

            Rect copyRect = new Rect(headerRect.xMax - 62f, headerRect.y + 2f, 62f, 28f);
            VidaPremiumGUI.DrawFrame(copyRect, "frame-button-secondary.png");
            if (GUI.Button(copyRect, GUIContent.none, GUIStyle.none))
            {
                EditorGUIUtility.systemCopyBuffer = data.data;
            }

            GUI.Label(copyRect, "Copy", _copyButtonStyle);
        }

        private static void DrawPremiumAccent(Rect cardRect)
        {
            EditorGUI.DrawRect(new Rect(cardRect.x + 2f, cardRect.y + 2f, cardRect.width - 4f, 1f), AccentColor);
        }

        private static void DrawCodeBackground(Rect codeRect, float lineNumberWidth)
        {
            EditorGUI.DrawRect(codeRect, CodeBackgroundColor);
            EditorGUI.DrawRect(new Rect(codeRect.x, codeRect.y, lineNumberWidth, codeRect.height), GutterBackgroundColor);
            EditorGUI.DrawRect(new Rect(codeRect.x + lineNumberWidth, codeRect.y, 1f, codeRect.height), BorderColor);
            EditorGUI.DrawRect(new Rect(codeRect.x, codeRect.y, codeRect.width, 1f), BorderColor);
            EditorGUI.DrawRect(new Rect(codeRect.x, codeRect.yMax - 1f, codeRect.width, 1f), BorderColor);
        }

        private static void DrawLines(string[] lines, Rect codeRect, float lineNumberWidth, float lineHeight)
        {
            bool isBlockComment = false;
            float y = codeRect.y + CodePadding;
            for (int i = 0; i < lines.Length; i++)
            {
                Rect numberRect = new Rect(codeRect.x + 4f, y, lineNumberWidth - 10f, lineHeight);
                Rect lineRect = new Rect(codeRect.x + lineNumberWidth + 10f, y, codeRect.width - lineNumberWidth - 18f, lineHeight);

                GUI.Label(numberRect, (i + 1).ToString(), _lineNumberStyle);
                GUI.Label(lineRect, GetHighlightedLine(lines[i], ref isBlockComment), _codeLineStyle);
                y += lineHeight;
            }
        }

        private static void DrawSelectableCodeOverlay(string code, Rect codeRect, float lineNumberWidth)
        {
            Rect selectableRect = new Rect(
                codeRect.x + lineNumberWidth + 10f,
                codeRect.y + CodePadding,
                codeRect.width - lineNumberWidth - 18f,
                codeRect.height - CodePadding * 2f);

            EditorGUI.SelectableLabel(selectableRect, NormalizeCode(code), _selectableCodeStyle);
        }

        private static string GetHighlightedLine(string line, ref bool isBlockComment)
        {
            StringBuilder builder = new StringBuilder(line.Length * 2);
            int index = 0;

            while (index < line.Length)
            {
                if (isBlockComment)
                {
                    int blockEndIndex = line.IndexOf("*/", index, System.StringComparison.Ordinal);
                    if (blockEndIndex < 0)
                    {
                        AppendColored(builder, line.Substring(index), CommentColor);
                        return builder.ToString();
                    }

                    AppendColored(builder, line.Substring(index, blockEndIndex - index + 2), CommentColor);
                    index = blockEndIndex + 2;
                    isBlockComment = false;
                    continue;
                }

                if (StartsWith(line, index, "//"))
                {
                    AppendColored(builder, line.Substring(index), CommentColor);
                    break;
                }

                if (StartsWith(line, index, "/*"))
                {
                    int blockEndIndex = line.IndexOf("*/", index + 2, System.StringComparison.Ordinal);
                    if (blockEndIndex < 0)
                    {
                        AppendColored(builder, line.Substring(index), CommentColor);
                        isBlockComment = true;
                        break;
                    }

                    AppendColored(builder, line.Substring(index, blockEndIndex - index + 2), CommentColor);
                    index = blockEndIndex + 2;
                    continue;
                }

                char character = line[index];
                if (character == '[' && index + 1 < line.Length && IsIdentifierStart(line[index + 1]))
                {
                    int attributeEndIndex = GetAttributeEndIndex(line, index);
                    if (attributeEndIndex > index)
                    {
                        AppendColored(builder, line.Substring(index, attributeEndIndex - index + 1), AttributeColor);
                        index = attributeEndIndex + 1;
                        continue;
                    }
                }

                if (character == '"' || character == '\'')
                {
                    int stringEndIndex = GetStringEndIndex(line, index, character);
                    AppendColored(builder, line.Substring(index, stringEndIndex - index + 1), StringColor);
                    index = stringEndIndex + 1;
                    continue;
                }

                if (IsIdentifierStart(character))
                {
                    int endIndex = index + 1;
                    while (endIndex < line.Length && IsIdentifierPart(line[endIndex]))
                    {
                        endIndex++;
                    }

                    string token = line.Substring(index, endIndex - index);
                    AppendIdentifier(builder, token);
                    index = endIndex;
                    continue;
                }

                if (char.IsDigit(character))
                {
                    int endIndex = index + 1;
                    while (endIndex < line.Length && IsNumberPart(line[endIndex]))
                    {
                        endIndex++;
                    }

                    AppendColored(builder, line.Substring(index, endIndex - index), NumberColor);
                    index = endIndex;
                    continue;
                }

                if (IsOperatorCharacter(character))
                {
                    AppendColored(builder, character.ToString(), OperatorColor);
                    index++;
                    continue;
                }

                AppendEscaped(builder, character);
                index++;
            }

            return builder.ToString();
        }

        private static void AppendIdentifier(StringBuilder builder, string token)
        {
            if (Keywords.Contains(token))
            {
                AppendColored(builder, token, KeywordColor);
                return;
            }

            if (Types.Contains(token))
            {
                AppendColored(builder, token, TypeColor);
                return;
            }

            AppendEscaped(builder, token);
        }

        private static void AppendColored(StringBuilder builder, string text, string color)
        {
            builder.Append("<color=");
            builder.Append(color);
            builder.Append(">");
            AppendEscaped(builder, text);
            builder.Append("</color>");
        }

        private static void AppendEscaped(StringBuilder builder, string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                AppendEscaped(builder, text[i]);
            }
        }

        private static void AppendEscaped(StringBuilder builder, char character)
        {
            switch (character)
            {
                case '<':
                    builder.Append("&lt;");
                    break;
                case '>':
                    builder.Append("&gt;");
                    break;
                case '&':
                    builder.Append("&amp;");
                    break;
                default:
                    builder.Append(character);
                    break;
            }
        }

        private static int GetStringEndIndex(string line, int startIndex, char quote)
        {
            bool isEscaped = false;
            for (int i = startIndex + 1; i < line.Length; i++)
            {
                if (isEscaped)
                {
                    isEscaped = false;
                    continue;
                }

                if (line[i] == '\\')
                {
                    isEscaped = true;
                    continue;
                }

                if (line[i] == quote)
                {
                    return i;
                }
            }

            return line.Length - 1;
        }

        private static int GetAttributeEndIndex(string line, int startIndex)
        {
            for (int i = startIndex + 1; i < line.Length; i++)
            {
                if (line[i] == ']')
                {
                    return i;
                }

                if (line[i] == '=' || line[i] == ';')
                {
                    return startIndex;
                }
            }

            return startIndex;
        }

        private static string[] GetCodeLines(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return new[] { "" };
            }

            return NormalizeCode(code).Split('\n');
        }

        private static string NormalizeCode(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return "";
            }

            return code.Replace("\r\n", "\n").Replace('\r', '\n');
        }

        private static string GetExtensionLabel(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return "";
            }

            int dotIndex = fileName.LastIndexOf('.');
            if (dotIndex < 0 || dotIndex >= fileName.Length - 1)
            {
                return "";
            }

            return fileName.Substring(dotIndex + 1).ToUpperInvariant();
        }

        private static float GetLineNumberWidth(int lineCount)
        {
            int digitCount = Mathf.Max(2, lineCount.ToString().Length);
            return digitCount * 8f + 22f;
        }

        private static float GetCodeLineHeight()
        {
            if (_selectableCodeStyle == null)
            {
                return FallbackLineHeight;
            }

            return Mathf.Ceil(_selectableCodeStyle.lineHeight);
        }

        private static bool StartsWith(string line, int index, string value)
        {
            if (index + value.Length > line.Length)
            {
                return false;
            }

            for (int i = 0; i < value.Length; i++)
            {
                if (line[index + i] != value[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsIdentifierStart(char character)
        {
            return char.IsLetter(character) || character == '_' || character == '@';
        }

        private static bool IsIdentifierPart(char character)
        {
            return char.IsLetterOrDigit(character) || character == '_';
        }

        private static bool IsNumberPart(char character)
        {
            return char.IsLetterOrDigit(character) || character == '.' || character == '_';
        }

        private static bool IsOperatorCharacter(char character)
        {
            return character == '=' || character == '+' || character == '-' || character == '*' ||
                   character == '/' || character == '%' || character == '!' || character == '?' ||
                   character == ':' || character == ';' || character == ',' || character == '.' ||
                   character == '(' || character == ')' || character == '{' || character == '}' ||
                   character == '[' || character == ']' || character == '<' || character == '>';
        }
    }
}

using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CompilerManager : MonoBehaviour
{
    [SerializeField] private Transform content; // Scroll View/Viewport/Content
    [SerializeField] private Compiler compiler;
    public GameObject CommandPrefab; // Ссылка на префаб (перетащите в инспектор)
    public Transform spawnParent;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void SpawnCommand()
    {
        if (CommandPrefab == null || spawnParent == null)
        {
            Debug.LogError("CompilerManager: не задан CommandPrefab и/или spawnParent.");
            return;
        }

        Instantiate(CommandPrefab, spawnParent);
    }

    public void RunCode()
    {
        if (content == null)
        {
            Debug.LogError("CompilerManager: не задан Transform 'content' (Scroll View/Viewport/Content).");
            return;
        }

        if (compiler == null)
            compiler = FindFirstObjectByType<Compiler>();

        if (compiler == null)
        {
            Debug.LogError("CompilerManager: не найден компонент Compiler в сцене (добавь Compiler на любой GameObject и/или привяжи поле 'compiler' в инспекторе).");
            return;
        }

        string[] names = LogAllNamesInContent();
        foreach (string name in names)
        {
            var command = FindDeep(content, name);
            if (command == null) continue;

            var param = command.Find("param");
            var value = command.Find("value");

            string paramText = ReadUiText(param);
            string valueText = ReadUiText(value);

            // Частый случай: в InputField/TMP_InputField не введён текст, и мы читаем плейсхолдер.
            if (string.IsNullOrWhiteSpace(paramText) || LooksLikePlaceholder(param, paramText))
                continue;
            if (string.IsNullOrWhiteSpace(valueText) || LooksLikePlaceholder(value, valueText))
                continue;

            var variableName = SanitizeIdentifier(paramText);
            if (string.IsNullOrWhiteSpace(variableName))
            {
                Debug.LogError($"CompilerManager: некорректное имя переменной '{paramText}' (Command='{name}'). Разрешены буквы/цифры/_ и первый символ не цифра.");
                continue;
            }

            // Превращаем ввод в C# выражение. Если это не число/bool/null/литерал,
            // считаем это строкой и добавляем кавычки/экранирование.
            var expr = ToCSharpExpression(valueText);
            if (string.IsNullOrWhiteSpace(expr))
            {
                Debug.LogError($"CompilerManager: пустое значение (Command='{name}', var='{variableName}').");
                continue;
            }

            // Важно: это "eval". Не компилируй ввод из недоверенных источников.
            string codeToCompile = BuildDynamicScript(variableName, expr);

            var asm = compiler.CompileCode(codeToCompile);
            if (asm == null)
            {
                Debug.LogError($"CompilerManager: компиляция не удалась (Command='{name}').");
                continue;
            }

            compiler.ExecuteCompiledCode(asm, "DynamicScript", "Run");
        }
    }

    private static string BuildDynamicScript(string variableName, string expression)
    {
        return $@"
using UnityEngine;
public class DynamicScript
{{
    public void Run()
    {{
        var {variableName} = {expression};
        Debug.Log({variableName});
    }}
}}";
    }

    private static string SanitizeIdentifier(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        string s = raw.Trim();
        return Regex.IsMatch(s, @"^[A-Za-z_][A-Za-z0-9_]*$") ? s : string.Empty;
    }

    private static string ToCSharpExpression(string raw)
    {
        if (raw == null) return string.Empty;
        string s = raw.Trim();
        if (s.Length == 0) return string.Empty;

        // Если уже похоже на валидный литерал — оставляем как есть.
        if (s.StartsWith("\"") && s.EndsWith("\"") && s.Length >= 2) return s;
        if (s.StartsWith("@\"") && s.EndsWith("\"") && s.Length >= 3) return s;
        if (s.StartsWith("'") && s.EndsWith("'") && s.Length >= 3) return s; // char literal

        if (bool.TryParse(s, out _)) return s.ToLowerInvariant();
        if (s.Equals("null", System.StringComparison.OrdinalIgnoreCase)) return "null";

        if (int.TryParse(s, out _)) return s;
        if (float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _)) return s;
        if (double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _)) return s;

        // Иначе считаем строкой из UI.
        return "\"" + EscapeCSharpString(s) + "\"";
    }

    private static string EscapeCSharpString(string s)
    {
        var sb = new StringBuilder(s.Length + 8);
        foreach (char c in s)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '\"': sb.Append("\\\""); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default: sb.Append(c); break;
            }
        }
        return sb.ToString();
    }

    private static bool LooksLikePlaceholder(Transform root, string text)
    {
        if (root == null) return false;
        if (string.IsNullOrWhiteSpace(text)) return true;

        var tmpInput = root.GetComponentInChildren<TMP_InputField>(true);
        if (tmpInput != null)
        {
            var placeholderText = (tmpInput.placeholder as TMP_Text)?.text;
            if (!string.IsNullOrWhiteSpace(placeholderText) && text.Trim() == placeholderText.Trim())
                return true;
        }

        var input = root.GetComponentInChildren<InputField>(true);
        if (input != null)
        {
            var placeholderText = (input.placeholder as Text)?.text;
            if (!string.IsNullOrWhiteSpace(placeholderText) && text.Trim() == placeholderText.Trim())
                return true;
        }

        // На случай дефолтного текста без корректной ссылки на placeholder
        if (text.Trim().Equals("Enter text...", System.StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    public List<GameObject> GetAllObjectsInContent(bool includeInactive = true)
    {
        var list = new List<GameObject>();
        if (content == null) return list;
        foreach (var t in content.GetComponentsInChildren<Transform>(includeInactive))
        {
            if (t == content) continue;
            list.Add(t.gameObject);
        }
        return list;
    }

    public string[] LogAllNamesInContent()
    {
        var objs = GetAllObjectsInContent(includeInactive: true);
        var names = new List<string>(objs.Count);
        foreach (var go in objs)
        {
            if (go != null && go.name != null && go.name.StartsWith("Command"))
                names.Add(go.name);
        }
        return names.ToArray();
    }

    private static Transform FindDeep(Transform root, string exactName)
    {
        if (root == null || exactName == null) return null;
        foreach (Transform child in root)
        {
            if (child.name == exactName) return child;
            var found = FindDeep(child, exactName);
            if (found != null) return found;
        }
        return null;
    }

    private static string ReadUiText(Transform t)
    {
        if (t == null) return string.Empty;

        // Для полей ввода важнее брать введённый текст, а не TMP_Text дочернего объекта.
        var tmpInput = t.GetComponentInChildren<TMP_InputField>(true);
        if (tmpInput != null) return tmpInput.text ?? string.Empty;

        var input = t.GetComponentInChildren<InputField>(true);
        if (input != null) return input.text ?? string.Empty;

        var tmp = t.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null) return tmp.text ?? string.Empty;

        var uiText = t.GetComponentInChildren<Text>(true);
        if (uiText != null) return uiText.text ?? string.Empty;

        return string.Empty;
    }
}

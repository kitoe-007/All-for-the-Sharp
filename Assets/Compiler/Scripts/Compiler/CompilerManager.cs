using System;
using System.Collections;
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
    private int CommandCounter = 1;
    private int HolderCounter = 1;

    public Transform ScrollContent => content;

    private string DynamicScriptStart = $@"
using UnityEngine;
public class DynamicScript
{{
    public void Run()
    {{
        
";

    private string DynamicScriptEnd = $@"
    }}
}}";

    public GameObject VariableCommandPrefab; // Ссылка на префаб (перетащите в инспектор)
    [Tooltip("Запасной родитель для клонов палитры, если Command Palette Scroll не задан. Должен быть Content палитры, не Viewport.")]
    public Transform spawnParent;
    [Tooltip("Если задан — клоны при перетаскивании всегда создаются в его Content (имеет приоритет над spawn Parent).")]
    [SerializeField] private ScrollRect commandPaletteScroll;
    public GameObject PrintCommandPrefab;
    public GameObject IfConditionCommandPrefab;
    public GameObject OpenBracketCommandPrefab;
    public GameObject CloseBracketCommandPrefab;

    [Tooltip("Вертикальный зазор между слотами Holder (как nextDropSpacingPixels у DropZone). Используется при сдвиге после удаления пустого слота.")]
    [SerializeField] private float holderVerticalSpacingPixels = 75f;

    /// <summary>Имена Holder с последнего <see cref="RunCode"/> (<see cref="GetHolderNamesSorted"/>).</summary>
    public string[] LastHolderNamesOrdered { get; private set; }

    /// <summary>Спавн по типу команды.</summary>
    public void SpawnCommand(CompilerCommandType commandType)
    {
        switch (commandType)
        {
            case CompilerCommandType.VariableCommand:
                SpawnVariableCommand();
                break;
            case CompilerCommandType.PrintCommand:
                SpawnPrintCommand();
                break;
            case CompilerCommandType.IfConditionCommand:
                SpawnIfConditionCommand();
                break;
            case CompilerCommandType.OpenBracketCommand:
                SpawnOpenBracketCommand();
                break;
            case CompilerCommandType.CloseBracketCommand:
                SpawnCloseBracketCommand();
                break;
            default:
                Debug.LogWarning($"CompilerManager: не обработан {nameof(CompilerCommandType)}.{commandType}");
                break;
        }
    }

    /// <summary>Совместимость со вызовами по строке.</summary>
    public void SpawnCommand(string type)
    {
        if (Enum.TryParse(type, ignoreCase: true, out CompilerCommandType parsed))
            SpawnCommand(parsed);
        else
            Debug.LogWarning($"CompilerManager: неизвестный тип команды «{type}».");
    }
    /// <summary>
    /// Родитель для клонов при StartDrag: сначала <see cref="commandPaletteScroll"/>.content (если Scroll задан),
    /// иначе <see cref="spawnParent"/> — чтобы старый spawn Parent не перетягивал клоны мимо палитры.
    /// </summary>
    public Transform GetCommandSpawnParent()
    {
        if (commandPaletteScroll != null && commandPaletteScroll.content != null)
            return commandPaletteScroll.content;
        return spawnParent;
    }

    private void PlaceSpawnedCommandInPaletteList(GameObject go, Transform parent)
    {
        if (go == null || parent == null)
            return;
        go.transform.SetAsLastSibling();

        // Не трогаем якоря/LayoutElement у клона — это ломает VerticalLayoutGroup (всё в одну кучу).
        // Достаточно отложенно пересобрать Content, когда rect родителя уже посчитан.
        if (parent is RectTransform contentRt)
            StartCoroutine(CoRebuildScrollContentLayoutDeferred(contentRt));
    }

    private IEnumerator CoRebuildScrollContentLayoutDeferred(RectTransform content)
    {
        yield return null;
        if (content == null)
            yield break;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }

    private void RefreshCommandPaletteLayoutIfNeeded(Transform instantiatedParent)
    {
        if (instantiatedParent == null)
            return;
        if (commandPaletteScroll == null || commandPaletteScroll.content == null)
            return;
        if (instantiatedParent != commandPaletteScroll.content)
            return;

        Canvas.ForceUpdateCanvases();
        if (commandPaletteScroll.content is RectTransform crt)
            LayoutRebuilder.ForceRebuildLayoutImmediate(crt);
    }

    public void SpawnVariableCommand()
    {
        if (VariableCommandPrefab == null)
        {
            Debug.LogError(
                "CompilerManager: VariableCommandPrefab не назначен. Перетащите префаб команды в поле VariableCommandPrefab на объекте с CompilerManager.");
            return;
        }

        Transform parent = GetCommandSpawnParent();
        GameObject go = parent != null
            ? Instantiate(VariableCommandPrefab, parent)
            : Instantiate(VariableCommandPrefab);
        if (parent == null)
            Debug.LogWarning(
                "CompilerManager: не задан Command Palette Scroll и spawn Parent — экземпляр создан без родителя.");

        CommandCounter++;
        go.name = $"VariableCommand_{CommandCounter}";
        PlaceSpawnedCommandInPaletteList(go, parent);
        RefreshCommandPaletteLayoutIfNeeded(parent);
    }

    public void SpawnPrintCommand()
    {
        if (PrintCommandPrefab == null)
        {
            Debug.LogError(
                "CompilerManager: PrintCommandPrefab не назначен. Перетащите префаб команды в поле PrintCommandPrefab на объекте с CompilerManager.");
            return;
        }

        Transform parent = GetCommandSpawnParent();
        GameObject go = parent != null
            ? Instantiate(PrintCommandPrefab, parent)
            : Instantiate(PrintCommandPrefab);
        if (parent == null)
            Debug.LogWarning(
                "CompilerManager: не задан Command Palette Scroll и spawn Parent — экземпляр создан без родителя.");

        CommandCounter++;
        go.name = $"PrintCommand_{CommandCounter}";
        PlaceSpawnedCommandInPaletteList(go, parent);
        RefreshCommandPaletteLayoutIfNeeded(parent);
    }

    public void SpawnIfConditionCommand()
    {
        if (IfConditionCommandPrefab == null)
        {
            Debug.LogError(
                "CompilerManager: IfConditionCommandPrefab не назначен. Перетащите префаб в поле IfConditionCommandPrefab.");
            return;
        }

        Transform parent = GetCommandSpawnParent();
        GameObject go = parent != null
            ? Instantiate(IfConditionCommandPrefab, parent)
            : Instantiate(IfConditionCommandPrefab);
        if (parent == null)
            Debug.LogWarning(
                "CompilerManager: не задан Command Palette Scroll и spawn Parent — экземпляр создан без родителя.");

        CommandCounter++;
        go.name = $"IfConditionCommand_{CommandCounter}";
        PlaceSpawnedCommandInPaletteList(go, parent);
        RefreshCommandPaletteLayoutIfNeeded(parent);
    }

    public void SpawnOpenBracketCommand()
    {
        if (OpenBracketCommandPrefab == null)
        {
            Debug.LogError(
                "CompilerManager: OpenBracketCommandPrefab не назначен. Перетащите префаб в поле OpenBracketCommandPrefab.");
            return;
        }

        Transform parent = GetCommandSpawnParent();
        GameObject go = parent != null
            ? Instantiate(OpenBracketCommandPrefab, parent)
            : Instantiate(OpenBracketCommandPrefab);
        if (parent == null)
            Debug.LogWarning(
                "CompilerManager: не задан Command Palette Scroll и spawn Parent — экземпляр создан без родителя.");

        CommandCounter++;
        go.name = $"OpenBracketCommand_{CommandCounter}";
        PlaceSpawnedCommandInPaletteList(go, parent);
        RefreshCommandPaletteLayoutIfNeeded(parent);
    }

    public void SpawnCloseBracketCommand()
    {
        if (CloseBracketCommandPrefab == null)
        {
            Debug.LogError(
                "CompilerManager: CloseBracketCommandPrefab не назначен. Перетащите префаб в поле CloseBracketCommandPrefab.");
            return;
        }

        Transform parent = GetCommandSpawnParent();
        GameObject go = parent != null
            ? Instantiate(CloseBracketCommandPrefab, parent)
            : Instantiate(CloseBracketCommandPrefab);
        if (parent == null)
            Debug.LogWarning(
                "CompilerManager: не задан Command Palette Scroll и spawn Parent — экземпляр создан без родителя.");

        CommandCounter++;
        go.name = $"CloseBracketCommand_{CommandCounter}";
        PlaceSpawnedCommandInPaletteList(go, parent);
        RefreshCommandPaletteLayoutIfNeeded(parent);
    }

    /// <summary>
    /// Следующее имя для нового слота Holder — по тому же правилу, что <see cref="SpawnVariableCommand"/>:
    /// сначала инкремент счётчика, затем <c>Holder_{номер}</c>.
    /// </summary>
    public string TakeNextHolderName()
    {
        HolderCounter++;
        return $"Holder_{HolderCounter}";
    }

    /// <summary>
    /// Принудительно пересчитывает layout для ScrollView Content, чтобы ScrollRect увидел новые элементы.
    /// Вызывайте после Instantiate/изменения иерархии UI под <see cref="content"/>.
    /// </summary>
    public void ForceUpdateScrollContentLayout()
    {
        if (content == null)
            return;

        // Важно: сначала обновить canvases, затем пересобрать layout именно у content.
        Canvas.ForceUpdateCanvases();
        if (content is RectTransform rt)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);

        // На всякий случай: если ScrollRect висит на родителе (типовая структура Scroll View),
        // пересоберём и viewport/корень, чтобы обновились bounds.
        var scroll = content.GetComponentInParent<ScrollRect>();
        if (scroll != null)
        {
            if (scroll.viewport != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.viewport);
            if (scroll.transform is RectTransform srt)
                LayoutRebuilder.ForceRebuildLayoutImmediate(srt);
        }
    }

    /// <summary>
    /// Корутину нельзя продолжать на перетаскиваемом объекте после <c>Destroy</c> — она обрывается.
    /// Сжатие пустых Holder выполняется здесь, на живом <see cref="MonoBehaviour"/>.
    /// </summary>
    public void ScheduleTryCollapseRedundantEmptyHoldersAfterMissedDrop()
    {
        StartCoroutine(CollapseRedundantEmptyHoldersAfterEndOfFrameCoroutine());
    }

    private IEnumerator CollapseRedundantEmptyHoldersAfterEndOfFrameCoroutine()
    {
        yield return new WaitForEndOfFrame();
        TryCollapseRedundantEmptyHoldersAfterCommandMissedDrop();
    }

    /// <summary>
    /// После сброса команды мимо Holder (объект команды уничтожен): если среди прямых детей
    /// <see cref="content"/> больше одного пустого Holder, удаляет пустой с минимальным «номером»
    /// в порядке <see cref="GetHolderNamesSorted"/> и сдвигает все следующие слоты вверх на
    /// высоту удалённого + <see cref="holderVerticalSpacingPixels"/>.
    /// </summary>
    public void TryCollapseRedundantEmptyHoldersAfterCommandMissedDrop()
    {
        if (content == null)
            return;

        var holdersOrdered = new List<Transform>();
        foreach (Transform child in content)
        {
            if (child != null && IsHolderRootName(child.name))
                holdersOrdered.Add(child);
        }

        holdersOrdered.Sort((a, b) => CompareHolderNames(a.name, b.name));

        var emptyHolders = new List<Transform>();
        foreach (Transform h in holdersOrdered)
        {
            if (IsHolderWithoutCommand(h))
                emptyHolders.Add(h);
        }

        if (emptyHolders.Count <= 1)
            return;

        emptyHolders.Sort((a, b) => CompareHolderNames(a.name, b.name));
        Transform toDestroy = emptyHolders[0];
        int removeIndex = holdersOrdered.IndexOf(toDestroy);
        if (removeIndex < 0)
            return;

        var removeRt = toDestroy as RectTransform;
        float slotStep = GetHolderSlotVerticalStep(removeRt);

        UnityEngine.Object.Destroy(toDestroy.gameObject);

        for (int i = removeIndex + 1; i < holdersOrdered.Count; i++)
        {
            Transform t = holdersOrdered[i];
            if (t == null)
                continue;
            if (t is RectTransform rt)
                rt.anchoredPosition += Vector2.up * slotStep;
        }

        ForceUpdateScrollContentLayout();
    }

    /// <summary>
    /// Пустой слот: под Holder нет блока команды (VariableCommand / PrintCommand / IfConditionCommand / …),
    /// в том числе если команда вложена под DropZone, а не прямой ребёнок Holder.
    /// </summary>
    private static bool IsHolderWithoutCommand(Transform holder)
    {
        if (holder == null)
            return true;
        foreach (Transform t in holder.GetComponentsInChildren<Transform>(true))
        {
            if (t == holder)
                continue;
            string n = t.name;
            if (n.StartsWith("VariableCommand", StringComparison.Ordinal) ||
                n.StartsWith("PrintCommand", StringComparison.Ordinal) ||
                n.StartsWith("IfConditionCommand", StringComparison.Ordinal) ||
                n.StartsWith("OpenBracketCommand", StringComparison.Ordinal) ||
                n.StartsWith("CloseBracketCommand", StringComparison.Ordinal))
                return false;
        }
        return true;
    }

    private float GetHolderSlotVerticalStep(RectTransform holderRt)
    {
        float h = 0f;
        if (holderRt != null)
        {
            h = holderRt.rect.height > 0f ? holderRt.rect.height : Mathf.Abs(holderRt.sizeDelta.y);
        }

        return h + holderVerticalSpacingPixels;
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

        // Holder, Holder_1, Holder_2, … — прямые дети content, по возрастанию номера.
        LastHolderNamesOrdered = GetHolderNamesSorted();

        var script = new StringBuilder();
        script.Append(DynamicScriptStart);
        script.Append(Environment.NewLine);

        foreach (string holderName in LastHolderNamesOrdered)
        {
            Transform holder = FindDeep(content, holderName);
            if (holder == null)
            {
                Debug.LogWarning($"CompilerManager: Holder '{holderName}' не найден под content.");
                continue;
            }

            // Только непосредственные дочерние объекты — блоки команд.
            foreach (Transform commandRoot in holder)
            {
                TryAppendCommandRunBody(script, commandRoot, holderName);
            }
        }

        script.Append(DynamicScriptEnd);

        string codeToCompile = script.ToString();
        Debug.Log($"CompilerManager: сгенерированный DynamicScript:\n{codeToCompile}");
        var asm = compiler.CompileCode(codeToCompile);
        if (asm == null)
        {
            Debug.LogError("CompilerManager: компиляция объединённого DynamicScript не удалась.");
            return;
        }

        compiler.ExecuteCompiledCode(asm, "DynamicScript", "Run");
    }

    /// <summary>
    /// Добавляет в тело <c>Run()</c> строки для одной команды (по имени корня в иерархии).
    /// </summary>
    private void TryAppendCommandRunBody(StringBuilder sb, Transform commandRoot, string holderName)
    {
        if (commandRoot == null) return;

        string n = commandRoot.name;
        if (n.StartsWith("VariableCommand", StringComparison.Ordinal))
        {
            TryAppendVariableCommandBody(sb, commandRoot, holderName);
            return;
        }
        else if (n.StartsWith("PrintCommand", StringComparison.Ordinal))
        {
            TryAppendPrintCommandBody(sb, commandRoot, holderName);
            return;
        }
        else if (n.StartsWith("IfConditionCommand", StringComparison.Ordinal))
        {
            TryAppendIfConditionCommandBody(sb, commandRoot, holderName);
            return;
        }
        else if (n.StartsWith("OpenBracketCommand", StringComparison.Ordinal))
        {
            sb.AppendLine("        {");
            return;
        }
        else if (n.StartsWith("CloseBracketCommand", StringComparison.Ordinal))
        {
            sb.AppendLine("        }");
            return;
        }
    }

    /// <summary>
    /// Команда вывода: в UI поле <c>param</c> — имя переменной (как в C#) или литерал/выражение,
    /// по тем же правилам, что поле значения у VariableCommand (<see cref="ToCSharpExpression"/>).
    /// </summary>
    private void TryAppendPrintCommandBody(StringBuilder sb, Transform commandRoot, string holderName)
    {
        var param = commandRoot.Find("param");
        string paramText = ReadUiText(param);

        if (string.IsNullOrWhiteSpace(paramText) || LooksLikePlaceholder(param, paramText))
            return;

        var variableName = SanitizeIdentifier(paramText);
        if (!string.IsNullOrWhiteSpace(variableName))
        {
            sb.AppendLine($"        Debug.Log({variableName});");
            return;
        }

        var expr = ToCSharpExpression(paramText);
        if (string.IsNullOrWhiteSpace(expr))
        {
            Debug.LogError(
                $"CompilerManager: не удалось преобразовать вывод '{paramText}' в выражение C# (Holder='{holderName}', Command='{commandRoot.name}').");
            return;
        }

        sb.AppendLine($"        Debug.Log({expr});");
    }

    /// <summary>
    /// Dropdown в <c>kind</c> / <c>type</c> / <c>if</c>: «if» и «else if» — условие в <c>condition</c> / <c>param</c>
    /// (сырое C# в скобках); «else» — строка <c>else</c> без скобок, поле условия не используется.
    /// </summary>
    private void TryAppendIfConditionCommandBody(StringBuilder sb, Transform commandRoot, string holderName)
    {
        var kindRoot = commandRoot.Find("kind") ?? commandRoot.Find("type") ?? commandRoot.Find("if");
        var conditionRoot = commandRoot.Find("condition") ?? commandRoot.Find("param");

        string branchText = ReadDropdownSelection(kindRoot).Trim();
        string conditionText = ReadUiText(conditionRoot);

        bool isElseIf = branchText.IndexOf("else if", StringComparison.OrdinalIgnoreCase) >= 0;
        bool isElse = !isElseIf && branchText.Equals("else", StringComparison.OrdinalIgnoreCase);

        if (isElse)
        {
            sb.AppendLine("        else");
            return;
        }

        if (string.IsNullOrWhiteSpace(conditionText) || LooksLikePlaceholder(conditionRoot, conditionText))
            return;

        string cond = conditionText.Trim();
        string keyword = isElseIf ? "else if" : "if";
        sb.AppendLine($"        {keyword} ({cond})");
    }

    private void TryAppendVariableCommandBody(StringBuilder sb, Transform commandRoot, string holderName)
    {
        var typeRoot = commandRoot.Find("type");
        var param = commandRoot.Find("param");
        var operatorRoot = commandRoot.Find("operator");
        var value = commandRoot.Find("value");

        string typeText = ReadDropdownSelection(typeRoot);
        string paramText = ReadUiText(param);
        string valueText = ReadUiText(value);
        string operatorText = ReadDropdownSelection(operatorRoot);

        if (string.IsNullOrWhiteSpace(paramText) || LooksLikePlaceholder(param, paramText))
            return;
        if (string.IsNullOrWhiteSpace(valueText) || LooksLikePlaceholder(value, valueText))
            return;

        var variableName = SanitizeIdentifier(paramText);
        if (string.IsNullOrWhiteSpace(variableName))
        {
            Debug.LogError(
                $"CompilerManager: некорректное имя переменной '{paramText}' (Holder='{holderName}', Command='{commandRoot.name}'). Разрешены буквы/цифры/_ и первый символ не цифра.");
            return;
        }

        var expr = ToCSharpExpression(valueText);
        if (string.IsNullOrWhiteSpace(expr))
        {
            Debug.LogError(
                $"CompilerManager: пустое значение (Holder='{holderName}', Command='{commandRoot.name}', var='{variableName}').");
            return;
        }

        sb.AppendLine($"        {typeText} {variableName} {operatorText} {expr};");
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
            if (go != null && go.name != null && go.name.IndexOf("Command", System.StringComparison.Ordinal) >= 0)
                names.Add(go.name);
        }
        return names.ToArray();
    }

    /// <summary>
    /// Имена всех слотов Holder среди <b>прямых</b> детей <see cref="content"/>:
    /// <c>Holder</c>, затем <c>Holder_1</c>, <c>Holder_2</c>, … по числовому суффиксу.
    /// </summary>
    public string[] GetHolderNamesSorted()
    {
        if (content == null)
            return Array.Empty<string>();

        var list = new List<string>();
        foreach (Transform child in content)
        {
            if (child == null) continue;
            string n = child.name;
            if (IsHolderRootName(n))
                list.Add(n);
        }

        list.Sort(CompareHolderNames);
        return list.ToArray();
    }

    private static bool IsHolderRootName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;
        if (string.Equals(name, "Holder", StringComparison.OrdinalIgnoreCase))
            return true;
        return Regex.IsMatch(name, @"^Holder_\d+$", RegexOptions.IgnoreCase);
    }

    /// <summary>Порядок: голый Holder первым, затем Holder_0, Holder_1, … по числу.</summary>
    private static int CompareHolderNames(string a, string b)
    {
        return HolderSortKey(a).CompareTo(HolderSortKey(b));
    }

    private static int HolderSortKey(string name)
    {
        if (string.Equals(name, "Holder", StringComparison.OrdinalIgnoreCase))
            return -1;

        var m = Regex.Match(name, @"^Holder_(\d+)$", RegexOptions.IgnoreCase);
        if (m.Success && int.TryParse(m.Groups[1].Value, out int n))
            return n;

        return int.MaxValue;
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

    private static string ReadDropdownSelection(Transform t)
    {
        if (t == null) return string.Empty;

        var tmpDropdown = t.GetComponentInChildren<TMP_Dropdown>(true);
        if (tmpDropdown != null)
        {
            int i = tmpDropdown.value;
            if (i >= 0 && i < tmpDropdown.options.Count)
                return tmpDropdown.options[i].text ?? string.Empty;
            return i.ToString();
        }

        var dropdown = t.GetComponentInChildren<Dropdown>(true);
        if (dropdown != null)
        {
            int i = dropdown.value;
            if (i >= 0 && i < dropdown.options.Count)
                return dropdown.options[i].text ?? string.Empty;
            return i.ToString();
        }

        return ReadUiText(t);
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

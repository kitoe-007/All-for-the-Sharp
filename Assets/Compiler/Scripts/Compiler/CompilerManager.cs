using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CompilerManager : MonoBehaviour
{
    [Tooltip("Content редактора программы (Holder, команды). Не панель Output.")]
    [SerializeField] private Transform content;
    [SerializeField] private Compiler compiler;
    private int CommandCounter = 1;
    private int HolderCounter = 1;

    public Transform ScrollContent => content;

    private string DynamicScriptStart = $@"
using System.Text;
using UnityEngine;
public class DynamicScript
{{
    public string Result = """";
    public void Run()
    {{
        var __output = new StringBuilder();
";

    private string DynamicScriptEnd = $@"
        Result = __output.ToString();
    }}
}}";

    public GameObject VariableCommandPrefab; // Ссылка на префаб (перетащите в инспектор)
    public Transform spawnParent;

    [Tooltip("Если spawn Parent — RectTransform: выровнять клон по левому краю и сдвинуть по Y (см. поля ниже). Выключи, если на родителе Vertical Layout Group сам расставляет детей.")]
    [SerializeField] private bool applySpawnRectLayout = true;
    [Tooltip("Отступ от левого края контейнера (anchored X).")]
    [SerializeField] private float spawnLayoutLeftPadding = 8f;
    [Tooltip("Смещение по Y от верхнего края контейнера (в координатах UI: вниз — отрицательные значения).")]
    [SerializeField] private float spawnLayoutTopOffset = 0f;
    [Tooltip("Шаг по Y между строками (индекс × шаг). 0 — взять высоту самого блока (или 48), чтобы клоны при drag не схлопывались в одну точку.")]
    [SerializeField] private float spawnLayoutRowStep = 0f;

    public GameObject PrintCommandPrefab;
    public GameObject IfConditionCommandPrefab;
    public GameObject OpenBracketCommandPrefab;
    public GameObject CloseBracketCommandPrefab;
    public GameObject ForCommandPrefab;
    public GameObject WhileCommandPrefab;

    [Tooltip("Корень панели Output (Scroll View/Viewport/Content/Text). Не content редактора.")]
    public GameObject OutPutObj;

    [Tooltip("TMP для вывода Print. Перетащите Text из панели Output — надёжнее, чем автопоиск.")]
    [SerializeField] private TMP_Text outputTextUi;

    [Tooltip("Вертикальный зазор между слотами Holder (как nextDropSpacingPixels у DropZone). Используется при сдвиге после удаления пустого слота.")]
    [SerializeField] private float holderVerticalSpacingPixels = 75f;

    [Tooltip("При Start один раз создать в spawn Parent по экземпляру каждого типа команды (дополнительно к логике DragAndDrop).")]
    [SerializeField] private bool spawnFullCommandPaletteOnStart = true;

    [Tooltip("Перед SpawnFullCommandPalette удалить прямых детей spawn Parent, чьи имена похожи на блоки команд — иначе дубли, если команды уже лежат в сцене под Content.")]
    [SerializeField] private bool clearSpawnParentCommandsBeforePaletteSpawn = true;

    [Tooltip("При OnBeginDrag создавать клон команды. Если уже есть палитра при Start — обычно выключи: при Row Step 0 клоны накапливаются в одной точке.")]
    [SerializeField] private bool spawnCommandCloneOnBeginDrag = false;

    /// <summary>Имена Holder с последнего <see cref="RunCode"/> (<see cref="GetHolderNamesSorted"/>).</summary>
    public string[] LastHolderNamesOrdered { get; private set; }

    private void Start()
    {
        if (spawnFullCommandPaletteOnStart)
            SpawnFullCommandPalette();
    }

    /// <summary>По одному вызову <see cref="SpawnCommand"/> для каждого значения <see cref="CompilerCommandType"/>.</summary>
    public void SpawnFullCommandPalette()
    {
        if (spawnParent == null)
        {
            Debug.LogWarning("CompilerManager: spawn Parent не задан — стартовая палитра не создана.");
            return;
        }

        if (clearSpawnParentCommandsBeforePaletteSpawn)
            ClearExistingPaletteCommandRootsUnderSpawnParent();

        var types = (CompilerCommandType[])Enum.GetValues(typeof(CompilerCommandType));
        System.Array.Sort(types, (a, b) => ((int)a).CompareTo((int)b));
        foreach (CompilerCommandType t in types)
            SpawnCommand(t, (int)t);
    }

    /// <summary>Прямые дети <see cref="spawnParent"/> с именами корней команд — как при старте из сцены + из SpawnFullCommandPalette.</summary>
    private void ClearExistingPaletteCommandRootsUnderSpawnParent()
    {
        if (spawnParent == null)
            return;
        for (int i = spawnParent.childCount - 1; i >= 0; i--)
        {
            Transform c = spawnParent.GetChild(i);
            if (c != null && IsPaletteCommandRootName(c.name))
                DestroyImmediate(c.gameObject);
        }
    }

    /// <summary>Имя корня блока команды в палитре / Holder (без вложенных детей).</summary>
    private static bool IsPaletteCommandRootName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;
        return name.StartsWith("VariableCommand", StringComparison.Ordinal) ||
               name.StartsWith("PrintCommand", StringComparison.Ordinal) ||
               name.StartsWith("IfConditionCommand", StringComparison.Ordinal) ||
               name.StartsWith("ForCommand", StringComparison.Ordinal) ||
               name.StartsWith("WhileCommand", StringComparison.Ordinal) ||
               name.StartsWith("OpenBracketCommand", StringComparison.Ordinal) ||
               name.StartsWith("CloseBracketCommand", StringComparison.Ordinal);
    }

    /// <summary>Спавн по типу команды.</summary>
    /// <param name="paletteSiblingIndex">
    /// Если ≥ 0 и родитель — <see cref="spawnParent"/>: вставить новый элемент на эту позицию в иерархии
    /// (чтобы клон при драге из палитры оставался на месте исходного блока). Иначе — в конец списка.
    /// </param>
    public void SpawnCommand(CompilerCommandType commandType, int paletteSiblingIndex = -1)
    {
        switch (commandType)
        {
            case CompilerCommandType.VariableCommand:
                SpawnVariableCommand(paletteSiblingIndex);
                break;
            case CompilerCommandType.PrintCommand:
                SpawnPrintCommand(paletteSiblingIndex);
                break;
            case CompilerCommandType.IfConditionCommand:
                SpawnIfConditionCommand(paletteSiblingIndex);
                break;
            case CompilerCommandType.OpenBracketCommand:
                SpawnOpenBracketCommand(paletteSiblingIndex);
                break;
            case CompilerCommandType.CloseBracketCommand:
                SpawnCloseBracketCommand(paletteSiblingIndex);
                break;
            case CompilerCommandType.ForCommand:
                SpawnForCommand(paletteSiblingIndex);
                break;
            case CompilerCommandType.WhileCommand:
                SpawnWhileCommand(paletteSiblingIndex);
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
    /// Если на <see cref="spawnParent"/> есть <see cref="LayoutGroup"/> — не используем ручные anchoredPosition
    /// (они ломают Vertical/Horizontal layout). Иначе — только при включённом applySpawnRectLayout.
    /// </summary>
    private static bool SpawnParentHasUILayout(Transform parent)
    {
        return parent != null && parent.GetComponent<LayoutGroup>() != null;
    }

    /// <summary>
    /// Высота строки палитры: сначала соседи (уже с layout), затем сам блок, минимум 48.
    /// Нужна при спавне в том же кадре, что и BeginDrag, когда у нового RectTransform ещё нулевой rect.
    /// </summary>
    private static float InferPaletteRowHeight(RectTransform parentRt, RectTransform blockRt)
    {
        float maxOther = 0f;
        for (int i = 0; i < parentRt.childCount; i++)
        {
            if (!(parentRt.GetChild(i) is RectTransform crt))
                continue;
            if (crt == blockRt)
                continue;

            float sh = crt.rect.height;
            if (sh < 4f)
                sh = Mathf.Abs(crt.sizeDelta.y);
            if (crt.TryGetComponent<LayoutElement>(out var leo))
                sh = Mathf.Max(sh, leo.preferredHeight, leo.minHeight);
            maxOther = Mathf.Max(maxOther, sh);
        }

        float self = blockRt.rect.height;
        if (self < 4f)
            self = Mathf.Abs(blockRt.sizeDelta.y);
        if (blockRt.TryGetComponent<LayoutElement>(out var les))
            self = Mathf.Max(self, les.preferredHeight, les.minHeight);

        return Mathf.Max(maxOther, self, 48f);
    }

    /// <summary>
    /// Ручной вертикальный стек всех детей <paramref name="parentRt"/> (без LayoutGroup на родителе).
    /// Пересчитывает весь столбец — чтобы клоны при драге не накапливались в одной точке.
    /// </summary>
    private void ReflowManualSpawnChildren(RectTransform parentRt)
    {
        if (parentRt == null || !applySpawnRectLayout)
            return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(parentRt);

        float step = spawnLayoutRowStep;
        if (step < 0.01f)
        {
            step = 48f;
            for (int i = 0; i < parentRt.childCount; i++)
            {
                if (!(parentRt.GetChild(i) is RectTransform crt))
                    continue;
                float sh = crt.rect.height;
                if (sh < 4f)
                    sh = Mathf.Abs(crt.sizeDelta.y);
                if (crt.TryGetComponent<LayoutElement>(out var le))
                    sh = Mathf.Max(sh, le.preferredHeight, le.minHeight);
                step = Mathf.Max(step, sh);
            }
        }

        for (int i = 0; i < parentRt.childCount; i++)
        {
            if (!(parentRt.GetChild(i) is RectTransform crt))
                continue;
            crt.anchorMin = new Vector2(0f, 1f);
            crt.anchorMax = new Vector2(0f, 1f);
            crt.pivot = new Vector2(0f, 1f);
            float y = spawnLayoutTopOffset - step * i;
            crt.anchoredPosition = new Vector2(spawnLayoutLeftPadding, y);
        }
    }

    /// <summary>
    /// После Instantiate: либо отдаём раскладку <see cref="LayoutGroup"/> на родителе (типичный Content со Scroll),
    /// либо вручную — левый верх и шаг по Y.
    /// </summary>
    private void ApplySpawnParentLayoutIfNeeded(GameObject go, int paletteSiblingIndex = -1)
    {
        if (go == null || spawnParent == null)
            return;
        if (!(spawnParent is RectTransform parentRt))
            return;
        if (!go.TryGetComponent<RectTransform>(out var rt))
            return;

        if (paletteSiblingIndex >= 0 && go.transform.parent == spawnParent)
        {
            int max = Mathf.Max(0, parentRt.childCount - 1);
            rt.SetSiblingIndex(Mathf.Clamp(paletteSiblingIndex, 0, max));
        }
        else
            rt.SetAsLastSibling();

        if (SpawnParentHasUILayout(spawnParent))
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRt);

            float h = InferPaletteRowHeight(parentRt, rt);

            // Полоса на всю ширину Content; pivot слева сверху — без «съезда» по горизонтали при VLG.
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(0f, h);

            var le = go.GetComponent<LayoutElement>();
            if (le == null)
                le = go.AddComponent<LayoutElement>();
            le.minHeight = h;
            le.preferredHeight = h;
            le.flexibleHeight = 0f;
            le.flexibleWidth = 0f;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRt);
            StartCoroutine(CoRebuildSpawnParentLayoutDelayed(parentRt));
            return;
        }

        if (!applySpawnRectLayout)
            return;

        ReflowManualSpawnChildren(parentRt);
        StartCoroutine(CoRebuildSpawnParentLayoutDelayed(parentRt));
    }

    private IEnumerator CoRebuildSpawnParentLayoutDelayed(RectTransform parentRt)
    {
        yield return null;
        yield return null;
        if (parentRt == null)
            yield break;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(parentRt);
        if (spawnParent == parentRt && applySpawnRectLayout && !SpawnParentHasUILayout(spawnParent))
            ReflowManualSpawnChildren(parentRt);
        var scroll = parentRt.GetComponentInParent<ScrollRect>();
        if (scroll != null && scroll.viewport is RectTransform vrt)
            LayoutRebuilder.ForceRebuildLayoutImmediate(vrt);
    }

    /// <summary>Нужен ли дубликат при <see cref="DragAndDrop.OnBeginDrag"/> (см. поле в инспекторе).</summary>
    public bool SpawnCommandCloneOnBeginDrag => spawnCommandCloneOnBeginDrag;

    public void SpawnVariableCommand(int paletteSiblingIndex = -1)
    {
        if (VariableCommandPrefab == null)
        {
            Debug.LogError(
                "CompilerManager: VariableCommandPrefab не назначен. Перетащите префаб команды в поле VariableCommandPrefab на объекте с CompilerManager.");
            return;
        }

        GameObject go;
        if (spawnParent != null)
            go = Instantiate(VariableCommandPrefab, spawnParent);
        else
        {
            Debug.LogWarning("CompilerManager: spawnParent не задан — экземпляр создан без родителя.");
            go = Instantiate(VariableCommandPrefab);
        }

        CommandCounter++;
        go.name = $"VariableCommand_{CommandCounter}";
        ApplySpawnParentLayoutIfNeeded(go, paletteSiblingIndex);
    }

    public void SpawnPrintCommand(int paletteSiblingIndex = -1)
    {
        if (PrintCommandPrefab == null)
        {
            Debug.LogError(
                "CompilerManager: PrintCommandPrefab не назначен. Перетащите префаб команды в поле PrintCommandPrefab на объекте с CompilerManager.");
            return;
        }

        GameObject go;
        if (spawnParent != null)
            go = Instantiate(PrintCommandPrefab, spawnParent);
        else
        {
            Debug.LogWarning("CompilerManager: spawnParent не задан — экземпляр создан без родителя.");
            go = Instantiate(PrintCommandPrefab);
        }

        CommandCounter++;
        go.name = $"PrintCommand_{CommandCounter}";
        ApplySpawnParentLayoutIfNeeded(go, paletteSiblingIndex);
    }

    public void SpawnIfConditionCommand(int paletteSiblingIndex = -1)
    {
        if (IfConditionCommandPrefab == null)
        {
            Debug.LogError(
                "CompilerManager: IfConditionCommandPrefab не назначен. Перетащите префаб в поле IfConditionCommandPrefab.");
            return;
        }

        GameObject go;
        if (spawnParent != null)
            go = Instantiate(IfConditionCommandPrefab, spawnParent);
        else
        {
            Debug.LogWarning("CompilerManager: spawnParent не задан — экземпляр создан без родителя.");
            go = Instantiate(IfConditionCommandPrefab);
        }

        CommandCounter++;
        go.name = $"IfConditionCommand_{CommandCounter}";
        ApplySpawnParentLayoutIfNeeded(go, paletteSiblingIndex);
    }

    public void SpawnOpenBracketCommand(int paletteSiblingIndex = -1)
    {
        if (OpenBracketCommandPrefab == null)
        {
            Debug.LogError(
                "CompilerManager: OpenBracketCommandPrefab не назначен. Перетащите префаб в поле OpenBracketCommandPrefab.");
            return;
        }

        GameObject go;
        if (spawnParent != null)
            go = Instantiate(OpenBracketCommandPrefab, spawnParent);
        else
        {
            Debug.LogWarning("CompilerManager: spawnParent не задан — экземпляр создан без родителя.");
            go = Instantiate(OpenBracketCommandPrefab);
        }

        CommandCounter++;
        go.name = $"OpenBracketCommand_{CommandCounter}";
        ApplySpawnParentLayoutIfNeeded(go, paletteSiblingIndex);
    }

    public void SpawnCloseBracketCommand(int paletteSiblingIndex = -1)
    {
        if (CloseBracketCommandPrefab == null)
        {
            Debug.LogError(
                "CompilerManager: CloseBracketCommandPrefab не назначен. Перетащите префаб в поле CloseBracketCommandPrefab.");
            return;
        }

        GameObject go;
        if (spawnParent != null)
            go = Instantiate(CloseBracketCommandPrefab, spawnParent);
        else
        {
            Debug.LogWarning("CompilerManager: spawnParent не задан — экземпляр создан без родителя.");
            go = Instantiate(CloseBracketCommandPrefab);
        }

        CommandCounter++;
        go.name = $"CloseBracketCommand_{CommandCounter}";
        ApplySpawnParentLayoutIfNeeded(go, paletteSiblingIndex);
    }

    public void SpawnForCommand(int paletteSiblingIndex = -1)
    {
        if (ForCommandPrefab == null)
        {
            Debug.LogError(
                "CompilerManager: ForCommandPrefab не назначен. Перетащите префаб в поле ForCommandPrefab.");
            return;
        }

        GameObject go;
        if (spawnParent != null)
            go = Instantiate(ForCommandPrefab, spawnParent);
        else
        {
            Debug.LogWarning("CompilerManager: spawnParent не задан — экземпляр создан без родителя.");
            go = Instantiate(ForCommandPrefab);
        }

        CommandCounter++;
        go.name = $"ForCommand_{CommandCounter}";
        ApplySpawnParentLayoutIfNeeded(go, paletteSiblingIndex);
    }

    public void SpawnWhileCommand(int paletteSiblingIndex = -1)
    {
        if (WhileCommandPrefab == null)
        {
            Debug.LogError(
                "CompilerManager: WhileCommandPrefab не назначен. Перетащите префаб в поле WhileCommandPrefab.");
            return;
        }

        GameObject go;
        if (spawnParent != null)
            go = Instantiate(WhileCommandPrefab, spawnParent);
        else
        {
            Debug.LogWarning("CompilerManager: spawnParent не задан — экземпляр создан без родителя.");
            go = Instantiate(WhileCommandPrefab);
        }

        CommandCounter++;
        go.name = $"WhileCommand_{CommandCounter}";
        ApplySpawnParentLayoutIfNeeded(go, paletteSiblingIndex);
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
            if (IsPaletteCommandRootName(t.name))
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

        string answer = ExecuteDynamicScriptAndGetResult(asm);
        if (!SetOutputText(answer))
            Debug.Log($"CompilerManager: ответ программы (панель Output не настроена): «{answer}»");

        if (OutPutObj != null)
        {
            var taskMove = OutPutObj.GetComponent<Taskmove>();
            if (taskMove != null)
                taskMove.ismove = true;
        }
    }

    private string ExecuteDynamicScriptAndGetResult(Assembly asm)
    {
        const string typeName = "DynamicScript";
        const string methodName = "Run";
        const string resultFieldName = "Result";

        try
        {
            Type scriptType = asm.GetType(typeName);
            if (scriptType == null)
            {
                foreach (Type t in asm.GetTypes())
                {
                    if (t.Name == typeName)
                    {
                        scriptType = t;
                        break;
                    }
                }
            }

            if (scriptType == null)
            {
                Debug.LogError($"CompilerManager: класс {typeName} не найден в сборке.");
                return string.Empty;
            }

            object instance = Activator.CreateInstance(scriptType);
            var runMethod = scriptType.GetMethod(methodName);
            if (runMethod == null)
            {
                Debug.LogError($"CompilerManager: метод {methodName} не найден.");
                return string.Empty;
            }

            runMethod.Invoke(instance, null);

            var resultField = scriptType.GetField(resultFieldName);
            return resultField?.GetValue(instance) as string ?? string.Empty;
        }
        catch (Exception ex)
        {
            Debug.LogError($"CompilerManager: ошибка выполнения DynamicScript: {ex.InnerException?.Message ?? ex.Message}");
            return string.Empty;
        }
    }

    /// <returns>false, если TMP для вывода не найден.</returns>
    private bool SetOutputText(string text)
    {
        TMP_Text target = ResolveOutputText();
        if (target == null)
        {
            if (OutPutObj == null && outputTextUi == null)
            {
                Debug.LogWarning(
                    "CompilerManager: задайте Output Text Ui или Out Put Obj (панель Output).");
            }
            else
            {
                Debug.LogWarning(
                    "CompilerManager: TMP_Text не найден. На Text должен быть TextMeshPro - Text. " +
                    "Перетащите его в поле Output Text Ui.");
            }

            return false;
        }

        target.text = text ?? string.Empty;
        target.ForceMeshUpdate();
        RefreshOutputLayout(target.rectTransform);
        ScrollOutputToBottom();
        return true;
    }

    private static void RefreshOutputLayout(RectTransform start)
    {
        if (start == null) return;

        Canvas.ForceUpdateCanvases();
        for (RectTransform rt = start; rt != null; rt = rt.parent as RectTransform)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }

    private TMP_Text ResolveOutputText()
    {
        if (outputTextUi != null)
            return outputTextUi;

        if (OutPutObj == null) return null;

        Transform outputContent = OutPutObj.transform.Find("Scroll View/Viewport/Content");
        if (outputContent != null)
        {
            var onContent = outputContent.GetComponent<TMP_Text>();
            if (onContent != null) return onContent;

            foreach (Transform child in outputContent)
            {
                if (!child.name.Equals("Text", StringComparison.OrdinalIgnoreCase))
                    continue;
                var named = child.GetComponent<TMP_Text>();
                if (named != null) return named;
            }

            return outputContent.GetComponentInChildren<TMP_Text>(true);
        }

        TMP_Text[] all = OutPutObj.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text tmp in all)
        {
            if (tmp.gameObject.name.Equals("Text", StringComparison.OrdinalIgnoreCase))
                return tmp;
        }

        return all.Length > 0 ? all[0] : null;
    }

    private void ScrollOutputToBottom()
    {
        if (OutPutObj == null) return;

        var scroll = OutPutObj.GetComponentInChildren<ScrollRect>(true);
        if (scroll == null) return;

        Canvas.ForceUpdateCanvases();
        scroll.verticalNormalizedPosition = 0f;
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
        else if (n.StartsWith("ForCommand", StringComparison.Ordinal))
        {
            TryAppendForCommandBody(sb, commandRoot, holderName);
            return;
        }
        else if (n.StartsWith("WhileCommand", StringComparison.Ordinal))
        {
            TryAppendWhileCommandBody(sb, commandRoot, holderName);
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
    /// Цикл while: дочерний <c>condition</c> — выражение в скобках заголовка (сырой C#). Тело — команда OpenBracket ниже по Holder.
    /// </summary>
    private void TryAppendWhileCommandBody(StringBuilder sb, Transform commandRoot, string holderName)
    {
        var conditionRoot = commandRoot.Find("condition");
        string conditionText = ReadUiText(conditionRoot).Trim();

        if (string.IsNullOrWhiteSpace(conditionText) || LooksLikePlaceholder(conditionRoot, conditionText))
            return;

        sb.AppendLine($"        while ({conditionText})");
    }

    /// <summary>
    /// Цикл for: дочерний объект <c>condition</c> (поле ввода) — инициализация, условие и итератор одной строкой,
    /// как внутри скобок в C#: <c>int i = 0; i &lt; 10; i++</c>. Открывающая <c>&#123;</c> — отдельной командой OpenBracket.
    /// </summary>
    private void TryAppendForCommandBody(StringBuilder sb, Transform commandRoot, string holderName)
    {
        var conditionRoot = commandRoot.Find("condition");
        string inner = ReadUiText(conditionRoot).Trim();

        if (string.IsNullOrWhiteSpace(inner) || LooksLikePlaceholder(conditionRoot, inner))
            return;

        if (inner.Length >= 2 &&
            inner.StartsWith("(", StringComparison.Ordinal) &&
            inner.EndsWith(")", StringComparison.Ordinal))
            inner = inner.Substring(1, inner.Length - 2).Trim();

        inner = NormalizeForLoopHeader(inner);
        sb.AppendLine($"        for ({inner})");
    }

    /// <summary>
    /// Подправляет заголовок for из UI: <c>int i; i &lt; n; i++</c> → <c>int i = 0; i &lt; n; i++</c>.
    /// </summary>
    private static string NormalizeForLoopHeader(string inner)
    {
        if (string.IsNullOrWhiteSpace(inner))
            return inner;

        int semi1 = inner.IndexOf(';');
        if (semi1 < 0)
            return inner;
        int semi2 = inner.IndexOf(';', semi1 + 1);
        if (semi2 < 0)
            return inner;

        string init = inner.Substring(0, semi1).Trim();
        string cond = inner.Substring(semi1 + 1, semi2 - semi1 - 1).Trim();
        string iter = inner.Substring(semi2 + 1).Trim();

        if (Regex.IsMatch(init, @"^int\s+[A-Za-z_][A-Za-z0-9_]*\s*$"))
            init += " = 0";

        return $"{init}; {cond}; {iter}";
    }

    /// <summary>
    /// Команда вывода: в UI поле <c>param</c> — имя переменной, литерал (<see cref="ToCSharpExpression"/>)
    /// или сырое C#-выражение (<c>a[i]</c>, вызовы, операторы).
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
            sb.AppendLine($"        __output.AppendLine(System.Convert.ToString({variableName}));");
            return;
        }

        if (LooksLikeRawCSharpExpression(paramText))
        {
            sb.AppendLine($"        __output.AppendLine(System.Convert.ToString({paramText.Trim()}));");
            return;
        }

        var expr = ToCSharpExpression(paramText);
        if (string.IsNullOrWhiteSpace(expr))
        {
            Debug.LogError(
                $"CompilerManager: не удалось преобразовать вывод '{paramText}' в выражение C# (Holder='{holderName}', Command='{commandRoot.name}').");
            return;
        }

        sb.AppendLine($"        __output.AppendLine(System.Convert.ToString({expr}));");
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

    /// <summary>
    /// Текст из UI уже похож на C#-выражение (индексатор, вызов, оператор), а не на простой идентификатор/литерал.
    /// </summary>
    private static bool LooksLikeRawCSharpExpression(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return false;
        string s = raw.Trim();

        if (s.StartsWith("\"") && s.EndsWith("\"") && s.Length >= 2) return false;
        if (s.StartsWith("'") && s.EndsWith("'") && s.Length >= 3) return false;
        if (bool.TryParse(s, out _)) return false;
        if (s.Equals("null", StringComparison.OrdinalIgnoreCase)) return false;
        if (int.TryParse(s, out _)) return false;
        if (float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _)) return false;
        if (double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _)) return false;
        if (Regex.IsMatch(s, @"^[A-Za-z_][A-Za-z0-9_]*$")) return false;

        if (s.IndexOf('[') >= 0 || s.IndexOf('(') >= 0) return true;
        if (s.IndexOf('.') >= 0) return true;
        return Regex.IsMatch(s, @"[+\-*/%<>=!&|^~?:]");
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

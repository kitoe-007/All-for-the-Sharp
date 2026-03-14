using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Microsoft.CodeAnalysis.Emit;

public class Compiler : MonoBehaviour
{
    // Простой пример кода, который будем компилировать
    private string codeToCompile = @"
using UnityEngine;
public class DynamicScript
{
    public void Run()
    {
        Debug.Log(""Это сообщение из скомпилированного кода!"");
    }
}";

    [ContextMenu("Run Test")]
    public void RunTest()
    {
        Debug.Log("Начинаем компиляцию...");
        Assembly compiledAssembly = CompileCode(codeToCompile);

        if (compiledAssembly != null)
        {
            Debug.Log("Компиляция успешна! Пытаемся выполнить код...");
            ExecuteCompiledCode(compiledAssembly);
        }
        else
        {
            Debug.LogError("Компиляция не удалась.");
        }
    }

    private Assembly CompileCode(string sourceCode)
    {
        // Создаём синтаксическое дерево из кода
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

        // Имя сборки
        string assemblyName = "DynamicAssembly_" + Guid.NewGuid().ToString("N");

        // Собираем ссылки на все сборки, загруженные в текущий домен
        var references = new List<MetadataReference>();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
            {
                try
                {
                    references.Add(MetadataReference.CreateFromFile(assembly.Location));
                    Debug.Log($"Добавлена ссылка на: {assembly.GetName().Name}");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Не удалось добавить ссылку на {assembly.GetName().Name}: {ex.Message}");
                }
            }
        }

        // Параметры компиляции
        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName,
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Компилируем в поток памяти
        using (var ms = new MemoryStream())
        {
            EmitResult result = compilation.Emit(ms);

            if (!result.Success)
            {
                // Выводим ошибки компиляции
                IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                    diagnostic.IsWarningAsError ||
                    diagnostic.Severity == DiagnosticSeverity.Error);

                foreach (Diagnostic diagnostic in failures)
                {
                    Debug.LogError($"Ошибка компиляции: {diagnostic.GetMessage()}");
                }
                return null;
            }
            else
            {
                ms.Seek(0, SeekOrigin.Begin);
                byte[] assemblyBytes = ms.ToArray();
                return Assembly.Load(assemblyBytes);
            }
        }
    }

    private void ExecuteCompiledCode(Assembly assembly)
    {
        try
        {
            // Ищем класс DynamicScript (как в нашем примере кода)
            Type scriptType = assembly.GetType("DynamicScript");
            if (scriptType != null)
            {
                object scriptInstance = Activator.CreateInstance(scriptType);
                MethodInfo runMethod = scriptType.GetMethod("Run");
                if (runMethod != null)
                {
                    runMethod.Invoke(scriptInstance, null);
                }
                else
                {
                    Debug.LogError("Метод Run не найден в классе DynamicScript");
                }
            }
            else
            {
                Debug.LogError("Класс DynamicScript не найден в сборке");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Ошибка при выполнении кода: {ex.Message}");
        }
    }
}
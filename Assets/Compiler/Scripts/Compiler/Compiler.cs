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

    // Roslyn компиляция требует набора ссылок (MetadataReference).
    // В Unity они уже загружены в AppDomain; собираем ссылки из Location.
    // Важно: некоторые сборки могут не иметь Location или быть динамическими — их пропускаем.
    public Assembly CompileCode(string sourceCode)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

        string assemblyName = "DynamicAssembly_" + Guid.NewGuid().ToString("N");

        var references = new List<MetadataReference>();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
            {
                try
                {
                    references.Add(MetadataReference.CreateFromFile(assembly.Location));
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Не удалось добавить ссылку на {assembly.GetName().Name}: {ex.Message}");
                }
            }
        }

        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName,
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using (var ms = new MemoryStream())
        {
            EmitResult result = compilation.Emit(ms);

            if (!result.Success)
            {
                IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                    diagnostic.IsWarningAsError ||
                    diagnostic.Severity == DiagnosticSeverity.Error);

                foreach (Diagnostic diagnostic in failures)
                {
                    Debug.LogError($"Ошибка компиляции: {diagnostic.GetMessage()}");
                }
                return null;
            }

            ms.Seek(0, SeekOrigin.Begin);
            byte[] assemblyBytes = ms.ToArray();
            return Assembly.Load(assemblyBytes);
        }
    }

    public void ExecuteCompiledCode(Assembly assembly, string typeName = "DynamicScript", string methodName = "Run")
    {
        try
        {
            Type scriptType = assembly.GetType(typeName);
            if (scriptType != null)
            {
                object scriptInstance = Activator.CreateInstance(scriptType);
                MethodInfo runMethod = scriptType.GetMethod(methodName);
                if (runMethod != null)
                {
                    runMethod.Invoke(scriptInstance, null);
                }
                else
                {
                    Debug.LogError($"Метод {methodName} не найден в классе {typeName}");
                }
            }
            else
            {
                Debug.LogError($"Класс {typeName} не найден в сборке");
            }
        }
        catch (Exception ex)
        {
            // Внутреннее исключение часто содержит первопричину (например, ошибка в Run()).
            Debug.LogError($"Ошибка при выполнении кода: {ex.InnerException?.Message ?? ex.Message}");
        }
    }
}
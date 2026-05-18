/// <summary>
/// Тип команды по имени корня блока (ForCommand_1, …). Не зависит от устаревшего int в префабе после смены enum.
/// </summary>
public static class CompilerCommandKind
{
    public static CompilerCommandType FromRootName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
            return CompilerCommandType.VariableCommand;

        string n = objectName.Trim();
        if (n.StartsWith("VariableCommand", System.StringComparison.Ordinal))
            return CompilerCommandType.VariableCommand;
        if (n.StartsWith("PrintCommand", System.StringComparison.Ordinal))
            return CompilerCommandType.PrintCommand;
        if (n.StartsWith("IfConditionCommand", System.StringComparison.Ordinal))
            return CompilerCommandType.IfConditionCommand;
        if (n.StartsWith("ForCommand", System.StringComparison.Ordinal))
            return CompilerCommandType.ForCommand;
        if (n.StartsWith("WhileCommand", System.StringComparison.Ordinal))
            return CompilerCommandType.WhileCommand;
        if (n.StartsWith("OpenBracketCommand", System.StringComparison.Ordinal))
            return CompilerCommandType.OpenBracketCommand;
        if (n.StartsWith("CloseBracketCommand", System.StringComparison.Ordinal))
            return CompilerCommandType.CloseBracketCommand;

        return CompilerCommandType.VariableCommand;
    }
}

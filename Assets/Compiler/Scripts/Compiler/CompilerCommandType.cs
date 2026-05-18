/// <summary>
/// Тип команды. Число = слот в палитре (0 — первый сверху).
/// </summary>
public enum CompilerCommandType
{
    VariableCommand = 0,
    PrintCommand = 1,
    IfConditionCommand = 2,
    ForCommand = 3,
    WhileCommand = 4,
    OpenBracketCommand = 5,
    CloseBracketCommand = 6,
}

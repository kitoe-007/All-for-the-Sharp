using UnityEngine;

public class CommandSpawn : MonoBehaviour
{
    [SerializeField] private CompilerManager compilerManager;
    [SerializeField] private CompilerCommandType commandType;

    public CompilerCommandType CommandKind => commandType;

    void Awake()
    {
        if (compilerManager == null)
            compilerManager = FindFirstObjectByType<CompilerManager>();
    }

    public void Spawn()
    {
        compilerManager?.SpawnCommand(commandType);
    }
}

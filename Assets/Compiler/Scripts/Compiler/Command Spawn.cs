using UnityEngine;

public class CommandSpawn : MonoBehaviour
{
    [SerializeField] private CompilerManager compilerManager;
    [SerializeField] private string type;

    void Awake()
    {
        if (compilerManager == null)
            compilerManager = FindFirstObjectByType<CompilerManager>();
    }

    public void Spawn()
    {
        compilerManager?.SpawnCommand(type);
    }
}

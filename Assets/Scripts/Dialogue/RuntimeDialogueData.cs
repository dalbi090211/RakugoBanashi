using System.Collections.Generic;

public class RuntimeDialogueData
{
    public string sceneName;
    public List<GameEvent> dialogues = new();
    public Dictionary<string, List<GameEvent>> branches = new();
}
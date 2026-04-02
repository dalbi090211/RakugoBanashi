using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "DialogueData", menuName = "Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [SerializeReference]  // 다형성 직렬화 활성화
    public List<GameEvent> dialogues = new List<GameEvent>();

    public string sceneName;
} 
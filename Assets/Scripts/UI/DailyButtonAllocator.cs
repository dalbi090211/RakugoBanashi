using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class DailyButtonAllocator : MonoBehaviour
{
    // ✅ 씬 이름 목록만 지정 (StreamingAssets/Dialogues/ 기준)
    [SerializeField] private List<string> dialogueNames = new();

    private RuntimeDialogueData[] dialogues;

    private async UniTaskVoid Start()
    {
        await LoadAll();
    }

    private async UniTask LoadAll()
    {
        dialogues = new RuntimeDialogueData[dialogueNames.Count];

        for (int i = 0; i < dialogueNames.Count; i++)
        {
            dialogues[i] = await DialogueJsonLoader.LoadAsync(dialogueNames[i]);

            if (dialogues[i] == null)
                Debug.LogError($"[DailyButtonAllocator] {dialogueNames[i]} 로드 실패");
        }

        Debug.Log($"✅ {dialogueNames.Count}개 다이얼로그 로드 완료");
    }

    public void OnClickButton(int buttonIdx)
    {
        if (dialogues == null || buttonIdx >= dialogues.Length) return;
        if (dialogues[buttonIdx] == null) return;

        DialogueManager.Instance.StartEvent(dialogues[buttonIdx]).Forget();
    }
}
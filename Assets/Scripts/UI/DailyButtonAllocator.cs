using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class DailyButtonAllocator : MonoBehaviour   //monobehaviour 나중에 지울듯
{
    [SerializeField] List<DialogueData> manualData; //debug용 manual data

    private DialogueData[] dialogues = new DialogueData[3];

    //debug
    private void dataAlloc()
    {
        for (int i = 0; i < 3; i++)
        {
            dialogues[i] = manualData[i];
        }
    }

    public void OnClickButton(int buttonIdx)
    {
        DialogueManager.Instance.StartEvent(dialogues[buttonIdx]).Forget();
    }

    private void Start()
    {
        dataAlloc();
    }
}

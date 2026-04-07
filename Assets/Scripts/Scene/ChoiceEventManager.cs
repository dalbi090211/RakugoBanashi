using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Cysharp.Threading.Tasks;
using System.Threading;

public class ChoiceEventManager : MonoBehaviour
{
    [SerializeField] private GameObject gameLayer;
    [SerializeField] private GameObject leftChoiceLayer;
    [SerializeField] private GameObject rightChoiceLayer;

    [SerializeField] private ChoiceButton leftChoiceButton1;
    [SerializeField] private ChoiceButton leftChoiceButton2;
    [SerializeField] private ChoiceButton leftChoiceButton3;
    [SerializeField] private ChoiceButton rightChoiceButton1;
    [SerializeField] private ChoiceButton rightChoiceButton2;
    [SerializeField] private ChoiceButton rightChoiceButton3;

    [SerializeField] private Image timerBar; // 타이머 UI (Image fillAmount으로 표시)
    [SerializeField] private float timeLimit = 5f;

    private CancellationTokenSource timerCts;
    private choiceRes defaultChoice; // 시간 초과시 실행할 기본 선택지

    private void Awake()
    {
        leftChoiceLayer.SetActive(false);
        rightChoiceLayer.SetActive(false);
        gameLayer.SetActive(false);
    }

    public void ShowChoice(ChoiceDialogue choiceDial)
    {
        defaultChoice = choiceDial.next1; // 기본값은 첫번째 선택지

        if (choiceDial.isLeft)
            setLeftCanva(choiceDial.next1, choiceDial.next2, choiceDial.next3);
        else
            setRightCanva(choiceDial.next1, choiceDial.next2, choiceDial.next3);
    }

    public async UniTask TimerTask()
    {
        timerCts = new CancellationTokenSource();
        float elapsed = 0f;

        while (elapsed < timeLimit)
        {
            if (timerCts.IsCancellationRequested) return;
            elapsed += Time.deltaTime;
            if (timerBar != null)
                timerBar.fillAmount = 1f - (elapsed / timeLimit);
            await UniTask.Yield();
        }

        // 취소 안됐을 때만 (시간 초과) 실행
        if (!timerCts.IsCancellationRequested)
            OnChoiceSelected(defaultChoice);
    }

    public void OnChoiceSelected(choiceRes choice)
    {
        timerCts?.Cancel();
        canvaOff();
        if (timerBar != null) timerBar.fillAmount = 1f;

        DialogueManager.Instance.AppendEvent(
            Resources.Load<DialogueData>(choice.appendDialPath)
        );
    }

    private void setLeftCanva(choiceRes dial1, choiceRes dial2, choiceRes dial3)
    {
        leftChoiceLayer.SetActive(true);
        gameLayer.SetActive(true);
        leftChoiceButton1.setData(dial1, this);
        leftChoiceButton2.setData(dial2, this);
        leftChoiceButton3.setData(dial3, this);
    }

    private void setRightCanva(choiceRes dial1, choiceRes dial2, choiceRes dial3)
    {
        rightChoiceLayer.SetActive(true);
        gameLayer.SetActive(true);
        rightChoiceButton1.setData(dial1, this);
        rightChoiceButton2.setData(dial2, this);
        rightChoiceButton3.setData(dial3, this);
    }

    private void canvaOff()
    {
        leftChoiceLayer.SetActive(false);
        rightChoiceLayer.SetActive(false);
        gameLayer.SetActive(false);
    }
}
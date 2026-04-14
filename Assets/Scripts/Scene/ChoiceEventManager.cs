using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

public class ChoiceEventManager : MonoBehaviour
{
    [Header("Canvas Groups for Fading")]
    [SerializeField] private CanvasGroup leftCanvasGroup;
    [SerializeField] private CanvasGroup middleCanvasGroup;
    [SerializeField] private CanvasGroup rightCanvasGroup;
    private CanvasGroup _currentActiveGroup;

    [Header("Choice Layers")]
    [SerializeField] private GameObject leftChoiceLayer;
    [SerializeField] private GameObject rightChoiceLayer;
    [SerializeField] private GameObject middleChoiceLayer;

    [Header("Buttons")]
    [SerializeField] private ChoiceButton leftChoiceButton1, leftChoiceButton2, leftChoiceButton3;
    [SerializeField] private ChoiceButton rightChoiceButton1, rightChoiceButton2, rightChoiceButton3;
    [SerializeField] private ChoiceButton middleChoiceButton1, middleChoiceButton2, middleChoiceButton3;

    [Header("Timer UI")]
    [SerializeField] private Slider timerSlider;
    [SerializeField] private CanvasGroup timerCanvasGroup; // ← 추가
    [SerializeField] private float timeLimit = 5f;

    private CancellationTokenSource timerCts;
    private choiceRes defaultChoice;
    private UniTaskCompletionSource<choiceRes> _choiceTaskSource;

    public async UniTask<choiceRes> ShowChoice(ChoiceDialogue choiceDial)
    {
        _choiceTaskSource = new UniTaskCompletionSource<choiceRes>();
        defaultChoice = choiceDial.next1;

        SetChoiceCanvas(choiceDial.direction, choiceDial.next1, choiceDial.next2, choiceDial.next3);

        // 선택지 + 타이머 동시에 페이드 인
        await UniTask.WhenAll(
            fadeCanvaOn(0.5f),
            fadeTimerOn(0.5f)
        );

        StartTimer().Forget();

        choiceRes result = await _choiceTaskSource.Task;

        // 선택지 + 타이머 동시에 페이드 아웃
        await UniTask.WhenAll(
            fadeCanvaOff(0.3f),
            fadeTimerOff(0.3f)
        );

        if (timerSlider != null) timerSlider.value = 1f;
        return result;
    }

    private async UniTask fadeTimerOn(float duration = 0.5f)
    {
        if (timerCanvasGroup == null) return;

        timerCanvasGroup.alpha = 0f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            timerCanvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
            await UniTask.Yield(PlayerLoopTiming.Update);
        }
        timerCanvasGroup.alpha = 1f;
    }

    private async UniTask fadeTimerOff(float duration = 0.3f)
    {
        if (timerCanvasGroup == null) return;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            timerCanvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / duration));
            await UniTask.Yield(PlayerLoopTiming.Update);
        }
        timerCanvasGroup.alpha = 0f;
    }

    private void SetChoiceCanvas(CamDir direction, choiceRes d1, choiceRes d2, choiceRes d3)
    {
        canvaOff();

        CanvasGroup group = null;
        switch (direction)
        {
            case CamDir.left:
                leftChoiceLayer.SetActive(true);
                group = leftCanvasGroup;
                leftChoiceButton1.setData(d1, this);
                leftChoiceButton2.setData(d2, this);
                leftChoiceButton3.setData(d3, this);
                break;
            case CamDir.middle:
                middleChoiceLayer.SetActive(true);
                group = middleCanvasGroup;
                middleChoiceButton1.setData(d1, this);
                middleChoiceButton2.setData(d2, this);
                middleChoiceButton3.setData(d3, this);
                break;
            case CamDir.right:
                rightChoiceLayer.SetActive(true);
                group = rightCanvasGroup;
                rightChoiceButton1.setData(d1, this);
                rightChoiceButton2.setData(d2, this);
                rightChoiceButton3.setData(d3, this);
                break;
        }

        _currentActiveGroup = group;
        if (_currentActiveGroup != null)
        {
            _currentActiveGroup.alpha = 0f;
            _currentActiveGroup.interactable = false;
            _currentActiveGroup.blocksRaycasts = false;
        }
    }

    private async UniTask StartTimer()
    {
        timerCts?.Cancel();
        timerCts?.Dispose();
        timerCts = new CancellationTokenSource();

        float elapsed = 0f;
        var token = timerCts.Token;

        if (timerSlider != null)
        {
            timerSlider.minValue = 0f;
            timerSlider.maxValue = 1f;
            timerSlider.value = 1f;
        }

        try
        {
            while (elapsed < timeLimit)
            {
                elapsed += Time.deltaTime;
                if (timerSlider != null)
                    timerSlider.value = 1f - (elapsed / timeLimit);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
            OnChoiceSelected(defaultChoice);
        }
        catch (OperationCanceledException) { }
    }

    public void OnChoiceSelected(choiceRes choice)
    {
        timerCts?.Cancel();

        // AppendEvent만 처리, 페이드는 ShowChoice에서 await
        var nextData = Resources.Load<DialogueData>(choice.appendDialPath);
        if (nextData != null)
            DialogueManager.Instance.AppendEvent(nextData);

        _choiceTaskSource?.TrySetResult(choice);
    }

    public async UniTask fadeCanvaOn(float duration = 0.5f)
    {
        if (_currentActiveGroup == null) return;

        _currentActiveGroup.interactable = false;
        _currentActiveGroup.blocksRaycasts = false;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _currentActiveGroup.alpha = Mathf.Clamp01(elapsed / duration);
            await UniTask.Yield(PlayerLoopTiming.Update);
        }

        _currentActiveGroup.alpha = 1f;
        _currentActiveGroup.interactable = true;
        _currentActiveGroup.blocksRaycasts = true;
    }

    public async UniTask fadeCanvaOff(float duration = 0.3f)
    {
        if (_currentActiveGroup == null) return;

        _currentActiveGroup.interactable = false;
        _currentActiveGroup.blocksRaycasts = false;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _currentActiveGroup.alpha = Mathf.Clamp01(1f - (elapsed / duration));
            await UniTask.Yield(PlayerLoopTiming.Update);
        }

        _currentActiveGroup.alpha = 0f;
        canvaOff();
    }

    private void canvaOff()
    {
        leftChoiceLayer.SetActive(false);
        rightChoiceLayer.SetActive(false);
        middleChoiceLayer.SetActive(false);
        _currentActiveGroup = null;
    }

    private void OnDestroy()
    {
        timerCts?.Cancel();
        timerCts?.Dispose();
    }
}
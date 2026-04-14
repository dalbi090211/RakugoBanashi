using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MakuraStarter : MonoBehaviour
{
    [SerializeField] private TMP_InputField userInput;
    [SerializeField] private TextMeshProUGUI topicTextBox;
    [SerializeField] float letterDelay;
    [SerializeField] float topicDelay;
    private MakuraEvaluator makuraEvaluator = new MakuraEvaluator();
    private bool isSubmitted = false;
    private const string fixedText = "오늘의 주제";
    public async UniTask<MakuraResult> InputAwait(string topic)
    {
        isSubmitted = false;
        setInputField(true);
        userInput.text = "";
        userInput.ActivateInputField();

        await UniTask.WaitUntil(() => isSubmitted);

        setInputField(false);

        string input = userInput.text;
        await DialogueManager.Instance.ShowText(input, CamDir.middle);
        MakuraResult result = await makuraEvaluator.Evaluate(topic, input);

        // 0~100 → 10~30 or -30~-10
        if (result.score > 50)
            result.score = Mathf.RoundToInt(Mathf.Lerp(10f, 30f, (result.score - 50) / 50f));
        else
            result.score = Mathf.RoundToInt(Mathf.Lerp(-30f, -10f, result.score / 50f));

        return result;
    }

    public async UniTask fillInputField(string topic)
    {
        topicTextBox.gameObject.SetActive(true);
        topicTextBox.text = "";
        for (int i = 0; i < fixedText.Length; i++)
        {
            topicTextBox.text += fixedText[i];
            await UniTask.Delay(TimeSpan.FromSeconds(letterDelay));
        }
        topicTextBox.text += " : ";

        await UniTask.Delay(TimeSpan.FromSeconds(letterDelay));
        await UniTask.Delay(TimeSpan.FromSeconds(letterDelay));

        for (int i = 0; i < topic.Length; i++)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(topicDelay));
            topicTextBox.text += topic[i];
        }
    }

    public void setInputField(bool active)
    {
        topicTextBox.gameObject.SetActive(active);
        userInput.gameObject.SetActive(active);
        userInput.interactable = active;
    }

    // 확인 버튼 or 엔터에 연결
    public void OnSubmit()
    {
        if (string.IsNullOrEmpty(userInput.text)) return;
        isSubmitted = true;
    }

    private void Awake()
    {
        userInput.onSubmit.AddListener((value) =>
        {
            OnSubmit();
        });
        setInputField(false);
    }
}
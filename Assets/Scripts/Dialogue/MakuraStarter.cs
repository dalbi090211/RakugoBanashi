using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MakuraStarter : MonoBehaviour
{
    [SerializeField] private TMP_InputField userInput;
    [SerializeField] private TextMeshProUGUI topicTextBox;
    private MakuraEvaluator makuraEvaluator = new MakuraEvaluator();
    private bool isSubmitted = false;

    public async UniTask<MakuraResult> InputAwait(string topic)
    {
        isSubmitted = false;
        setInputField(true);
        userInput.text = "";
        userInput.ActivateInputField();

        await UniTask.WaitUntil(() => isSubmitted);

        setInputField(false);

        string input = userInput.text;
        MakuraResult result = await makuraEvaluator.Evaluate(topic, input);
        return result;
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
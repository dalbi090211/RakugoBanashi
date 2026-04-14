using Cysharp.Threading.Tasks;
using UnityEngine;

public class EndingChoiceManager : MonoBehaviour
{
    [SerializeField] private EndingButton[] buttons;
    [SerializeField] private CanvasGroup cg;

    private endingRes selected;
    private bool isDone;

    private void Awake()
    {
        cg.alpha = 0;
        cg.interactable = false;
        cg.blocksRaycasts = false;
    }

    public async UniTask<endingRes> ShowEnding(EndingChoice data, int curFlow, float fadeTime = 0.5f)
    {
        isDone = false;
        gameObject.SetActive(true);

        endingRes[] endings = {
            data.ending1, data.ending2,
            data.ending3, data.ending4
        };

        for (int i = 0; i < buttons.Length; i++)
        {
            bool isUnlocked = curFlow >= endings[i].requiredFlow;
            buttons[i].Init(endings[i], isUnlocked, OnSelect);
        }

        await Fade(0f, 1f, fadeTime);
        await UniTask.WaitUntil(() => isDone);
        await Fade(1f, 0f, fadeTime);

        gameObject.SetActive(false);
        return selected;
    }

    private async UniTask Fade(float from, float to, float time)
    {
        cg.interactable = false;
        cg.blocksRaycasts = false;
        float elapsed = 0f;
        cg.alpha = from;

        while (elapsed < time)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, elapsed / time);
            await UniTask.Yield();
        }

        cg.alpha = to;
        cg.interactable = true;
        cg.blocksRaycasts = true;
    }

    private void OnSelect(endingRes ending)
    {
        selected = ending;
        isDone = true;
    }
}
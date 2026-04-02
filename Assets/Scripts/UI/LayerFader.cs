using System.Collections;
using UnityEngine;

public class LayerFader : MonoBehaviour
{
    [SerializeField] private CanvasGroup selectLayer;
    [SerializeField] private float fadeDuration = 1f;

    // 외부에서 호출
    public void FadeOut()
    {
        selectLayer.interactable = false;
        selectLayer.blocksRaycasts = false;
        StartCoroutine(FadeAlpha(1f, 0f));
    }
    public void FadeIn()
    {
        selectLayer.interactable = true;
        selectLayer.blocksRaycasts = true;
        StartCoroutine(FadeAlpha(0f, 1f));
    }

    private IEnumerator FadeAlpha(float from, float to)
    {
        float elapsed = 0f;
        selectLayer.alpha = from;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            selectLayer.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }

        selectLayer.alpha = to;
    }
}
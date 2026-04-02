using UnityEngine;

public class ButtonMapper : MonoBehaviour
{
    [SerializeField] private LayerFader TitleLayer;
    [SerializeField] private LayerFader NormalLayer;
    [SerializeField] private LayerFader AILayer;

    public void TitleOn()
    {
        TitleLayer.FadeIn();
    }

    public void TitleOff()
    {
        TitleLayer.FadeOut();
        NormalLayer.FadeOut();
        AILayer.FadeOut();
    }

    public void OnClickNormal()
    {
        TitleLayer.FadeOut();
        NormalLayer.FadeIn();
    }

    public void OnClickAI()
    {
        TitleLayer.FadeOut();
        AILayer.FadeIn();
    }

    public void OnClickNormal2Title()
    {
        NormalLayer.FadeOut();
        TitleLayer.FadeIn();
    }

    public void OnClickAI2Title()
    {
        AILayer.FadeOut();
        TitleLayer.FadeIn();
    }
}

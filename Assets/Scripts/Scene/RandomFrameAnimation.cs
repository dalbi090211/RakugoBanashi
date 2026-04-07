using UnityEngine;
using System.Collections;

public class RandomFrameAnimation : MonoBehaviour
{
    public Sprite[] frames;
    private SpriteRenderer sr;
    private int currentFrame = 0;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        StartCoroutine(PlayAnimation());
    }

    IEnumerator PlayAnimation()
    {
        currentFrame = Random.Range(0, frames.Length); // 초기 난수값
        while (true)
        {
            sr.sprite = frames[currentFrame];

            if (Random.Range(0, 2) >= 1) currentFrame++;
            else currentFrame--;

            // 범위 초과 방지
            currentFrame = Mathf.Clamp(currentFrame, 0, frames.Length - 1);

            float randomDelay = Random.Range(0.6f, 2.5f);
            yield return new WaitForSeconds(randomDelay);
        }
    }
}
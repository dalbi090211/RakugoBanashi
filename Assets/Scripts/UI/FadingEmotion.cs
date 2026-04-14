using UnityEngine;

public class FadingEmotion : MonoBehaviour
{
    private const float maxTime = 2.0f;
    private const float distance = 6.0f;

    private float curTime = 0f;
    private SpriteRenderer sr;
    private Vector3 startPos;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        startPos = transform.position;
    }

    private void Update()
    {
        curTime += Time.deltaTime;

        float t = Mathf.Clamp01(curTime / maxTime);
        float moveT = Mathf.SmoothStep(0f, 1f, t);
        Vector3 targetPos = startPos + new Vector3(0, distance, 0);
        transform.position = Vector3.Lerp(startPos, targetPos, moveT);

        float alpha = 1f - Mathf.Pow(t, 2); // ease-in
        Color c = sr.color;
        c.a = alpha;
        sr.color = c;

        if (t >= 1f)
            Destroy(gameObject);
    }
}
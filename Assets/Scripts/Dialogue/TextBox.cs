using Cysharp.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

/// <summary>
/// 텍스트 박스 렌더링 전담 클래스
/// 말풍선 구조: left | leftBorder | middle(stretch) | rightBorder | right
/// </summary>
public class TextBox : MonoBehaviour
{
    // ── 박스 레이아웃 ────────────────────────────────────────────
    [SerializeField] private RectTransform uiRect;
    [SerializeField] private float maxWidth = 600f;

    [Header("박스 여백")]
    [SerializeField] private float marginX = 10f;
    [SerializeField] private float marginY = 5f;

    [Header("말풍선 오브젝트")]
    [SerializeField] private RectTransform leftBox;
    [SerializeField] private RectTransform leftBorderBox;
    [SerializeField] private RectTransform middleBox;       // 텍스트 너비에 맞게 stretch
    [SerializeField] private RectTransform rightBorderBox;
    [SerializeField] private RectTransform rightBox;

    // ── TMP ──────────────────────────────────────────────────────
    [SerializeField] private TextMeshProUGUI textBox;
    private ContentSizeFitter textFitter;
    private TMP_TextInfo textInfo;
    private Vector3[][] originalVertices;
    private Color32[][] newVertexColors;

    // ── 출력 설정 ─────────────────────────────────────────────────
    [Header("출력 속도")]
    [SerializeField] public float fadeDuration = 0.01f;
    [SerializeField] public float moveDistance = 5f;
    [SerializeField] public int letterShowDelay = 30;

    [Header("사운드")]
    [SerializeField] private float talkSpeed = 1f;

    // ── 특수효과 설정 ──────────────────────────────────────────────
    [Header("특수효과")]
    [SerializeField] private float shakeIntensity = 1f;
    [SerializeField] private float upwnIntensity = 2f;

    // ── 런타임 상태 ───────────────────────────────────────────────
    private int curIndex;
    private TextBoxData curData;
    private CancellationTokenSource showCTS;
    private CancellationTokenSource effectCTS;

    // 상태 버퍼
    private Vector3[] baseOffsets;    // Fade용
    private Vector3[] effectOffsets;  // Shake/UpDown
    private float[] charAlpha;

    // 랜덤 캐시 (Shake 안정화)
    private float[] randomPhase;

    // ─────────────────────────────────────────────────────────────
    #region Unity Lifecycle

    private void Awake()
    {
        if (uiRect == null) uiRect = GetComponent<RectTransform>();
        textFitter = textBox.GetComponent<ContentSizeFitter>();
    }

    private void OnDestroy() => CancelAll();

    #endregion

    // ─────────────────────────────────────────────────────────────
    #region Public API

    public async UniTask Init(Vector2 position, TextBoxData data)
    {
        CancelAll();
        ResetCTS();

        curData = data;
        curIndex = 0;

        SetBoxVisible(true);
        ApplyText(data.parsedText);
        ResizeBox();
        uiRect.anchoredPosition = position;

        SetupMeshCache();
        RenderLoop().Forget();
        await PlayTextAnim();
    }

    public bool Skip()
    {
        if (curIndex >= textInfo.characterCount)
        {
            return true;
        }
        else
        {
            letterShowDelay = 0;
            return false;
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────────
    #region Init Helpers

    private void ApplyText(string text)
    {
        textBox.text = text;
        textBox.margin = new Vector4(marginX / 2, marginY / 2, marginX / 2, marginY / 2);
        textBox.ForceMeshUpdate();

        var textRect = textBox.GetComponent<RectTransform>();
        float w = textBox.preferredWidth;
        float h = textBox.preferredHeight;

        if (w < maxWidth)
        {
            if (textFitter != null) textFitter.enabled = false;
            textRect.sizeDelta = new Vector2(w, h);
            textBox.textWrappingMode = TextWrappingModes.NoWrap;
        }
        else
        {
            if (textFitter != null) textFitter.enabled = true;
            textRect.sizeDelta = new Vector2(maxWidth, 0);
            textBox.textWrappingMode = TextWrappingModes.Normal;
            textBox.ForceMeshUpdate();
        }

        textBox.color = new Color32(0, 0, 0, 0);
    }

    private void ResizeBox()
    {
        textBox.ForceMeshUpdate();
        float textW = Mathf.Min(textBox.preferredWidth, maxWidth);
        float textH = textBox.preferredHeight;
        float boxH = textH + marginY * 2;

        // left / right / middle: 고정 크기 (Y만 높이에 맞게)
        middleBox.sizeDelta = new Vector2(middleBox.sizeDelta.x, boxH);

        // border만 X(텍스트 너비에 맞게) + Y(줄바꿈 시 높이)
        float fixedW = leftBox.sizeDelta.x + middleBox.sizeDelta.x + rightBox.sizeDelta.x;
        float borderW = Mathf.Max(0, (textW + marginX * 2 - fixedW) / 2f);

        leftBorderBox.sizeDelta = new Vector2(borderW, boxH);
        rightBorderBox.sizeDelta = new Vector2(borderW, boxH);

        // 위치 배치 (중앙 기준)
        float halfMid = middleBox.sizeDelta.x / 2f;
        float halfBorder = borderW / 2f;
        float halfLeftCap = leftBox.sizeDelta.x / 2f;
        float halfRightCap = rightBox.sizeDelta.x / 2f;

        middleBox.anchoredPosition = Vector2.zero;
        leftBorderBox.anchoredPosition = new Vector2(-(halfMid + halfBorder), 0);
        rightBorderBox.anchoredPosition = new Vector2(halfMid + halfBorder, 0);
        leftBox.anchoredPosition = new Vector2(-(halfMid + borderW + halfLeftCap), 0);
        rightBox.anchoredPosition = new Vector2(halfMid + borderW + halfRightCap, 0);

        // 루트 RectTransform 전체 크기 업데이트
        uiRect.sizeDelta = new Vector2(fixedW + borderW * 2, boxH);
    }

    private void SetupMeshCache()
    {
        textBox.ForceMeshUpdate();
        textInfo = textBox.textInfo;

        originalVertices = new Vector3[textInfo.meshInfo.Length][];
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
            originalVertices[i] = (Vector3[])textInfo.meshInfo[i].vertices.Clone();

        int count = textInfo.characterCount;

        baseOffsets = new Vector3[count];
        effectOffsets = new Vector3[count];
        charAlpha = new float[count];
        randomPhase = new float[count];

        for (int i = 0; i < count; i++)
        {
            baseOffsets[i] = Vector3.zero;
            effectOffsets[i] = Vector3.zero;
            charAlpha[i] = 0f;
            randomPhase[i] = Random.Range(0f, 100f);
        }
    }

    private void SetBoxVisible(bool visible)
    {
        leftBox.gameObject.SetActive(visible);
        leftBorderBox.gameObject.SetActive(visible);
        middleBox.gameObject.SetActive(visible);
        rightBorderBox.gameObject.SetActive(visible);
        rightBox.gameObject.SetActive(visible);
        textBox.gameObject.SetActive(visible);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────
    #region Text Animation

    private async UniTask PlayTextAnim()
    {
        var token = showCTS.Token;
        int crumbleIndex = 0;

        for (curIndex = 0; curIndex < textInfo.characterCount; curIndex++)
        {
            if (token.IsCancellationRequested) return;

            // 1. 딜레이
            if (curData.delayAt != null && curData.delayAt.TryGetValue(curIndex, out int delayMs))
            {
                PlayTalkSound(delayMs).Forget();
                await SafeDelay(delayMs, token);
            }

            // 2. 효과음 끊김
            if (curData.soundBreaks != null &&
                curData.soundBreaks.Count > 0 &&
                curData.soundBreaks.Peek() == curIndex)
            {
                int nextBreak = curData.soundBreaks.Dequeue();
                PlayTalkSound((nextBreak - curIndex) * letterShowDelay).Forget();
            }

            // 3. 화면 흔들림
            if (crumbleIndex < curData.crambleTiming.Length &&
                curData.crambleTiming[crumbleIndex] == curIndex)
            {
                // CameraManager.instance.CameraShake(0.07f, 0.1f);
                crumbleIndex++;
            }

            // 4. 공백 스킵
            if (textInfo.characterInfo[curIndex].character == ' ')
            {
                await SafeDelay(letterShowDelay, token);
                continue;
            }

            // 5. 페이드인
            await FadeInChar(curIndex, token);
            if (token.IsCancellationRequested) return;

            await SafeDelay(letterShowDelay, token);
        }
    }

    private async UniTask FadeInChar(int index, CancellationToken token)
    {
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            if (token.IsCancellationRequested) return;

            float t = elapsed / fadeDuration;

            charAlpha[index] = t;
            baseOffsets[index] = new Vector3(0, Mathf.Lerp(-moveDistance, 0, t), 0);

            elapsed += Time.deltaTime;
            await UniTask.Yield(PlayerLoopTiming.Update);
        }

        charAlpha[index] = 1f;
        baseOffsets[index] = Vector3.zero;
    }

    #endregion

    // ─────────────────────────────────────────────────────────────
    #region Special Effects

    private void ApplyRangeEffect(TextRange[] ranges, System.Action<int, int> applyFn)
    {
        if (ranges == null) return;
        foreach (var range in ranges)
        {
            if (range.startIndex > curIndex) break;
            applyFn(range.startIndex, Mathf.Min(range.endIndex, curIndex));
        }
    }

    private void ApplyShake(int start, int end, float time)
    {
        for (int i = start; i < end; i++)
        {
            if (charAlpha[i] < 1f) continue;

            float phase = randomPhase[i];

            float ox = Mathf.Sin((time + phase) * 30f) * shakeIntensity;
            float oy = Mathf.Cos((time + phase) * 30f) * shakeIntensity;

            effectOffsets[i] += new Vector3(ox, oy, 0);
        }
    }

    private void ApplyUpwn(int start, int end, float time)
    {
        for (int i = start; i < end; i++)
        {
            if (charAlpha[i] < 1f) continue;

            float oy = Mathf.Sin((time + i) * 10f) * upwnIntensity;
            effectOffsets[i] += new Vector3(0, oy, 0);
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────────
    #region Sound

    private async UniTask PlayTalkSound(int durationMs)
    {
        var token = showCTS.Token;
        if (token.IsCancellationRequested || durationMs <= 0) return;

        int total = Mathf.Max(1, Mathf.RoundToInt(durationMs / (letterShowDelay * talkSpeed)));
        int oneDelay = Mathf.RoundToInt(durationMs / total / 4f * 3f);

        for (int i = 0; i < total; i++)
        {
            if (token.IsCancellationRequested) return;
            // SoundManager.instance.CreateInstance(FmodEvents.instance.talkEvent, Random.Range(0.8f, 1.2f));
            await SafeDelay(oneDelay, token);
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────────
    #region Mesh Helpers

    private async UniTask RenderLoop()
    {
        var token = effectCTS.Token;

        while (!token.IsCancellationRequested)
        {
            textBox.ForceMeshUpdate();
            textInfo = textBox.textInfo;

            float t = Time.time;

            // effect 초기화
            for (int i = 0; i < effectOffsets.Length; i++)
                effectOffsets[i] = Vector3.zero;

            // 효과 적용
            ApplyRangeEffect(curData.shakeRanges, (s, e) => ApplyShake(s, e, t));
            ApplyRangeEffect(curData.upwnRanges, (s, e) => ApplyUpwn(s, e, t));

            // vertex 반영
            for (int i = 0; i < textInfo.characterCount; i++)
            {
                if (!textInfo.characterInfo[i].isVisible) continue;

                int mat = textInfo.characterInfo[i].materialReferenceIndex;
                int vert = textInfo.characterInfo[i].vertexIndex;

                var vertices = textInfo.meshInfo[mat].vertices;
                var colors = textInfo.meshInfo[mat].colors32;

                Vector3 finalOffset = baseOffsets[i] + effectOffsets[i];
                byte alpha = (byte)(charAlpha[i] * 255);

                for (int j = 0; j < 4; j++)
                {
                    vertices[vert + j] = originalVertices[mat][vert + j] + finalOffset;
                    colors[vert + j] = new Color32(0, 0, 0, alpha);
                }
            }

            textBox.UpdateVertexData(TMP_VertexDataUpdateFlags.All);

            await UniTask.Yield(PlayerLoopTiming.Update);
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────────
    #region CTS Management

    private void CancelAll()
    {
        showCTS?.Cancel(); effectCTS?.Cancel();
        showCTS?.Dispose(); effectCTS?.Dispose();
        showCTS = null; effectCTS = null;
    }

    private void ResetCTS()
    {
        showCTS = new CancellationTokenSource();
        effectCTS = new CancellationTokenSource();
    }

    private async UniTask SafeDelay(int ms, CancellationToken token)
    {
        try { await UniTask.Delay(ms, cancellationToken: token); }
        catch (System.OperationCanceledException) { }
    }

    #endregion
}
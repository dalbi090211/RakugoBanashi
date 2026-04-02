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
    [SerializeField] private float maxWidth = 600f;

    // ── TMP ──────────────────────────────────────────────────────
    [SerializeField] private TextMeshPro textBox;
    private TMP_TextInfo textInfo;
    private Vector3[][] originalVertices;

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
    }

    private void OnDestroy() => CancelAll();

    #endregion

    // ─────────────────────────────────────────────────────────────
    #region Public API

    public async UniTask Init(TextBoxData data)
    {
        textBox.text = "";
        CancelAll();
        ResetCTS();

        curData = data;
        curIndex = 0;
        ApplyText(data.parsedText);

        SetupMeshCache();
        RenderLoop().Forget();
        await PlayTextAnim();
    }

    public void Hide()
    {
        CancelAll();
        textBox.text = "";
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

        // maxWidth 넘으면 줄바꿈, 아니면 한 줄
        if (textBox.preferredWidth > maxWidth)
            textBox.textWrappingMode = TextWrappingModes.Normal;
        else
            textBox.textWrappingMode = TextWrappingModes.NoWrap;

        textBox.ForceMeshUpdate();
        textBox.color = new Color32(255, 255, 255, 0);
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
                    colors[vert + j] = new Color32(255, 255, 255, alpha);
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
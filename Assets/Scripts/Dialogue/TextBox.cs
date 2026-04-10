using Cysharp.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class TextBox : MonoBehaviour
{
    [SerializeField] private float maxWidth = 600f;

    [SerializeField] private TextMeshPro textBox;
    private TMP_TextInfo textInfo;
    private Vector3[][] originalVertices;

    [Header("출력 속도")]
    [SerializeField] public float fadeDuration = 0.01f;
    [SerializeField] public float moveDistance = 5f;
    [SerializeField] public int letterShowDelay = 30;

    [Header("사운드")]
    [SerializeField] private float talkSpeed = 1f;

    [Header("특수효과")]
    [SerializeField] private float shakeIntensity = 0.1f;
    [SerializeField] private float upwnIntensity = 2f;

    private int curIndex;
    private TextBoxData curData;
    private CancellationTokenSource showCTS;
    private CancellationTokenSource effectCTS;

    private Vector3[] baseOffsets;
    private Vector3[] effectOffsets;
    private float[] charAlpha;
    private float[] randomPhase;

    #region Unity Lifecycle

    private void OnDestroy() => CancelAll();

    #endregion

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
            return true;

        letterShowDelay = 0;
        return false;
    }

    #endregion

    #region Init Helpers

    private void ApplyText(string text)
    {
        textBox.textWrappingMode = TextWrappingModes.Normal;
        textBox.text = text;
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

    #region Text Animation

    private async UniTask PlayTextAnim()
    {
        var token = showCTS.Token;
        int crumbleIndex = 0;

        for (curIndex = 0; curIndex < textInfo.characterCount; curIndex++)
        {
            if (token.IsCancellationRequested) return;

            // 1. 커스텀 딜레이 (<delay=0.3f> 포함)
            // ── 변경: int → float 으로 받아서 SafeDelay(float) 호출
            if (curData.delayAt != null &&
                curData.delayAt.TryGetValue(curIndex, out float delayMs))
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

            // 3. 카메라 흔들림
            if (crumbleIndex < curData.crambleTiming.Length &&
                curData.crambleTiming[crumbleIndex] == curIndex)
            {
                EnvManager.Instance.CameraShake(0.07f, 0.1f);
                crumbleIndex++;
            }

            // 4. 공백 스킵
            if (textInfo.characterInfo[curIndex].character == ' ')
            {
                await SafeDelay(letterShowDelay, token);
                continue;
            }

            // 6. 글자 간 딜레이
            // ── 변경: speedRanges에서 현재 인덱스의 multiplier를 찾아 딜레이에 적용
            float speedMul = GetSpeedMultiplier(curIndex);
            await FadeInChar(curIndex, speedMul, token);        // speedMul 넘기기
            await SafeDelay(letterShowDelay / speedMul, token);
        }
    }

    /// <summary>
    /// speedRanges에서 charIndex에 해당하는 배율을 반환.
    /// 해당 구간이 없으면 1.0f (기본 속도)
    /// </summary>
    private float GetSpeedMultiplier(int charIndex)
    {
        if (curData.speedRanges == null)
        {
            Debug.Log("speedRanges null"); // null이면 파싱 문제
            return 1.0f;
        }

        foreach (var range in curData.speedRanges)
        {
            Debug.Log($"[{range.start}~{range.end}] mul={range.multiplier} / curIndex={charIndex}");
            if (charIndex >= range.start && charIndex <= range.end)
                return range.multiplier;
        }
        return 1.0f;
    }

    // FadeInChar에 speedMul 파라미터 추가
    private async UniTask FadeInChar(int index, float speedMul, CancellationToken token)
    {
        float elapsed = 0f;
        float duration = fadeDuration / speedMul;  // ← speed 반영

        while (elapsed < duration)
        {
            if (token.IsCancellationRequested) return;

            float t = elapsed / duration;
            charAlpha[index] = t;
            baseOffsets[index] = new Vector3(0, Mathf.Lerp(-moveDistance, 0, t), 0);

            elapsed += Time.deltaTime;
            await UniTask.Yield(PlayerLoopTiming.Update);
        }

        charAlpha[index] = 1f;
        baseOffsets[index] = Vector3.zero;
    }

    #endregion

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

            float ox = Mathf.Sin((time + phase) * 23f) * shakeIntensity;
            float oy = Mathf.Sin((time + phase) * 17f) * shakeIntensity;

            effectOffsets[i] += new Vector3(ox, oy, 0);
        }
    }

    private void ApplyUpwn(int start, int end, float time)
    {
        for (int i = start; i < end; i++)
        {
            if (charAlpha[i] < 1f) continue;

            // i * 0.5f → 글자 간 위상차를 작게 줘서 부드러운 파도 연출
            float oy = Mathf.Sin(time * 3f + i * 0.5f) * upwnIntensity;
            effectOffsets[i] += new Vector3(0, oy, 0);
        }
    }

    #endregion

    #region Sound

    // ── 변경: durationMs를 float으로 통일
    private async UniTask PlayTalkSound(float durationMs)
    {
        var token = showCTS.Token;
        if (token.IsCancellationRequested || durationMs <= 0f) return;

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

    #region Mesh Helpers

    private async UniTask RenderLoop()
    {
        var token = effectCTS.Token;

        while (!token.IsCancellationRequested)
        {

            float t = Time.time;

            for (int i = 0; i < effectOffsets.Length; i++)
                effectOffsets[i] = Vector3.zero;

            ApplyRangeEffect(curData.shakeRanges, (s, e) => ApplyShake(s, e, t));
            ApplyRangeEffect(curData.upwnRanges, (s, e) => ApplyUpwn(s, e, t));

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

    // int 버전 (기존 호환)
    private async UniTask SafeDelay(int ms, CancellationToken token)
    {
        try { await UniTask.Delay(ms, cancellationToken: token); }
        catch (System.OperationCanceledException) { }
    }

    // ── 추가: float 버전 (speed 배율 적용 후 소수점 딜레이 처리)
    private async UniTask SafeDelay(float ms, CancellationToken token)
    {
        int rounded = Mathf.Max(0, Mathf.RoundToInt(ms));
        try { await UniTask.Delay(rounded, cancellationToken: token); }
        catch (System.OperationCanceledException) { }
    }

    #endregion
}
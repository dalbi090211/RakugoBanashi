using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

/// <summary>
/// 대화 관리 매니저 (테스트용 간소화 버전)
/// - CameraManager 의존성 제거
/// - TextBox.Init(Vector2, TextBoxData) 적용
/// - 파싱은 임시로 Manager에서 직접 처리
/// </summary>
public class DialogueManager : Singleton<DialogueManager>
{

    // ── References ───────────────────────────────────────────────
    [SerializeField] private GameObject talkUI;
    [SerializeField] private GameObject textBoxPrefab;
    [SerializeField] private ButtonMapper titleManager;
    [SerializeField] private TextMeshPro leftTextBox;
    [SerializeField] private TextMeshPro rightTextBox;

    // ── 설정 ─────────────────────────────────────────────────────
    [Header("텍스트 박스 위치")]
    [SerializeField] private float textBoxOffsetY = 1.5f;

    [Header("딜레이")]
    [SerializeField] private float setBoxDelay = 0.1f;
    [SerializeField] private float boxFadingDelay = 0.5f;

    // ── 런타임 상태 ───────────────────────────────────────────────
    private DialogueData curTalkData;
    private DialogueData appendData;
    private GameObject curTextBoxObj;
    private bool eventLock;
    private EventCommandInvoker eventCommandInvoker = new EventCommandInvoker();

    // ─────────────────────────────────────────────────────────────
    #region Unity Lifecycle

    protected override void Awake()
    {
        base.Awake();
    }

    private void Update()
    {
        // TODO: InputManager 연결 후 주석 해제
        // if (InputManager.WasPressedThisFrame(KeyList.nextEvent))
        //     SkipText();
    }

    #endregion

    // ─────────────────────────────────────────────────────────────
    #region Public API

    public async UniTask StartEvent(DialogueData data)
    {
        if (data == null) { Debug.LogError("DialogueData가 null입니다."); return; }
        titleManager.TitleOff();
        await EnvManager.Instance.SetBrightEnv(2.0f);
        curTalkData = data;
        await TimeLineEvent();
        await EnvManager.Instance.SetDarkEnv(2.0f);
        titleManager.TitleOn();
    }

    public void AppendEvent(DialogueData data)
    {
        appendData = data;
    }

    public void SkipText()
    {
        if (!eventLock) return;

        bool isDone = curTextBoxObj != null &&
                      curTextBoxObj.GetComponent<TextBox>().Skip();

        if (isDone)
        {
            Destroy(curTextBoxObj);
            curTextBoxObj = null;
            eventLock = false;
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────────
    #region Timeline

    private async UniTask TimeLineEvent()
    {
        for (int i = 0; i < curTalkData.dialogues.Count; i++)
        {
            var ev = curTalkData.dialogues[i];
            await UniTask.Delay(TimeSpan.FromSeconds(ev.delayBefore));

            switch (ev.Type)
            {
                case eventType.Dial:
                    var dialogue = ev as Dialogue;
                    await CreateTalkBoxRoutine(dialogue);
                    if (dialogue.isLeft) EnvManager.Instance.SetLeftVCam(1f).Forget();
                    else EnvManager.Instance.SetRightVCam(1f).Forget();
                    await UniTask.Delay(TimeSpan.FromSeconds(boxFadingDelay));
                    SkipText();
                    break;

                case eventType.Anim:
                    var anim = ev as Animation;
                    anim.AnimTarget.GetComponent<Animator>().Play(anim.animationName);
                    break;

                case eventType.Event:
                    var trigger = ev as MethodTrigger;
                    eventCommandInvoker.InvokeCommand(
                        $"{trigger.commandName}:{trigger.commandParameter}");
                    break;
            }

            if (eventLock)
                await UniTask.WaitUntil(() => !eventLock);
        }

        if (appendData != null)
        {
            curTalkData = appendData;
            appendData = null;
            await TimeLineEvent();
        }

        CleanUp();
    }

    #endregion

    // ─────────────────────────────────────────────────────────────
    #region TextBox 생성

    private async UniTask CreateTalkBoxRoutine(Dialogue dialogue)
    {
        eventLock = dialogue.checkInput;
        await UniTask.Delay(TimeSpan.FromSeconds(setBoxDelay));
        await CreateTalkBox(dialogue);
    }

    private async UniTask CreateTalkBox(Dialogue dialogue)
    {
        // 이전 텍스트박스 정리
        if (curTextBoxObj != null)
            Destroy(curTextBoxObj);

        curTextBoxObj = Instantiate(textBoxPrefab, Vector3.zero, Quaternion.identity,
                                    talkUI.transform);

        Vector2 uiPos = GetUIPosition(dialogue.TextTarget);
        TextBoxData data = BuildTextBoxData(dialogue.Text);
        await curTextBoxObj.GetComponent<TextBox>().Init(uiPos, data);
    }

    /// <summary>
    /// 월드 오브젝트 머리 위 → Canvas anchoredPosition 변환
    /// CameraManager 없이 Camera.main 직접 사용
    /// </summary>
    private Vector2 GetUIPosition(GameObject target)
    {
        float topY = target.transform.position.y;
        var sr = target.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
            topY = sr.bounds.max.y;

        Vector3 worldPos = target.transform.position;
        worldPos.y = topY + textBoxOffsetY;

        Vector2 viewportPoint = Camera.main.WorldToViewportPoint(worldPos);
        viewportPoint.x = Mathf.Clamp01(viewportPoint.x);
        viewportPoint.y = Mathf.Clamp01(viewportPoint.y);

        var canvasRect = talkUI.GetComponent<Canvas>().GetComponent<RectTransform>();
        Vector2 canvasSize = canvasRect.sizeDelta;

        return new Vector2(
            (viewportPoint.x - 0.5f) * canvasSize.x,
            (viewportPoint.y - 0.5f) * canvasSize.y
        );
    }

    /// <summary>
    /// 텍스트 → TextBoxData 변환
    /// Common.removeTag로 커스텀 태그 파싱 후 TMP 리치텍스트만 남긴다.
    /// TODO: 추후 별도 TextParser 클래스로 분리
    /// </summary>
    [SerializeField] private int letterDelay = 300; // <delay> 1개당 대기 ms
    private TextBoxData BuildTextBoxData(string rawText)
    {
        string text = rawText;

        // 1. 단순 타이밍 태그 파싱
        int[] cramble;
        int[] delayArr;
        (text, cramble) = Common.removeTag(text, "<cramble>");
        (text, delayArr) = Common.removeTag(text, "<delay>");

        // 2. 구간 태그 파싱
        TextRange[] shakeRanges;
        TextRange[] upwnRanges;
        (text, shakeRanges) = Common.removeTag(text, "<shake>", "</shake>");
        (text, upwnRanges) = Common.removeTag(text, "<upwn>", "</upwn>");

        // 3. delayArr → Dictionary<index, ms>
        //    동일 인덱스에 <delay>가 여러 개면 누적
        var delayAt = new Dictionary<int, int>();
        foreach (int idx in delayArr)
        {
            if (delayAt.ContainsKey(idx)) delayAt[idx] += letterDelay;
            else delayAt[idx] = letterDelay;
        }

        // 4. 효과음 끊김 포인트 (쉼표/마침표 위치)
        Queue<int> soundBreaks = Common.nextEscape(
            Common.rich2normal(text), escapeType.comma);

        return new TextBoxData
        {
            parsedText = text,
            delayAt = delayAt,
            soundBreaks = soundBreaks,
            shakeRanges = shakeRanges,
            upwnRanges = upwnRanges,
            crambleTiming = cramble
        };
    }

    #endregion

    // ─────────────────────────────────────────────────────────────
    #region Cleanup

    private void CleanUp()
    {
        curTalkData = null;
        curTextBoxObj = null;
        eventLock = false;
    }

    #endregion
}
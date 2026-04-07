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
    [SerializeField] private ButtonMapper titleManager;
    [SerializeField] private ChoiceEventManager choiceManager;
    [SerializeField] private MakuraStarter makuraStarter;
    [SerializeField] private TextBox leftTextBox;
    [SerializeField] private TextBox rightTextBox;
    [SerializeField] private TextBox middleTextBox;

    // ── 설정 ─────────────────────────────────────────────────────
    [Header("딜레이")]
    [SerializeField] private float textShowDelay = 0.7f;    //광클로 바로 스킵할 수 없도록 스킵에 존재하는 지연시간

    // ── 런타임 상태 ───────────────────────────────────────────────
    private DialogueData curTalkData;
    private DialogueData appendData;
    private bool eventLock;
    private CamDir curDir = CamDir.middle;
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
        await UniTask.Delay(TimeSpan.FromSeconds(1.5f));
        await EnvManager.Instance.SetBrightSpotLight(0f);
        curTalkData = data;

        await UniTask.Delay(TimeSpan.FromSeconds(1f));
        makuraStarter.setInputField(true);
        MakuraResult result = await makuraStarter.InputAwait(data.sceneName);
        makuraStarter.setInputField(false);

        Debug.Log(result.score);
        Debug.Log(result.feedback);

        await TimeLineEvent();
        await EnvManager.Instance.SetDarkEnv(2.0f);
        await EnvManager.Instance.SetOffSpotLight(0f);
        titleManager.TitleOn();
    }

    public void AppendEvent(DialogueData data)
    {
        appendData = data;
    }

    public void SkipText()
    {
        if (!eventLock) return;

        bool isDone;
        if (curDir == CamDir.left)
            isDone = leftTextBox.Skip();
        else if (curDir == CamDir.right)
            isDone = rightTextBox.Skip();
        else
            isDone = middleTextBox.Skip();

        if (isDone)
        {
            if (curDir == CamDir.left)
                leftTextBox.Hide();
            else if (curDir == CamDir.right)
                rightTextBox.Hide();
            else
                middleTextBox.Hide();
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
                    eventLock = dialogue.checkInput;
                    curDir = dialogue.direction;

                    TextBoxData data = BuildTextBoxData(dialogue.Text);

                    if (dialogue.direction == CamDir.left)
                    {
                        rightTextBox.Hide();
                        middleTextBox.Hide();
                        EnvManager.Instance.SetLeftVCam(1f).Forget();
                        await leftTextBox.Init(data);
                    }
                    else if (dialogue.direction == CamDir.right)
                    {
                        leftTextBox.Hide();
                        middleTextBox.Hide();
                        EnvManager.Instance.SetRightVCam(1f).Forget();
                        await rightTextBox.Init(data);
                    }
                    else // middle
                    {
                        leftTextBox.Hide();
                        rightTextBox.Hide();
                        EnvManager.Instance.SetMiddleVCam(1f).Forget();
                        await middleTextBox.Init(data);
                    }
                    await UniTask.Delay(TimeSpan.FromSeconds(textShowDelay));
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

                case eventType.ChoiceDial:
                    var choiceDial = ev as ChoiceDialogue;
                    rightTextBox.Hide();
                    leftTextBox.Hide();
                    middleTextBox.Hide();

                    if (choiceDial.direction == CamDir.left)
                        await EnvManager.Instance.SetLeftVCam(1f);
                    else if (choiceDial.direction == CamDir.right)
                        await EnvManager.Instance.SetRightVCam(1f);
                    else
                        await EnvManager.Instance.SetMiddleVCam(1f);

                    choiceManager.ShowChoice(choiceDial);
                    await choiceManager.TimerTask();
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
        EnvManager.Instance.SetEnvVCam(1f).Forget();
        CleanUp();
    }

    #endregion

    // ─────────────────────────────────────────────────────────────
    #region TextBox 생성

    /// <summary>
    /// 텍스트 → TextBoxData 변환
    /// Common.removeTag로 커스텀 태그 파싱 후 TMP 리치텍스트만 남긴다.
    /// TODO: 추후 별도 TextParser 클래스로 분리
    /// </summary>
    [SerializeField] private int letterDelay = 700; // <delay> 1개당 대기 ms
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

        Common.AddNewLine(ref text);

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
        eventLock = false;
    }

    #endregion
}
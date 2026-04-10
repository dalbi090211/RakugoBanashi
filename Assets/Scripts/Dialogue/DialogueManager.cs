using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

using Random = UnityEngine.Random;

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
    [SerializeField] private GameObject winObj1;
    [SerializeField] private GameObject winObj2;
    [SerializeField] private GameObject loseObj1;
    [SerializeField] private GameObject loseObj2;
    [SerializeField] private Animator animator;


    // ── 몰입도 시스템 ───────────────────────────────────────────────
    private const int flowMax = 100;
    private int curFlow = 0;
    private int flowBench = 50;
    [SerializeField] private Slider flowSlider;
    private float gaugeShowDelay = 1.2f;
    private float gaugeFillDelay = 0.1f;

    // ─────────────────────────────────────────────────────────────
    #region Unity Lifecycle

    protected override void Awake()
    {
        base.Awake();
        flowSlider.gameObject.SetActive(false);
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

    public async UniTask ShowText(string text, CamDir direction)
    {
        TextBoxData data = BuildTextBoxData(text);
        float cvtTime = 1f;

        setAnim(dirToTalkState(direction)); // 말하기 시작

        if (direction == CamDir.left)
        {
            rightTextBox.Hide();
            middleTextBox.Hide();
            EnvManager.Instance.SetLeftVCam(cvtTime).Forget();
            await leftTextBox.Init(data);
        }
        else if (direction == CamDir.right)
        {
            leftTextBox.Hide();
            middleTextBox.Hide();
            EnvManager.Instance.SetRightVCam(cvtTime).Forget();
            await rightTextBox.Init(data);
        }
        else
        {
            leftTextBox.Hide();
            rightTextBox.Hide();
            EnvManager.Instance.SetMiddleVCam(cvtTime).Forget();
            await middleTextBox.Init(data);
        }

        setAnim(1); // idle로 복귀
        await UniTask.Delay(TimeSpan.FromSeconds(textShowDelay));
    }

    private async UniTask showFlowSliderFill(int flow)
    {
        flowSlider.gameObject.SetActive(true);
        flowSlider.maxValue = flowMax;
        flowSlider.value = curFlow; // 현재값부터 시작

        int targetFlow = Mathf.Clamp(curFlow + flow, 0, flowMax);

        while (flowSlider.value < targetFlow)
        {
            flowSlider.value += 1;
            await UniTask.Delay(TimeSpan.FromSeconds(gaugeFillDelay));
        }

        flowSlider.value = targetFlow;
        curFlow = targetFlow;
    }

    private async UniTask startFlowResult(int flow)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(gaugeShowDelay));
        await showFlowSliderFill(flow);
        await UniTask.Delay(TimeSpan.FromSeconds(gaugeShowDelay));
        flowSlider.gameObject.SetActive(false);
    }

    private async UniTask emotionTask(int score, bool clear)
    {
        rightTextBox.Hide();
        leftTextBox.Hide();
        middleTextBox.Hide();
        EnvManager.Instance.SetEnvVCam(1.0f).Forget();
        Debug.Log($"점수: {score}");
        makuraStarter.setInputField(false);
        startEmotion(clear).Forget();
        await startFlowResult(score);
    }

    private async UniTask startEmotion(bool clear)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(1.5f));
        if (clear)
        {
            await spawnEmotions(winObj1, winObj2);
        }
        else
        {
            await spawnEmotions(loseObj1, loseObj2);
        }
    }

    private async UniTask spawnEmotions(GameObject obj1, GameObject obj2)
    {
        int count = 20;

        for (int i = 0; i < count; i++)
        {
            float x = Random.Range(-5.84f, 5.84f);
            float y = Random.Range(-2.5f, -1.6f);
            Vector3 pos = new Vector3(x, y, 0f);

            GameObject prefab = Random.value > 0.5f ? obj1 : obj2;
            Instantiate(prefab, pos, Quaternion.identity);

            await UniTask.Delay(TimeSpan.FromMilliseconds(80));
        }
    }

    public async UniTask StartEvent(DialogueData data)
    {
        if (data == null) { Debug.LogError("DialogueData가 null입니다."); return; }
        curFlow = 30;
        flowSlider.value = curFlow / flowMax;
        titleManager.TitleOff();
        await EnvManager.Instance.SetBrightEnv(2.0f);
        await UniTask.Delay(TimeSpan.FromSeconds(1.5f));
        await EnvManager.Instance.SetBrightSpotLight(0f);
        curTalkData = data;

        //ai평가
        await UniTask.Delay(TimeSpan.FromSeconds(1f));
        await makuraStarter.fillInputField(data.sceneName);
        await UniTask.Delay(TimeSpan.FromSeconds(2f));
        makuraStarter.setInputField(true);
        MakuraResult result = await makuraStarter.InputAwait(data.sceneName);
        if (result == null)
        {
            Debug.LogError("AI 평가 실패");
            // 기본값으로 진행하거나 재시도
            return;
        }
        await UniTask.Delay(TimeSpan.FromSeconds(0.6f));
        await emotionTask(result.score, result.score > 0);
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
        // ── List → 인덱스 순회 대신 앞에 삽입 가능하도록 변경
        var queue = new List<GameEvent>(curTalkData.dialogues);

        for (int i = 0; i < queue.Count; i++)
        {
            var ev = queue[i];
            await UniTask.Delay(TimeSpan.FromSeconds(ev.delayBefore));

            switch (ev.Type)
            {
                case eventType.Dial:
                    var dialogue = ev as Dialogue;
                    eventLock = dialogue.checkInput;
                    curDir = dialogue.direction;

                    TextBoxData data = BuildTextBoxData(dialogue.Text);

                    // ── 말하기 시작 ──────────────────────────────
                    setAnim(dirToTalkState(dialogue.direction));

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
                    else
                    {
                        leftTextBox.Hide();
                        rightTextBox.Hide();
                        EnvManager.Instance.SetMiddleVCam(1f).Forget();
                        await middleTextBox.Init(data);
                    }

                    // ── 타이핑 끝난 후 idle ──────────────────────
                    setAnim(1);

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

                    // ── 변경: TimerTask() 제거하고 ShowChoice 하나로 통합
                    await choiceManager.ShowChoice(choiceDial);
                    break;

                case eventType.Emotion:
                    var emotionDial = ev as Emotion;
                    await emotionTask(emotionDial.score, emotionDial.clear);
                    await UniTask.Delay(TimeSpan.FromSeconds(1.5f));
                    break;
            }

            if (eventLock)
                await UniTask.WaitUntil(() => !eventLock);

            // ── 변경: appendData가 생기면 현재 위치 바로 다음에 삽입
            if (appendData != null)
            {
                queue.InsertRange(i + 1, appendData.dialogues);
                appendData = null;
            }
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

        int[] cramble;
        (text, cramble) = Common.removeTag(text, "<cramble>");

        // <delay> 고정값 + <delay=0.3f> 커스텀값 병합
        int[] delayFixed;
        Dictionary<int, float> delayCustom;
        (text, delayFixed) = Common.removeTag(text, "<delay>");
        (text, delayCustom) = Common.removeParamTag(text, "delay");

        // speed 태그: 구간 태그로 처리 (시작~끝 인덱스 + 배율)
        SpeedRange[] speedRanges;
        (text, speedRanges) = Common.removeParamRangeTag(text, "speed");

        TextRange[] shakeRanges;
        TextRange[] upwnRanges;
        (text, shakeRanges) = Common.removeTag(text, "<shake>", "</shake>");
        (text, upwnRanges) = Common.removeTag(text, "<upwn>", "</upwn>");

        // 고정 delay + 커스텀 delay 병합
        var delayAt = new Dictionary<int, float>();
        foreach (int idx in delayFixed)
        {
            if (delayAt.ContainsKey(idx)) delayAt[idx] += letterDelay;
            else delayAt[idx] = letterDelay;
        }
        foreach (var kv in delayCustom)
        {
            if (delayAt.ContainsKey(kv.Key)) delayAt[kv.Key] += kv.Value;
            else delayAt[kv.Key] = kv.Value;
        }

        Queue<int> soundBreaks = Common.nextEscape(
            Common.rich2normal(text), escapeType.comma);

        return new TextBoxData
        {
            parsedText = text,
            delayAt = delayAt,       // float으로 변경
            speedRanges = speedRanges,   // 추가
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
        setAnim(1); // 이벤트 종료 후 idle 보장
    }
    #endregion

    // ─────────────────────────────────────────────────────────────
    #region Animator

    private void setAnim(int parameter)
    {
        if (animator == null) return;
        animator.SetInteger("curState", parameter);
    }

    private int dirToTalkState(CamDir dir)
    {
        return dir switch
        {
            CamDir.left => 3,
            CamDir.right => 4,
            _ => 2,   // middle → 정면
        };
    }

    #endregion
}